using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Windfall : AlchemistCard
{
    public Windfall() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("draw", 1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<WindfallPower>(choiceContext, Owner.Creature,
            DynamicVars["draw"].IntValue, Owner.Creature, this);
    }
}
