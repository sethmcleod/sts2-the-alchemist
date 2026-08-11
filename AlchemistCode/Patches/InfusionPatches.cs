using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Runs;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Patches;

// Infuse enchantments are combat-only, but the base enchantment system is run-permanent, so clear them at combat end
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class InfusionCombatEndPatch
{
    public static void Postfix() => Infusion.ClearCombatInfusions();
}

// Infusion tracks cards in static sets, which outlive a run. Starting or loading a run must drop the
// references from the last one, or combat end works on cards that no longer belong to this run
[HarmonyPatch(typeof(RunManager), nameof(RunManager.Launch))]
public static class InfusionRunLaunchPatch
{
    public static void Prefix() => Infusion.ResetTracking();
}

// This counts every card that any source enchants during combat, so the Masterwork threshold works with
// other mods. The CombatState gate stops a count outside combat, for example at a rest site smith
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
