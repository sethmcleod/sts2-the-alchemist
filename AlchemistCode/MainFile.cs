using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Alchemist.AlchemistCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Alchemist"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        // Registers this mod's [ScriptPath] Godot node scripts (e.g. AlchemistParticles)
        // so scenes can reference them by res:// path. Required for the custom energy counter VFX.
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        // Patch classes one at a time instead of Harmony.PatchAll(): a single failing patch
        // (e.g. a game update renaming a patched method) then disables only itself — logged
        // loudly — rather than aborting mod init and taking every card/power down with it.
        Harmony harmony = new(ModId);
        foreach (var type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (System.Exception e)
            {
                Logger.Error($"Failed to apply Harmony patch class {type.FullName}: {e}");
            }
        }
    }
}