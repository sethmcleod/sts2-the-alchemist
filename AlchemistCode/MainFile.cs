using System.Reflection;
using BaseLib.Config;
using Godot;
using HarmonyLib;
using Alchemist.AlchemistCode.Config;
using MegaCrit.Sts2.Core.Modding;

namespace Alchemist.AlchemistCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Alchemist";
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        // registers mod [ScriptPath] node scripts so scenes resolve them by res:// path
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        // Patch classes one at a time so a single failing patch disables only itself, not the whole mod
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

        try
        {
            Epochs.EpochRegistration.RegisterEpochs();
        }
        catch (System.Exception e)
        {
            Logger.Error($"Failed to register Alchemist epochs (Timeline feature disabled): {e}");
        }

        ModConfigRegistry.Register("The Alchemist", new AlchemistModConfig());
    }
}