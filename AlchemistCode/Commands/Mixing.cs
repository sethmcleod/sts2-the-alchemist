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

    public static IEnumerable<IHoverTip> MixTips() => AlchemistTips.Mix;

    private static List<CardModel> Options(ICombatState combat, Player owner) =>
        new()
        {
            combat.CreateCard<BurstingMix>(owner),
            combat.CreateCard<FumingMix>(owner),
            combat.CreateCard<SturdyMix>(owner),
            combat.CreateCard<ZestyMix>(owner),
        };

    /// <summary>Shows the four Mixes and returns the chosen one, unadded. Null outside combat.</summary>
    public static async Task<CardModel?> Choose(PlayerChoiceContext ctx, Player owner)
    {
        if (owner.Creature.CombatState is not { } combat) return null;
        var options = Options(combat, owner);
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
            SturdyMix => Analytics.RunCounters.MixSturdy,
            _ => Analytics.RunCounters.MixZesty,
        }, 1);

    /// <summary>Choose a Mix and add it to the owner's hand, count times.</summary>
    public static async Task CreateChosen(PlayerChoiceContext ctx, Player owner, int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            var picked = await Choose(ctx, owner);
            if (picked == null) return;
            await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
        }
    }

    /// <summary>Transform an existing card into a chosen Mix.</summary>
    public static async Task TransformIntoChosen(PlayerChoiceContext ctx, Player owner, CardModel victim)
    {
        var picked = await Choose(ctx, owner);
        if (picked == null) return;
        await CardCmd.Transform(victim, picked);
    }
}
