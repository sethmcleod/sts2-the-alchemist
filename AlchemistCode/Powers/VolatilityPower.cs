using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class VolatilityPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Transient per-turn flags: the card reads "the first time your HP changes on each of your
    // turns", so the trigger must be gated to the owner's own turn — AfterCurrentHpChanged also
    // fires on enemy turns (e.g. taking attack damage) and would otherwise trigger there.
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

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || !_ownerTurn || _triggeredThisTurn) return;
        _triggeredThisTurn = true;
        Flash();
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), enemy, Amount, Owner, null);
    }
}
