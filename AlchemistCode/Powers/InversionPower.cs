using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class InversionPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Reentrancy guard: our damage can trigger heals that re-enter this hook and loop
    private bool _resolving;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta <= 0 || _resolving || Amount <= 0) return;
        Flash();
        _resolving = true;
        try
        {
            // Re-snapshot the alive enemies each hit so mid-sequence kills are respected
            for (var i = 0; i < Amount; i++)
                foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
                    await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), enemy, delta, ValueProp.Move, Owner, null, null);
        }
        finally
        {
            _resolving = false;
        }
    }
}
