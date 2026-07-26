using System;
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
        var damage = Math.Floor(delta * Amount / 100m);
        if (damage <= 0) return;
        Flash();
        _resolving = true;
        try
        {
            // Snapshot the living enemies so a mid-sequence kill is respected. Unpowered keeps this out
            // of the attack pipeline, since a percent of a heal must not scale with Strength, Vigor, or
            // Vulnerable. Flame Barrier and Reflect pass the same flag
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
                await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), enemy, damage, ValueProp.Unpowered, Owner, null, null);
        }
        finally
        {
            _resolving = false;
        }
    }
}
