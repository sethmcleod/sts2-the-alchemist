using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class DelayedReactionPower : AlchemistPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // The play turn's own end arms the charge; the end of the next player turn detonates it. The
    // flag is transient: a mid-combat reload restarts the delay, which is acceptable
    private bool _armed;

    // The forecast reads this so the health bar preview shows only on the turn the hit will land, not
    // the turn it is applied
    internal bool IsArmed => _armed;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // The power sits on an enemy, so track the APPLIER's turn ends, not the owner's
        if (Applier == null || !participants.Contains(Applier)) return;
        if (!_armed)
        {
            _armed = true;
            return;
        }
        Flash();
        // Unpowered: the number on the card is the number dealt (the Inversion precedent). Block applies
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, Amount, ValueProp.Unpowered, Applier, null, null);
        await PowerCmd.Remove(this);
    }
}
