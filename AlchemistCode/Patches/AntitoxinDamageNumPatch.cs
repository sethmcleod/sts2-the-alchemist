using Alchemist.AlchemistCode.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Alchemist.AlchemistCode.Patches;

// WasFullyBlocked is never set for Poison, so a tick Antitoxin absorbed still shows a 0
public static class AntitoxinDamageNumPatch
{
    [HarmonyPatch(typeof(NDamageNumVfx), nameof(NDamageNumVfx.Create),
        [typeof(Creature), typeof(DamageResult)])]
    public static class SkipZero
    {
        public static bool Prefix(Creature target, DamageResult result, ref NDamageNumVfx? __result)
        {
            // Not a check for 0 damage: other 0s still show, and this hit may spend the power to nothing
            if (result.UnblockedDamage > 0 || result.OverkillDamage > 0) return true;
            if (!AntitoxinRules.AbsorbedThisTurn(target)) return true;

            __result = null;
            return false;
        }
    }
}
