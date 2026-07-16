using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Unlocks;

namespace Alchemist.AlchemistCode.Patches;

// Base-game hardening, not Alchemist-specific — worth upstreaming to BaseLib.
//
// An epoch belonging to an uninstalled mod stays in progress.save (JSON tolerates unknown IDs and only
// warns) and is seeded into every run's unlock state. Packet serialization is not tolerant: WriteEpochId
// -> ModelIdSerializationCache.GetNetIdForEpochId throws for any epoch whose owning mod is not loaded.
// That write happens inside CombatManager.EndCombatInternal (replay write), so the throw aborts combat
// teardown and the run wedges in an unrecoverable post-combat state the moment the last enemy dies.
//
// GetNetIdForEpochId has no Try* variant to probe with, unlike the category and entry maps, so drop
// unmappable epochs before the writer sees them. EpochModel.IsValid is the right predicate: it reads
// AllEpochIds, which is derived from the same _allEpochs list ModelIdSerializationCache.Init() builds
// its net ID map from, so it accepts exactly what the writer can encode (mod epochs included, since
// EpochRegistration adds to _allEpochs and busts the AllEpochIds cache at load).
//
// The filtered list is swapped in only for the duration of the write, then restored, so a disabled mod
// never costs the player saved unlock progress — re-installing it restores the epoch. Only the ephemeral
// replay/network packet omits it. Restoring from a Finalizer rather than a Postfix means the real list
// comes back even if Serialize throws for an unrelated reason.
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
