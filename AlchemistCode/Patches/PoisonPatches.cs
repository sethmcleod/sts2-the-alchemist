using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Patches;

// Fold Fester into TriggerCount (capped at Amount). The base game drives both the extra ticks and the
// lethal-HP preview off this, so an enemy that Fester will kill shows green "will die" HP
[HarmonyPatch(typeof(PoisonPower), "TriggerCount", MethodType.Getter)]
public static class FesterPoisonTriggerPatch
{
    public static void Postfix(PoisonPower __instance, ref int __result)
    {
        var fester = __instance.Owner?.GetPowerAmount<FesterPower>() ?? 0;
        if (fester > 0)
            __result = Math.Min(__instance.Amount, __result + fester);
    }
}
