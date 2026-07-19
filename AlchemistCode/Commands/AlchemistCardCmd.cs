using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Commands;

public static class AlchemistCardCmd
{
    // forceUpgrade: null follows the source card's upgrade state; true/false pins the token's upgrade
    public static async Task GiveCard<T>(AlchemistCard source, int count = 1, bool? forceUpgrade = null)
        where T : CardModel, new()
    {
        if (source.CombatState == null) return;
        var upgrade = forceUpgrade ?? source.IsUpgraded;
        for (var i = 0; i < count; i++)
        {
            var card = source.CombatState.CreateCard<T>(source.Owner);
            if (upgrade) CardCmd.Upgrade(card);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, source.Owner);
        }
    }

    public static async Task AddStatus<T>(AlchemistCard source) where T : CardModel, new()
    {
        if (source.CombatState == null) return;
        var card = source.CombatState.CreateCard<T>(source.Owner);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, source.Owner));
    }

    public static async Task TransformFromHand<T>(
        PlayerChoiceContext choiceContext, AlchemistCard source) where T : CardModel, new()
    {
        if (source.CombatState == null) return;
        var selected = (await CardSelectCmd.FromHand(
            choiceContext, source.Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1),
            null, source)).FirstOrDefault();
        if (selected != null)
        {
            var replacement = source.CombatState.CreateCard<T>(source.Owner);
            if (source.IsUpgraded) CardCmd.Upgrade(replacement);
            await CardCmd.Transform(selected, replacement);
        }
    }

    public static async Task PoisonAll(
        PlayerChoiceContext choiceContext, AlchemistCard source)
    {
        if (source.CombatState == null) return;
        // This targets enemies and the player that casts the card. It excludes allies on purpose
        var targets = source.CombatState.Enemies
            .Append(source.Owner.Creature)
            .Where(c => c.IsAlive);
        foreach (var creature in targets)
            await PowerCmd.Apply<PoisonPower>(
                choiceContext, creature, source.DynamicVars.Poison.BaseValue,
                source.Owner.Creature, source);
    }
}
