using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Percolate : AlchemistCard
{
    public Percolate() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("draw", 2, 1);
        WithTip(typeof(DrawCardsNextTurnPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, Owner.Creature,
            DynamicVars["draw"].IntValue, Owner.Creature, this);
    }
}
