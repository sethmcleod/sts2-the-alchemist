using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Patches;

/// <summary>
/// Infuse enchantments are combat-only, but the base enchantment system is run-permanent. Clear
/// every card this combat's Infusions touched once combat ends (the Hook.AfterCombatEnd dispatcher
/// fires for both victory and defeat).
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class InfusionCombatEndPatch
{
    public static void Postfix() => Infusion.ClearCombatInfusions();
}
