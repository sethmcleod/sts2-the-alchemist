using Alchemist.AlchemistCode.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace Alchemist.AlchemistCode.Patches;

// A Poison tick that Antitoxin swallowed whole still pops a damage number, and it reads "0".
//
// The base game only skips the number when damage was fully blocked, and Poison is Unblockable, so
// that path never applies here. The Antitoxin power icon already flashes as it is spent, which is the
// feedback the moment wants; the 0 on top of it just looks like a bug.
public static class AntitoxinDamageNumPatch
{
    [HarmonyPatch(typeof(NDamageNumVfx), nameof(NDamageNumVfx.Create),
        [typeof(Creature), typeof(DamageResult)])]
    public static class SkipZero
    {
        public static bool Prefix(Creature target, DamageResult result, ref NDamageNumVfx? __result)
        {
            // AbsorbedThisTurn keeps this narrow: a 0 from any other source still shows, and the
            // record survives the power being spent down to nothing
            if (result.UnblockedDamage > 0 || result.OverkillDamage > 0) return true;
            if (!AntitoxinRules.AbsorbedThisTurn(target)) return true;

            __result = null;
            return false;
        }
    }
}
