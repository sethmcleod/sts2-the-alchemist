using System.Reflection;
using BaseLib.Config;
using BaseLib.Hooks;
using Godot;
using HarmonyLib;
using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Potions;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.PotionPools;

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
        // Registers mod [ScriptPath] node scripts so scenes resolve them by res:// path
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        // Patch the classes one at a time. A patch that fails then disables only itself, not the whole mod
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
            RegisterBrewOnlyPotions();
        }
        catch (System.Exception e)
        {
            Logger.Error($"Failed to register Brew-only potions (they will show as Locked): {e}");
        }

        try
        {
            Epochs.EpochRegistration.RegisterEpochs();
        }
        catch (System.Exception e)
        {
            Logger.Error($"Failed to register Alchemist epochs (Timeline feature disabled): {e}");
        }

        try
        {
            // Preview Delayed Reaction's pending damage on the enemy health bar
            HealthBarForecastRegistry.Register<Powers.DelayedReactionForecast>(ModId, "delayed_reaction");
        }
        catch (System.Exception e)
        {
            Logger.Error($"Failed to register health bar forecasts: {e}");
        }

        ModConfigRegistry.Register("The Alchemist", new AlchemistModConfig());
    }

    // A Brew-only potion is kept out of the Alchemist potion pool so that nothing can generate it.
    // That also makes UnlockState.Potions miss it, and the compendium then shows it as Locked.
    // EventPotionPool is the pool the base game uses for a potion that is obtainable but never
    // generated, such as Ambergris. No generation path reads it. See IBrewOnly
    private static void RegisterBrewOnlyPotions()
    {
        foreach (var type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            if (type.IsAbstract || !typeof(IBrewOnly).IsAssignableFrom(type)) continue;
            ModHelper.AddModelToPool(typeof(EventPotionPool), type);
        }
    }
}