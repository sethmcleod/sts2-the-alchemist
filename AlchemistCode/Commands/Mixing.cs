using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Cards.Token;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Alchemist.AlchemistCode.Commands;

public enum MixKind
{
    Bursting,
    Syrupy,
    Zesty,
    Fuming,
    Acrid,
    Sparkling,
}

public static class Mixing
{
    private static LocString SelectPrompt => new("card_keywords", "ALCHEMIST-MIX.selectionPrompt");
    private static LocString SelectBasicPrompt => new("card_keywords", "ALCHEMIST-MIX.selectionPromptBasic");

    public static readonly MixKind[] Basic = { MixKind.Bursting, MixKind.Syrupy, MixKind.Zesty };
    public static readonly MixKind[] Volatile = { MixKind.Fuming, MixKind.Acrid, MixKind.Sparkling };
    public static readonly MixKind[] All =
        { MixKind.Bursting, MixKind.Syrupy, MixKind.Zesty, MixKind.Fuming, MixKind.Acrid, MixKind.Sparkling };

    public static MixKind? KindOf(CardModel card) => card switch
    {
        BurstingMix => MixKind.Bursting,
        SyrupyMix => MixKind.Syrupy,
        ZestyMix => MixKind.Zesty,
        FumingMix => MixKind.Fuming,
        AcridMix => MixKind.Acrid,
        SparklingMix => MixKind.Sparkling,
        _ => null,
    };

    public static bool IsMix(CardModel card) => card is CompoundMix || KindOf(card) != null;

    public static string KindLabel(CardModel card) =>
        KindOf(card)?.ToString().ToLowerInvariant() ?? "compound";

    public static bool IsIngredient(CardModel card) => KindOf(card) != null;

    public static IEnumerable<IHoverTip> MixTips(bool upgraded = false) =>
        upgraded ? AlchemistTips.MixUpgraded : AlchemistTips.Mix;

    public static IEnumerable<IHoverTip> MixRefTips() => new[] { AlchemistTips.MixHeader };

    // CardPlaysStarted, not Finished: a finished-only count lags one play behind on the card face
    public static int PlayedThisCombat(Player owner) =>
        CombatManager.Instance?.History.CardPlaysStarted
            .Where(e => IsMix(e.CardPlay.Card) && e.CardPlay.Card.Owner == owner)
            .Sum(e => e.CardPlay.Card is CompoundMix ? 2 : 1) ?? 0;

    public static int KindsPlayedThisCombat(Player owner) =>
        CombatManager.Instance?.History.CardPlaysStarted
            .Where(e => IsMix(e.CardPlay.Card) && e.CardPlay.Card.Owner == owner)
            .SelectMany(e => e.CardPlay.Card is CompoundMix compound
                ? compound.Ingredients
                : new[] { KindOf(e.CardPlay.Card)!.Value })
            .Distinct()
            .Count() ?? 0;

    public static CardModel Create(ICombatState combat, Player owner, MixKind kind) => kind switch
    {
        MixKind.Bursting => combat.CreateCard<BurstingMix>(owner),
        MixKind.Syrupy => combat.CreateCard<SyrupyMix>(owner),
        MixKind.Zesty => combat.CreateCard<ZestyMix>(owner),
        MixKind.Fuming => combat.CreateCard<FumingMix>(owner),
        MixKind.Acrid => combat.CreateCard<AcridMix>(owner),
        _ => combat.CreateCard<SparklingMix>(owner),
    };

    private static List<CardModel> Options(ICombatState combat, Player owner, IReadOnlyList<MixKind> kinds) =>
        kinds.Select(kind => Create(combat, owner, kind)).ToList();

    private static LocString PromptFor(IReadOnlyList<MixKind> kinds) =>
        kinds.SequenceEqual(Basic) ? SelectBasicPrompt : SelectPrompt;

    public static async Task<CardModel?> Choose(PlayerChoiceContext ctx, Player owner,
        bool upgraded = false, IReadOnlyList<MixKind>? kinds = null, AbstractModel? source = null,
        Action<CardModel>? prepare = null)
    {
        kinds ??= Basic;
        if (owner.Creature.CombatState is not { } combat) return null;
        var options = Options(combat, owner, kinds);
        if (upgraded)
            foreach (var option in options)
                CardCmd.Upgrade(option);
        if (prepare != null)
            foreach (var option in options)
                prepare(option);
        Patches.MixPickerGridPatch.Options = new HashSet<CardModel>(options, ReferenceEqualityComparer.Instance);
        Patches.MixPickerGridPatch.Columns = options.Count > 5 ? (options.Count + 1) / 2 : null;
        CardModel? picked;
        try
        {
            picked = (await CardSelectCmd.FromSimpleGrid(ctx, options, owner,
                new CardSelectorPrefs(PromptFor(kinds), 1))).FirstOrDefault();
        }
        finally
        {
            Patches.MixPickerGridPatch.Clear();
        }
        if (picked != null) RecordCreated(owner, picked, source);
        return picked;
    }

    public static void RecordCreated(Player? creator, CardModel mix, AbstractModel? source = null)
    {
        if (source != null)
            Analytics.RunCounters.Tally(creator, Analytics.RunCounters.MixSource + Analytics.RunCounters.Label(source));
        Analytics.RunCounters.Tally(creator, Analytics.RunCounters.MixMade + KindLabel(mix));
        Analytics.RunCounters.Add(creator, Analytics.RunCounters.MixPrefix + KindLabel(mix), 1);
    }

