using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Poultice : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    protected override bool Ferments => true;

    public Poultice() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("antitoxin", 3, 0);
        WithVar("perTurn", 2, 1);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(AntitoxinPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var total = DynamicVars["antitoxin"].IntValue
            + DynamicVars["perTurn"].IntValue * FermentTurns;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner.Creature, total, Owner.Creature, this);
    }
}
