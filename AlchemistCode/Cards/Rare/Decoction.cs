using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Decoction : AlchemistCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Enchanted;

    public Decoction() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var reacted = ReactionActive;

        // The built-in exhaust prompt, because the card's own SelectionScreenPrompt getter throws
        // without a per-card loc key
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            null, this);
        foreach (var card in selected)
            await CardCmd.Exhaust(choiceContext, card);

        await PotionCmd.TryToProcure(
            PotionFactory.CreateRandomPotionInCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable(),
            Owner);

        if (reacted)
            await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 1);
    }
}
