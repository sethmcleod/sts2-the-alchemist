using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Saves;

namespace Alchemist.AlchemistCode.Patches;

// The rethrow through TaskHelper.RunSafely can freeze input, so it's swallowed once the visual is up
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

// ShowInfo throws on a missing act or character before Continue; fall back to the error panel
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
