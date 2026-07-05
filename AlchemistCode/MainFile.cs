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

        // Inject the Alchemist's Timeline epochs + story into the base game's registries. Wrapped so a
        // reflection break on a future game update disables only the epoch feature, not the whole mod.
        try
        {
            Epochs.EpochRegistration.RegisterEpochs();
        }
        catch (System.Exception e)
        {
            Logger.Error($"Failed to register Alchemist epochs (Timeline feature disabled): {e}");
        }

        // Mod settings page (BaseLib): testing helpers to unlock/re-lock all Alchemist content.
        // Registered under the display name (not ModId) so the mods list shows "The Alchemist"
        // and sorts it after "BaseLib".
        ModConfigRegistry.Register("The Alchemist", new AlchemistModConfig());
    }
}