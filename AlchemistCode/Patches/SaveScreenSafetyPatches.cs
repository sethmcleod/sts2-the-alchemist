using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Saves;

namespace Alchemist.AlchemistCode.Patches;

// These guards keep the run-save menu screens usable when a save names a model id that ModelDb cannot
// resolve. The Alchemist card list changes between versions, so an old run in the history can name a card
// that a later version renamed or removed. The base game reads that id through ModelDb.GetById, which throws
// ModelNotFoundException. Without a guard the throw propagates and the screen locks. Both guards run only
// while this mod is loaded, so they protect the player against any absent content, not the Alchemist alone.
// Neither guard is Alchemist-specific, so both are good contributions to BaseLib. See
// UnlockStateSerializationPatches for the same idea on the combat replay writer. The pattern follows
// RitsuLib's missing-content menu guards

// NRunHistory.RefreshAndSelectRun is synchronous. Its own catch already logs the failure and shows the
// out-of-date visual, then it rethrows. The rethrow travels through TaskHelper.RunSafely and can freeze
// input. Swallow it once the vanilla error visual is up
[HarmonyPatch(typeof(NRunHistory), "RefreshAndSelectRun", typeof(int))]
public static class RunHistoryLoadSafetyPatch
{
    [HarmonyFinalizer]
    private static Exception? SuppressAfterErrorVisual(Exception? __exception)
    {
        if (__exception == null)
            return null;
        MainFile.Logger.Warn(
            "[SaveSafety] Run history load failed after the vanilla error visual; suppressed to keep the menu "
            + "usable: " + __exception.Message);
        return null;
    }
}

// NContinueRunInfo.ShowInfo reads the act and the character through ModelDb.GetById with no guard, so a
// missing model throws on the main menu before the player presses Continue. Fall back to the same error
// panel that the screen shows for a failed save read. The run save stays on disk and is not changed. A
// non-content exception still propagates
[HarmonyPatch(typeof(NContinueRunInfo), "ShowInfo", typeof(SerializableRun))]
public static class ContinueRunPreviewSafetyPatch
{
    private static readonly Action<NContinueRunInfo> ShowError =
        AccessTools.MethodDelegate<Action<NContinueRunInfo>>(
            AccessTools.DeclaredMethod(typeof(NContinueRunInfo), "ShowError"));

    [HarmonyFinalizer]
    private static Exception? RecoverFromMissingModel(Exception? __exception, NContinueRunInfo __instance)
    {
        if (__exception is not ModelNotFoundException missing)
            return __exception;
        MainFile.Logger.Warn(
            "[SaveSafety] Continue-run preview names a model that is not in ModelDb; showed the error panel, "
            + "run save left unchanged: " + missing.Message);
        ShowError(__instance);
        return null;
    }
}
