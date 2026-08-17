using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment, CardTheme.Antitoxin)]
public class Immunize : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    protected override bool Ferments => true;

    public Immunize() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Amount", 1, 1);
        WithVar("perTurn", 1, 0);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(AntitoxinPower));
    }


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var stacks = DynamicVars["Amount"].IntValue
            + DynamicVars["perTurn"].IntValue * FermentTurns;
        await PowerCmd.Apply<ImmunizePower>(choiceContext, Owner.Creature, stacks, Owner.Creature, this);
    }
}
