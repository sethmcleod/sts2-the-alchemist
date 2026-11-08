using Alchemist.AlchemistCode.Enchantments;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// The game folds enchantment bonuses into EnchantedValue, so a Laced total prints with no highlight
[HarmonyPatch(typeof(DamageVar), nameof(DamageVar.UpdateCardPreview))]
public static class LacedDamagePreviewPatch
{
    public static void Postfix(DamageVar __instance, CardModel card)
    {
        if (card.Enchantment is Laced) __instance.EnchantedValue = __instance.BaseValue;
    }
}
