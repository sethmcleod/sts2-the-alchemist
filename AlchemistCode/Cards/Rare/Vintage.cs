using BaseLib.Extensions;
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
        WithVar(new FermentVar("Draw", 1, 1).WithUpgrade(1));
        WithKeyword(CardKeyword.Retain);
        WithKeyword(CardKeyword.Exhaust);
        WithEnergyTip();
    }

    private int DrawCount => ((FermentVar)DynamicVars["Draw"]).Total(this, null);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, DrawCount, Owner);
    }
}
