using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// With the "any character can sell potions" setting on, the Kit relics no longer uniquely grant selling to
// the Merchant, so their descriptions drop that line. The variant loc key holds the shorter text. Only the
// two Kit relics change; every other relic passes through
[HarmonyPatch(typeof(RelicModel), "DynamicDescription", MethodType.Getter)]
public static class KitRelicTextPatches
{
    public static void Postfix(RelicModel __instance, ref LocString __result)
    {
        if (!AlchemistModConfig.UniversalPotionSelling) return;
        if (__instance is WeatheredKit or GildedKit)
            __result = new LocString("relics", __instance.Id.Entry + ".descriptionUniversalSell");
    }
}
