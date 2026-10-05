using Alchemist.AlchemistCode.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Alchemist.AlchemistCode.Patches;

[HarmonyPatch(typeof(DustyTome), nameof(DustyTome.SetupForPlayer))]
public static class DustyTomePatches
{
    [HarmonyPriority(Priority.First)]
    public static void Prefix() => CrossMod.RollingDustyTome = true;

    public static void Finalizer() => CrossMod.RollingDustyTome = false;
}
