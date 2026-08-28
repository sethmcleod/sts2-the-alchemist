using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

[CardTheme(CardTheme.Ferment)]
public class Vintage : AlchemistCard
{
    protected override bool Ferments => true;

    public Vintage() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithVar("Amount", 1, 1);
        WithKeyword(CardKeyword.Retain);
        WithKeyword(CardKeyword.Exhaust);
        WithEnergyTip();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var amount = DynamicVars["Amount"].IntValue + FermentTurns;
        await PlayerCmd.GainEnergy(amount, Owner);
        await CardPileCmd.Draw(choiceContext, amount, Owner);
    }
}
