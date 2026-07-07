using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class CatalysePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Gate to the owner's turn — AfterCurrentHpChanged also fires on enemy turns
    private bool _ownerTurn;
    private bool _triggeredThisTurn;

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner))
        {
            _ownerTurn = true;
            _triggeredThisTurn = false;
        }
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            _ownerTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || !_ownerTurn || _triggeredThisTurn) return Task.CompletedTask;
        if (Owner.Player is not { } player) return Task.CompletedTask;
        _triggeredThisTurn = true;
        Flash();
        Infusion.InfuseRandomFromHand(player, Amount);
        return Task.CompletedTask;
    }
}
