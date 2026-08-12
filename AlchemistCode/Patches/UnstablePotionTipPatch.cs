using Alchemist.AlchemistCode.Potions;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// The mark lives outside the potion model, so the potion cannot carry this tip on its own.
public static class UnstablePotionTipPatch
{
    [HarmonyPatch(typeof(PotionModel), "get_HoverTips")]
    public static class UnstableTip
    {
        public static void Postfix(PotionModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            // Canonical potions have no Owner and cannot be marked, and reading them throws
            if (!__instance.IsMutable || !UnstablePotions.IsUnstable(__instance)) return;
            __result = __result.Append(UnstablePotions.Tip);
        }
    }
}
