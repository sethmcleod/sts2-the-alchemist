using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Vintage : AlchemistCard
{
    protected override int FermentPeak => 3;

    // The whole payout lands on play, so show it before the card leaves your hand
    protected override string FermentTotalText =>
        FermentTurns > 0 ? $" (Gains [green]{FermentTurns}[/green] Energy and draws [green]{FermentTurns}[/green].)" : "";

    public Vintage() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Retain);
        WithKeyword(CardKeyword.Exhaust);
        WithEnergyTip();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (FermentTurns <= 0) return;
        await PlayerCmd.GainEnergy(FermentTurns, Owner);
        await CardPileCmd.Draw(choiceContext, FermentTurns, Owner);
    }
}
