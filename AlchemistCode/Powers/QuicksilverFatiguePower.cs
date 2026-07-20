using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Powers;

// Applied by Quicksilver Draught at Amount 2. The end of the turn the potion was drunk on
// takes one stack, so the last stack is live exactly during the extra turn: it blocks the
// start-of-turn hand draw there, then clears at that turn's end. Invisible, like the base
// game's extra-turn counter
public class QuicksilverFatiguePower : AlchemistPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override bool IsVisibleInternal => false;

    public override bool ShouldDraw(Player player, bool fromHandDraw)
    {
        if (!fromHandDraw || player != Owner.Player) return true;
        return Amount > 1;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        if (Amount > 1)
            await PowerCmd.Decrement(this);
        else
            await PowerCmd.Remove(this);
    }
}
