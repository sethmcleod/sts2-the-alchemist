using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Patches;

// Infuse enchantments are combat-only, but the base enchantment system is run-permanent, so clear them at combat end
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class InfusionCombatEndPatch
{
    public static void Postfix() => Infusion.ClearCombatInfusions();
}

// Tally every card enchanted during combat, from any source, so Masterwork's threshold is mod-agnostic.
// Gated on CombatState so out-of-combat enchants (e.g. a rest-site smith) don't count
[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Enchant),
    new[] { typeof(EnchantmentModel), typeof(CardModel), typeof(decimal) })]
public static class EnchantCountPatch
{
    public static void Postfix(CardModel card)
    {
        if (card.CombatState != null)
            Infusion.RecordCombatEnchant(card);
    }
}
