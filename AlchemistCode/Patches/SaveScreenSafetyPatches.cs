using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Saves;

namespace Alchemist.AlchemistCode.Patches;

// These guards keep the run-save menu screens usable when a save names a model id that ModelDb cannot
// resolve, such as a card a later version renamed or removed. GetById throws ModelNotFoundException, and
// without a guard the throw propagates and the screen locks. Neither guard is Alchemist-specific: while
// the mod is loaded they cover any absent content. Follows RitsuLib's missing-content menu guards

// RefreshAndSelectRun already logs the failure and shows the out-of-date visual, then rethrows. The
// rethrow travels through TaskHelper.RunSafely and can freeze input, so swallow it once that visual is up
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

// ShowInfo reads the act and the character through GetById with no guard, so a missing model throws on the
// main menu before the player presses Continue. Fall back to the screen's own error panel. The run save is
// left unchanged, and a non-content exception still propagates
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
