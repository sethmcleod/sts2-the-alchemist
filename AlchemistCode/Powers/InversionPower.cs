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
        // The amount is a percent of the heal. Round down, and skip a heal too small to convert
        var damage = Math.Floor(delta * Amount / 100m);
        if (damage <= 0) return;
        Flash();
        _resolving = true;
        try
        {
            // Snapshot the alive enemies so a mid-sequence kill is respected. Unpowered keeps this out of
            // the attack pipeline: the damage is a percent of the heal, so Strength, Vigor, and Vulnerable
            // must not change it. The base game reactive powers, Flame Barrier and Reflect, pass the same
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive).ToList())
                await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), enemy, damage, ValueProp.Unpowered, Owner, null, null);
        }
        finally
        {
            _resolving = false;
        }
    }
}
