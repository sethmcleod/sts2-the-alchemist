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
        WithEnergy(1, 1);
        WithCards(1, 1);
        WithKeyword(CardKeyword.Retain);
        WithKeyword(CardKeyword.Exhaust);
        WithEnergyTip();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Energy and Cards hold the same number; the csv cross-check in the lint keeps them equal
        var amount = DynamicVars.Energy.IntValue + FermentTurns;
        await PlayerCmd.GainEnergy(amount, Owner);
        await CardPileCmd.Draw(choiceContext, amount, Owner);
    }
}
