using System.ComponentModel;
using System.Reflection;
using HarmonyLib;

namespace Alchemist.AlchemistCode.Patches;

internal static class LinuxRecompile
{
    [ThreadStatic] public static nint WritableCodeCopy;

    public static readonly MethodBase? RecompileHandler =
        Find("MonoMod.Core.Platforms.PlatformTripleDetourFactory+Detour+ManagedDetourBox", "OnMethodCompiled");

    public static readonly MethodBase? ProtectStep =
        Find("MonoMod.Core.Platforms.Systems.LinuxSystem", "ProtectRWX");

    private static int _patchesKept;

    public static void ReportPatchKept(Win32Exception refusal)
    {
        if (Interlocked.Increment(ref _patchesKept) != 1) return;
        MainFile.Logger.Info($"[LinuxRecompile] The system refused to change the protection of recompiled code "
            + $"(error {refusal.NativeErrorCode}). The Harmony patch was written through the writable copy and "
            + "stays active. This message is logged one time.");
    }

    private static MethodBase? Find(string typeName, string methodName)
    {
        if (!OperatingSystem.IsLinux()) return null;

        var method = AccessTools.Method(typeof(Harmony).Assembly.GetType(typeName), methodName);
        if (method == null)
            MainFile.Logger.Warn($"[LinuxRecompile] {typeName}.{methodName} is not in this build of Harmony, "
                + "so a Harmony patch can stop working after the game recompiles its method.");
        return method;
    }
}

[HarmonyPatch]
public static class LinuxRecompileHandlerPatch
{
    private static bool Prepare() => LinuxRecompile.RecompileHandler != null;

    private static MethodBase? TargetMethod() => LinuxRecompile.RecompileHandler;

    private static void Prefix(nint codeStart, nint codeStartRw, out nint __state)
    {
        __state = LinuxRecompile.WritableCodeCopy;
        LinuxRecompile.WritableCodeCopy = codeStartRw != codeStart ? codeStartRw : 0;
    }

    private static void Finalizer(nint __state) => LinuxRecompile.WritableCodeCopy = __state;
}

[HarmonyPatch]
public static class LinuxRecompileProtectPatch
{
    private static bool Prepare() => LinuxRecompile.ProtectStep != null;

    private static MethodBase? TargetMethod() => LinuxRecompile.ProtectStep;

    private static void Prefix(nint addr, out nint __state) => __state = addr;

    private static Exception? Finalizer(Exception? __exception, nint __state)
    {
        if (__exception is not Win32Exception refusal) return __exception;
        if (__state == 0 || __state != LinuxRecompile.WritableCodeCopy) return __exception;

        LinuxRecompile.ReportPatchKept(refusal);
        return null;
    }
}
