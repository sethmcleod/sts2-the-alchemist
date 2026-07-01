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

        Harmony harmony = new(ModId);

        harmony.PatchAll();
    }
}