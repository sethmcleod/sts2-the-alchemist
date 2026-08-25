using System;
using System.Reflection;
using Alchemist.AlchemistCode.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Patches;

// The two game branches spell the Poison tick differently. The public build runs the whole loop inline
// in PoisonPower.AfterSideTurnStart; the beta build factored it out into Trigger(), which the public
// build has no member of at all (nor TriggerCount). Neither name can be written at compile time,
// because the project leaves the publicizer off and each branch is missing the other's member, so
// naming one is a build break on the other. Resolving the target at load time keeps a single patch
// correct on both, and it needs no Compat/ file, so there is nothing for the merge driver to carry.
//
// Skipping AfterSideTurnStart is equivalent to skipping Trigger: the whole body is the Poison tick, and
// on a side turn the owner is not part of, it does nothing to skip
[HarmonyPatch]
public static class NumbPatches
{
    private static MethodBase TargetMethod() =>
        AccessTools.DeclaredMethod(typeof(PoisonPower), "Trigger")
        ?? AccessTools.DeclaredMethod(typeof(PoisonPower), "AfterSideTurnStart")
        ?? throw new MissingMethodException(nameof(PoisonPower), "Trigger or AfterSideTurnStart");

    // __result must be set: skipping an async method leaves the caller awaiting a null Task
    public static bool Prefix(PoisonPower __instance, ref Task __result)
    {
        if (__instance.Owner?.GetPowerAmount<NumbPower>() is not > 0) return true;
        __result = Task.CompletedTask;
        return false;
    }
}
