using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Vintage : AlchemistCard
{
    protected override bool Ferments => true;

    // The whole payout lands on play, so show it before the card leaves your hand

    public Vintage() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Retain);
        WithKeyword(CardKeyword.Exhaust);
        WithEnergyTip();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var amount = 1 + FermentTurns;
        await PlayerCmd.GainEnergy(amount, Owner);
        await CardPileCmd.Draw(choiceContext, amount, Owner);
    }
}