    public static async Task CreateRandom(PlayerChoiceContext ctx, Player owner, bool upgraded = false,
        IReadOnlyList<MixKind>? kinds = null, AbstractModel? source = null)
    {
        if (owner.Creature.CombatState is not { } combat) return;
        var kind = owner.RunState.Rng.CombatCardGeneration.NextItem(kinds ?? All);
        var picked = Create(combat, owner, kind);
        if (picked == null) return;
        if (upgraded) CardCmd.Upgrade(picked);
        RecordCreated(owner, picked, source);
        await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
    }

    public static async Task GiveRandom(PlayerChoiceContext ctx, Player giver, Player receiver,
        bool upgraded = false, AbstractModel? source = null)
    {
        if (receiver.Creature.CombatState is not { } combat) return;
        var picked = Create(combat, receiver, giver.RunState.Rng.CombatCardGeneration.NextItem(All));
        if (picked == null) return;
        if (upgraded) CardCmd.Upgrade(picked);
        RecordCreated(giver, picked, source);
        await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, giver);
    }

    public static async Task CreateChosenCopies(PlayerChoiceContext ctx, Player owner, int count,
        bool upgraded = false, AbstractModel? source = null)
    {
        if (count <= 0) return;
        var picked = await Choose(ctx, owner, upgraded, source: source);
        if (picked == null) return;
        // Clone before adding the first, so what Refine does to it doesn't carry over to the copies
        var copies = Enumerable.Range(1, count - 1).Select(_ => picked.CreateClone()).ToList();
        await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
        foreach (var copy in copies)
        {
            RecordCreated(owner, copy, source);
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, owner);
        }
    }

    public static async Task CreateChosen(PlayerChoiceContext ctx, Player owner, int count = 1,
        bool upgraded = false, IReadOnlyList<MixKind>? kinds = null, AbstractModel? source = null)
    {
        for (var i = 0; i < count; i++)
        {
            var picked = await Choose(ctx, owner, upgraded, kinds, source);
            if (picked == null) return;
            await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
        }
    }

    public static async Task<CardModel?> TransformIntoChosen(PlayerChoiceContext ctx, Player owner,
        CardModel victim, bool upgraded = false, IReadOnlyList<MixKind>? kinds = null, AbstractModel? source = null)
    {
        var picked = await Choose(ctx, owner, upgraded, kinds, source);
        if (picked == null) return null;
        await CardCmd.Transform(victim, picked);
        return picked;
    }

    public static async Task<CardModel?> CreateOne<T>(PlayerChoiceContext ctx, Player owner,
        bool upgraded = false, AbstractModel? source = null)
        where T : CardModel
    {
        if (owner.Creature.CombatState is not { } combat) return null;
        var mix = combat.CreateCard<T>(owner);
        if (upgraded) CardCmd.Upgrade(mix);
        RecordCreated(owner, mix, source);
        await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, owner);
        return mix;
    }

    public static async Task<CardModel?> TransformIntoRandom(PlayerChoiceContext ctx, Player owner,
        CardModel victim, bool upgraded = false, AbstractModel? source = null, IReadOnlyList<MixKind>? kinds = null)
    {
        if (owner.Creature.CombatState is not { } combat) return null;
        var picked = Create(combat, owner, owner.RunState.Rng.CombatCardGeneration.NextItem(kinds ?? All));
        if (picked == null) return null;
        if (upgraded) CardCmd.Upgrade(picked);
        RecordCreated(owner, picked, source);
        await CardCmd.Transform(victim, picked);
        return picked;
    }

    public static async Task<CardModel?> Combine(PlayerChoiceContext ctx, Player owner, CardModel first,
        CardModel second, AbstractModel? source = null)
    {
        await CardCmd.Exhaust(ctx, first);
        await CardCmd.Exhaust(ctx, second);
        Analytics.RunCounters.Tally(owner, Analytics.RunCounters.MixCombined, 2);
        return await CreateCompound(ctx, owner, first, second, source);
    }

    public static async Task<CardModel?> CreateCompound(PlayerChoiceContext ctx, Player owner,
        CardModel first, CardModel second, AbstractModel? source = null)
    {
        if (owner.Creature.CombatState is not { } combat) return null;
        var compound = (CompoundMix)combat.CreateCard<CompoundMix>(owner);
        compound.Compose(first, second);
        var pair = new[] { KindLabel(first), KindLabel(second) }.OrderBy(k => k).ToArray();
        Analytics.RunCounters.Tally(owner, Analytics.RunCounters.CompoundPair + string.Join("+", pair));
        RecordCreated(owner, compound, source);
        await CardPileCmd.AddGeneratedCardToCombat(compound, PileType.Hand, owner);
        CarryRetain(compound, first, second);
        return compound;
    }

    private static void CarryRetain(CardModel compound, params CardModel[] ingredients)
    {
        if (ingredients.Any(i => i.Keywords.Contains(CardKeyword.Retain)))
            CardCmd.ApplyKeyword(compound, CardKeyword.Retain);
        else if (ingredients.Any(i => i.ShouldRetainThisTurn))
        {
            compound.GiveSingleTurnRetain();
            if (NCard.FindOnTable(compound) is { } node && compound.Pile is { } pile)
                node.UpdateVisuals(pile.Type, CardPreviewMode.Normal);
        }
    }
}
