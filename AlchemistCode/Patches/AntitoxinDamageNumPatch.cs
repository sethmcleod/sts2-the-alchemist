using Alchemist.AlchemistCode.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Alchemist.AlchemistCode.Patches;

// A Poison tick that Antitoxin swallowed whole still pops a damage number reading "0". CreatureCmd
// only skips the number on DamageResult.WasFullyBlocked, which is computed as !Unblockable, so a
// Poison tick can never set it.
public static class AntitoxinDamageNumPatch
{
    [HarmonyPatch(typeof(NDamageNumVfx), nameof(NDamageNumVfx.Create),
        [typeof(Creature), typeof(DamageResult)])]
    public static class SkipZero
    {
        public static bool Prefix(Creature target, DamageResult result, ref NDamageNumVfx? __result)
        {
            // AbsorbedThisTurn, not "damage was 0": a 0 from any other source still shows, and the
            // record survives the power being spent down to nothing by this very hit
            if (result.UnblockedDamage > 0 || result.OverkillDamage > 0) return true;
            if (!AntitoxinRules.AbsorbedThisTurn(target)) return true;

            __result = null;
            return false;
        }
    }
}
