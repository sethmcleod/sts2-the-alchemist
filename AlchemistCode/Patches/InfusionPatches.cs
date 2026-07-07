using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Patches;

// Infuse enchantments are combat-only, but the base enchantment system is run-permanent, so clear them at combat end
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class InfusionCombatEndPatch
{
    public static void Postfix() => Infusion.ClearCombatInfusions();
}
