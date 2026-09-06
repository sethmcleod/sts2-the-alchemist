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

// One home for the Mix family: the kinds, the two tiers, the picker, and the tips, so every maker
// shows the same screen and the family can grow in one place. Basic Mixes are the choice set below
// Rare; random creation draws from all of them
public static class Mixing
{
    private static LocString SelectPrompt => new("card_keywords", "ALCHEMIST-MIX.selectionPrompt");
    private static LocString SelectBasicPrompt => new("card_keywords", "ALCHEMIST-MIX.selectionPromptBasic");
    private static LocString SelectSpecialPrompt => new("card_keywords", "ALCHEMIST-MIX.selectionPromptSpecial");

    public static readonly MixKind[] Basic = { MixKind.Bursting, MixKind.Syrupy, MixKind.Zesty };
    public static readonly MixKind[] Special = { MixKind.Fuming, MixKind.Acrid, MixKind.Sparkling };
    public static readonly MixKind[] All =
        { MixKind.Bursting, MixKind.Syrupy, MixKind.Zesty, MixKind.Fuming, MixKind.Acrid, MixKind.Sparkling };

    public static bool IsBasic(MixKind kind) => Basic.Contains(kind);

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

    // A Compound Mix counts as a Mix everywhere, but it cannot be an ingredient again
    public static bool IsIngredient(CardModel card) => KindOf(card) != null;

    public static IEnumerable<IHoverTip> MixTips(bool upgraded = false) =>
        upgraded ? AlchemistTips.MixUpgraded : AlchemistTips.Mix;

    /// <summary>How many Mixes this player has played this combat. 0 outside combat.</summary>
    // Started, not finished: the hand previews refresh while the Mix is still resolving, and a
    // finished-only count lags one play behind on the card face
    public static int PlayedThisCombat(Player owner) =>
        CombatManager.Instance?.History.CardPlaysStarted
            .Count(e => IsMix(e.CardPlay.Card) && e.CardPlay.Card.Owner == owner) ?? 0;

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
        kinds.SequenceEqual(Basic) ? SelectBasicPrompt
        : kinds.SequenceEqual(Special) ? SelectSpecialPrompt
        : SelectPrompt;

    /// <summary>
    /// Shows the given Mixes (the basic three by default) and returns the chosen one, unadded. Null
    /// outside combat. With upgraded, the grid shows the + versions, so the previews match what is given.
    /// </summary>
    public static async Task<CardModel?> Choose(PlayerChoiceContext ctx, Player owner,
        bool upgraded = false, IReadOnlyList<MixKind>? kinds = null)
    {
        kinds ??= Basic;
        if (owner.Creature.CombatState is not { } combat) return null;
        var options = Options(combat, owner, kinds);
        if (upgraded)
            foreach (var option in options)
                CardCmd.Upgrade(option);
        var picked = (await CardSelectCmd.FromSimpleGrid(ctx, options, owner,
            new CardSelectorPrefs(PromptFor(kinds), 1))).FirstOrDefault();
        if (picked != null) RecordCreated(owner, picked);
        return picked;
    }

    // Every created Mix goes through here so the Mixes badge and the analytics count them all,
    // including the makers that skip the picker (Grand Batch, Effervesce)
    public static void RecordCreated(Player? creator, CardModel mix) =>
        Analytics.RunCounters.Add(creator, mix switch
        {
            BurstingMix => Analytics.RunCounters.MixBursting,
            FumingMix => Analytics.RunCounters.MixFuming,
            SyrupyMix => Analytics.RunCounters.MixSyrupy,
            ZestyMix => Analytics.RunCounters.MixZesty,
            AcridMix => Analytics.RunCounters.MixAcrid,
            SparklingMix => Analytics.RunCounters.MixSparkling,
            _ => Analytics.RunCounters.MixCompound,
        }, 1);

    /// <summary>Add a random Mix (from all six by default) to the owner's hand. Seeded, so multiplayer stays in sync.</summary>
    public static async Task CreateRandom(PlayerChoiceContext ctx, Player owner, bool upgraded = false,
        IReadOnlyList<MixKind>? kinds = null)
    {
        if (owner.Creature.CombatState is not { } combat) return;
        var kind = owner.RunState.Rng.CombatCardGeneration.NextItem(kinds ?? All);
        var picked = Create(combat, owner, kind);
        if (picked == null) return;
        if (upgraded) CardCmd.Upgrade(picked);
        RecordCreated(owner, picked);
        await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
    }

