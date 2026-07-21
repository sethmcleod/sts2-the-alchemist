using System;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Powers;
using Alchemist.AlchemistCode.Relics;

namespace Alchemist.AlchemistCode.Patches;

// Fold Fester into TriggerCount (capped at the Poison stack). The base game drives both the extra ticks and the
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

// Fold Glowing Shard into TriggerCount, the same way the base game folds Accelerant, but without a power on
// the player. An enemy's Poison triggers one more time for each opposing player that holds the relic. Reading
// the relic here, instead of giving the player an Accelerant power, keeps the Accelerant icon off the player
// and an ally, and it drives both the extra ticks and the lethal-HP preview like Fester above. The cap
// composes with the Fester postfix in either order, because a nested Min never exceeds the Poison stack
[HarmonyPatch(typeof(PoisonPower), "TriggerCount", MethodType.Getter)]
public static class GlowingShardPoisonTriggerPatch
{
    public static void Postfix(PoisonPower __instance, ref int __result)
    {
        if (__instance.Owner is not { CombatState: { } combat } poisoned) return;
        var shards = combat.GetOpponentsOf(poisoned)
            .Count(c => c.IsAlive && c.Player?.GetRelic<GlowingShard>() != null);
        if (shards > 0)
            __result = Math.Min(__instance.Amount, __result + shards);
    }
}
