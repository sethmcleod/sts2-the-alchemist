using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Cards;

namespace Alchemist.AlchemistCode.Commands;

public static class AlchemistCardCmd
{
    public static async Task GiveCard<T>(AlchemistCard source) where T : CardModel, new()
    {
        if (source.CombatState == null) return;
        var card = source.CombatState.CreateCard<T>(source.Owner);
        if (source.IsUpgraded) CardCmd.Upgrade(card);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, source.Owner);
    }

    public static async Task ShuffleIntoDeck<T>(AlchemistCard source) where T : CardModel, new()
    {
        if (source.CombatState == null) return;
        var card = source.CombatState.CreateCard<T>(source.Owner);
        if (source.IsUpgraded) CardCmd.Upgrade(card);
        // PreviewCardPileAdd pops the shuffled card up center-screen (the base "shuffle X into your
        // draw pile" feedback) so the addition is visible and the draw-pile count updates on-screen.
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, source.Owner, CardPilePosition.Random));
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
        // Enemies + the casting player only — allies are intentionally excluded so poison doesn't harm them.
        var targets = source.CombatState.Enemies
            .Append(source.Owner.Creature)
            .Where(c => c.IsAlive);
        foreach (var creature in targets)
            await PowerCmd.Apply<PoisonPower>(
                choiceContext, creature, source.DynamicVars.Poison.BaseValue,
                source.Owner.Creature, source);
    }
}
