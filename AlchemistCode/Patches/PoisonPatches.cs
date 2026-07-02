using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Patches;

/// <summary>
/// The base game drives poison's extra triggers AND its lethal-HP prediction off
/// PoisonPower.TriggerCount, which natively only counts the player's Accelerant. Our Fester is a
/// per-enemy, one-turn marker, so fold it into the trigger count on the poisoned creature. This
/// makes the game handle the extra ticks itself (deal current Poison, decrement) and — the point of
/// this patch — makes CalculateTotalDamageNextTurn include Fester, so an enemy that Fester will kill
/// shows green "will die" HP during your turn. Capped at Amount (can't trigger more than the Poison).
/// </summary>
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
