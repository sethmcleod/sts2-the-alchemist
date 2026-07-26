using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Unlocks;

namespace Alchemist.AlchemistCode.Patches;

// An epoch from an uninstalled mod stays in progress.save, and the JSON load accepts it with only a
// warning. The packet writer does not: WriteEpochId throws for any epoch whose mod is not loaded, and that
// write sits inside CombatManager.EndCombatInternal, so the run locks up for good when the last enemy dies.
//
// GetNetIdForEpochId has no Try* form to test an id first, so drop what it cannot map before the writer
// reads the list. EpochModel.IsValid is the matching test: it reads the same _allEpochs list that builds
// the net id map, mod epochs included. The swap is temporary, so a disabled mod costs no saved progress,
// and a Finalizer restores the real list even if Serialize throws for another reason
[HarmonyPatch(typeof(SerializableUnlockState), nameof(SerializableUnlockState.Serialize))]
public static class UnlockStateSerializationPatches
{
    private static void Prefix(SerializableUnlockState __instance, out List<string>? __state)
    {
        __state = null;

        var epochs = __instance.UnlockedEpochs;
        if (epochs == null || epochs.All(EpochModel.IsValid)) return;

        __state = epochs;
        __instance.UnlockedEpochs = epochs.Where(EpochModel.IsValid).ToList();

        var dropped = epochs.Where(id => !EpochModel.IsValid(id));
        MainFile.Logger.Warn(
            "[Epochs] Omitting unmappable epoch(s) from packet write, owning mod not loaded: "
            + string.Join(", ", dropped));
    }

    // Runs even if Serialize throws, unlike a postfix
    private static void Finalizer(SerializableUnlockState __instance, List<string>? __state)
    {
        if (__state != null) __instance.UnlockedEpochs = __state;
    }
}
