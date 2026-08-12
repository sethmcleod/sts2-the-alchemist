using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

// Holds the Anti-toxin ceiling for everyone in combat.
//
// This cannot live on AntitoxinPower itself. TryModifyPowerAmountReceived is broadcast to the models
// that are actually in combat, so a cap written on the power would not exist the first time a creature
// gains Anti-toxin from zero, and that first grant would go uncapped. A combat-hook singleton is always
// listening, so every grant is clamped, including the first one.
public sealed class AntitoxinCap() : CustomSingletonModel(HookType.Combat)
{
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target,
        decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not AntitoxinPower || amount <= 0) return false;

        var room = AntitoxinPower.MaxFor(target) - target.GetPowerAmount<AntitoxinPower>();
        if (amount <= room) return false;

        modifiedAmount = Math.Max(0, room);
        return true;
    }
}
