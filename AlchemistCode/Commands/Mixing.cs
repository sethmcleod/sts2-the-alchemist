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

// One home for "Choose a Mix": the four options, the picker, and the tips, so every maker shows the
// same screen and the family can grow in one place
public static class Mixing
{
    private static LocString SelectPrompt => new("card_keywords", "ALCHEMIST-MIX.selectionPrompt");

    public static IEnumerable<IHoverTip> MixTips(bool upgraded = false) =>
        upgraded ? AlchemistTips.MixUpgraded : AlchemistTips.Mix;

    public static bool IsMix(CardModel card) =>
        card is BurstingMix or FumingMix or SyrupyMix or ZestyMix;

    /// <summary>How many Mixes this player has played this combat. 0 outside combat.</summary>
    public static int PlayedThisCombat(Player owner) =>
        CombatManager.Instance?.History.CardPlaysFinished
            .Count(e => IsMix(e.CardPlay.Card) && e.CardPlay.Card.Owner == owner) ?? 0;

    private static List<CardModel> Options(ICombatState combat, Player owner) =>
        new()
        {
            combat.CreateCard<BurstingMix>(owner),
            combat.CreateCard<FumingMix>(owner),
            combat.CreateCard<SyrupyMix>(owner),
            combat.CreateCard<ZestyMix>(owner),
        };

    /// <summary>
    /// Shows the four Mixes and returns the chosen one, unadded. Null outside combat.
    /// With upgraded, the grid shows the + versions, so the previews match what is given.
    /// </summary>
    public static async Task<CardModel?> Choose(PlayerChoiceContext ctx, Player owner,
        bool upgraded = false)
    {
        if (owner.Creature.CombatState is not { } combat) return null;
        var options = Options(combat, owner);
        if (upgraded)
            foreach (var option in options)
                CardCmd.Upgrade(option);
        var picked = (await CardSelectCmd.FromSimpleGrid(ctx, options, owner,
            new CardSelectorPrefs(SelectPrompt, 1))).FirstOrDefault();
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
            _ => Analytics.RunCounters.MixZesty,
        }, 1);

    /// <summary>Add a random Mix to the owner's hand. Seeded, so multiplayer stays in sync.</summary>
    public static async Task CreateRandom(PlayerChoiceContext ctx, Player owner, bool upgraded = false)
    {
        if (owner.Creature.CombatState is not { } combat) return;
        var options = Options(combat, owner);
        var picked = owner.RunState.Rng.CombatCardGeneration.NextItem(options);
        if (picked == null) return;
        if (upgraded) CardCmd.Upgrade(picked);
        RecordCreated(owner, picked);
        await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
    }

    /// <summary>
    /// Add a random Mix to another player's hand, counted for the giver. Zesty is left out:
    /// its Antitoxin means nothing to a character without Poison ticks to absorb.
    /// </summary>
    public static async Task GiveRandom(PlayerChoiceContext ctx, Player giver, Player receiver)
    {
        if (receiver.Creature.CombatState is not { } combat) return;
        var options = new List<CardModel>
        {
            combat.CreateCard<BurstingMix>(receiver),
            combat.CreateCard<SyrupyMix>(receiver),
            combat.CreateCard<FumingMix>(receiver),
        };
        var picked = giver.RunState.Rng.CombatCardGeneration.NextItem(options);
        if (picked == null) return;
        RecordCreated(giver, picked);
        await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, receiver);
    }

    /// <summary>One picker, many cards: choose a Mix once, then add that many copies.</summary>
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
        bool upgraded = false)
    {
        for (var i = 0; i < count; i++)
        {
            var picked = await Choose(ctx, owner, upgraded);
            if (picked == null) return;
            await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
        }
    }

    /// <summary>Transform an existing card into a chosen Mix. Returns the Mix, or null if cancelled.</summary>
    public static async Task<CardModel?> TransformIntoChosen(PlayerChoiceContext ctx, Player owner,
        CardModel victim)
    {
        var picked = await Choose(ctx, owner);
        if (picked == null) return null;
        await CardCmd.Transform(victim, picked);
        return picked;
    }
}
