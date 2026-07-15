using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Powers;

public class MetabolismPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // Gate to the owner's turn — energy gained on an enemy turn would be wiped by the turn-start refresh
    private bool _ownerTurn = true;
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

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta >= 0 || !_ownerTurn || _triggeredThisTurn) return;
        if (Owner.Player is not { } player) return;
        _triggeredThisTurn = true;
        Flash();
        await PlayerCmd.GainEnergy(Amount, player);
    }
}
