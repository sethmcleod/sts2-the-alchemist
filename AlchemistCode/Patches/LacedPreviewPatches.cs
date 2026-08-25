using Alchemist.AlchemistCode.Enchantments;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// A card face prints PreviewValue and colours it by comparing against EnchantedValue. The base game
// folds an enchantment's bonus into EnchantedValue on purpose, because "a card's enchantment is a part
// of the card", so a flat +3 enchantment reads as the card's new number with no highlight.
//
// That is right for a static bonus and wrong for Laced, whose bonus is the live Poison stack. Without
// this, a Laced Spores at 3 base with 2 Poison prints a plain "5" and the player cannot tell it apart
// from a card that simply deals 5. Putting the un-Laced number back into EnchantedValue leaves the
// printed total alone and turns it green, which is what a native dose card like Jab already does: its
// CalculatedDamageVar keeps the calculation base in EnchantedValue and the dosed total in PreviewValue.
[HarmonyPatch(typeof(DamageVar), nameof(DamageVar.UpdateCardPreview))]
public static class LacedDamagePreviewPatch
{
    // A no-op whenever Laced added nothing, because EnchantedValue already equals BaseValue there
    public static void Postfix(DamageVar __instance, CardModel card)
    {
        if (card.Enchantment is Laced) __instance.EnchantedValue = __instance.BaseValue;
    }
}