    /// <summary>Add a random basic Mix to another player's hand, counted for the giver.</summary>
    public static async Task GiveRandom(PlayerChoiceContext ctx, Player giver, Player receiver)
    {
        if (receiver.Creature.CombatState is not { } combat) return;
        var picked = Create(combat, receiver, giver.RunState.Rng.CombatCardGeneration.NextItem(Basic));
        if (picked == null) return;
        RecordCreated(giver, picked);
        await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, receiver);
    }

    /// <summary>One picker, many cards: choose a basic Mix once, then add that many copies.</summary>
    public static async Task CreateChosenCopies(PlayerChoiceContext ctx, Player owner, int count)
    {
        if (count <= 0) return;
        var picked = await Choose(ctx, owner);
        if (picked == null) return;
        await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
        for (var i = 1; i < count; i++)
        {
            var copy = picked.CreateClone();
            RecordCreated(owner, copy);
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, owner);
        }
    }

    /// <summary>Choose a Mix and add it to the owner's hand, count times.</summary>
    public static async Task CreateChosen(PlayerChoiceContext ctx, Player owner, int count = 1,
        bool upgraded = false, IReadOnlyList<MixKind>? kinds = null)
    {
        for (var i = 0; i < count; i++)
        {
            var picked = await Choose(ctx, owner, upgraded, kinds);
            if (picked == null) return;
            await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
        }
    }

    /// <summary>Transform an existing card into a chosen Mix. Returns the Mix, or null if cancelled.</summary>
    public static async Task<CardModel?> TransformIntoChosen(PlayerChoiceContext ctx, Player owner,
        CardModel victim, IReadOnlyList<MixKind>? kinds = null)
    {
        var picked = await Choose(ctx, owner, kinds: kinds);
        if (picked == null) return null;
        await CardCmd.Transform(victim, picked);
        return picked;
    }

    /// <summary>Add one specific Mix to the owner's hand. Returns it, or null outside combat.</summary>
    public static async Task<CardModel?> CreateOne<T>(PlayerChoiceContext ctx, Player owner,
        bool upgraded = false)
        where T : CardModel
    {
        if (owner.Creature.CombatState is not { } combat) return null;
        var mix = combat.CreateCard<T>(owner);
        if (upgraded) CardCmd.Upgrade(mix);
        RecordCreated(owner, mix);
        await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, owner);
        return mix;
    }

    /// <summary>Transform an existing card into a random Mix. Seeded, so multiplayer stays in sync.</summary>
    public static async Task<CardModel?> TransformIntoRandom(PlayerChoiceContext ctx, Player owner,
        CardModel victim, bool upgraded = false)
    {
        if (owner.Creature.CombatState is not { } combat) return null;
        var picked = Create(combat, owner, owner.RunState.Rng.CombatCardGeneration.NextItem(All));
        if (picked == null) return null;
        if (upgraded) CardCmd.Upgrade(picked);
        RecordCreated(owner, picked);
        await CardCmd.Transform(victim, picked);
        return picked;
    }

    /// <summary>
    /// Fold two ingredient Mixes into one Compound Mix in the owner's hand. Ethereal carries over,
    /// so a Sparkling's energy cannot be banked through the compound.
    /// </summary>
    public static async Task<CardModel?> CreateCompound(PlayerChoiceContext ctx, Player owner,
        CardModel first, CardModel second)
    {
        if (owner.Creature.CombatState is not { } combat) return null;
        var compound = (CompoundMix)combat.CreateCard<CompoundMix>(owner);
        compound.Compose(first, second);
        if (first.Keywords.Contains(CardKeyword.Ethereal) || second.Keywords.Contains(CardKeyword.Ethereal))
            CardCmd.ApplyKeyword(compound, CardKeyword.Ethereal);
        RecordCreated(owner, compound);
        await CardPileCmd.AddGeneratedCardToCombat(compound, PileType.Hand, owner);
        return compound;
    }
}
