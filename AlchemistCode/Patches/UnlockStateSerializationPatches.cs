using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Unlocks;

namespace Alchemist.AlchemistCode.Patches;

// This makes the base game safe. It is not Alchemist-specific, so it is a good contribution to BaseLib.
//
// An epoch from an uninstalled mod stays in progress.save. The JSON load accepts an unknown id and gives
// only a warning. The game then puts that epoch into the unlock state of every run. The packet
// serialization does not accept it. WriteEpochId calls ModelIdSerializationCache.GetNetIdForEpochId,
// which throws for any epoch whose mod is not loaded.
//
// That write happens inside CombatManager.EndCombatInternal, which is the replay write. The exception
// stops the combat teardown. The run then stops permanently when the last enemy dies.
//
// GetNetIdForEpochId has no Try* method to test an id first, unlike the category and entry maps.
// Therefore remove the epochs that it cannot map before the writer reads the list. EpochModel.IsValid is
// the correct test. It reads AllEpochIds, which comes from the same _allEpochs list that
// ModelIdSerializationCache.Init() uses for its net id map. It accepts exactly what the writer can
// encode. This includes mod epochs, because EpochRegistration adds to _allEpochs and clears the
// AllEpochIds cache at load.
//
// The filtered list replaces the real list only for the write, then the code restores it. A disabled mod
// therefore never costs the player any saved unlock progress. If you install the mod again, the epoch
// returns. Only the temporary replay packet omits it. A Finalizer restores the list, not a Postfix, so
// the real list returns even if Serialize throws for another reason
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
