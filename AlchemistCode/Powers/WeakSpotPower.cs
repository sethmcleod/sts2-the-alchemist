using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// The ModifyDamageMultiplicative override this power needs is spelled differently on the two game
// branches, and an override cannot be routed through a wrapper, so it lives in
// Compat/WeakSpotPowerCompat.cs and calls the branch-agnostic multiplier below
public partial class WeakSpotPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    internal decimal PoisonedAttackMultiplier(Creature? target, ValueProp props)
    {
        // WeakSpot Poisons its own caster, and in multiplayer a Poisoned teammate is not an enemy either
        if (target == null || target == Owner) return 1m;
        if (Owner.CombatState is not { } combat || !combat.GetOpponentsOf(Owner).Contains(target)) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        if (!target.HasPower<PoisonPower>()) return 1m;
        return 1m + Amount / 100m;
    }
}
