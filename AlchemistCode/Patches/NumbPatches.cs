using Alchemist.AlchemistCode.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Patches;

[HarmonyPatch(typeof(PoisonPower), nameof(PoisonPower.Trigger))]
public static class NumbPatches
{
    // __result must be set: skipping an async method leaves the caller awaiting a null Task
    public static bool Prefix(PoisonPower __instance, ref Task __result)
    {
        if (__instance.Owner?.GetPowerAmount<NumbPower>() is not > 0) return true;
        __result = Task.CompletedTask;
        return false;
    }
}
