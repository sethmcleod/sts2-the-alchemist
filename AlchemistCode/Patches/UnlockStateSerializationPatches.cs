using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Unlocks;

namespace Alchemist.AlchemistCode.Patches;

// WriteEpochId throws for an unloaded mod's epoch inside EndCombatInternal and locks the run
// The list swap is temporary, so a disabled mod's progress survives
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

    private static void Finalizer(SerializableUnlockState __instance, List<string>? __state)
    {
        if (__state != null) __instance.UnlockedEpochs = __state;
    }
}
