using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Potions;
using BaseLib.Config;
using BaseLib.Hooks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models;
using System.Reflection;

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
            // Injecting builds the one instance, and the CustomSingletonModel constructor is what
            // subscribes it for run hooks. Without this the sweeper never runs and Unstable potions
            // would quietly become permanent
            ModelDb.Inject(typeof(Potions.UnstablePotionSweeper));
            ModelDb.Inject(typeof(Powers.AntitoxinCap));
        }
        catch (System.Exception e)
        {
            Logger.Error($"Failed to register the Unstable potion sweeper or the Anti-toxin cap: {e}");
        }

        try
        {
            HealthBarForecastRegistry.Register<Powers.UnstableCompoundForecast>(ModId, "unstable_compound");
        }
        catch (System.Exception e)
        {
            Logger.Error($"Failed to register health bar forecasts: {e}");
        }

        try
        {
            Badges.PotionSaleCounter.Register();
        }
        catch (System.Exception e)
        {
            Logger.Error($"Failed to register the potion sale counter (its badge will never unlock): {e}");
        }

        ModConfigRegistry.Register("The Alchemist", new AlchemistModConfig());
    }

    // A Brew-only potion is out of the Alchemist pool so nothing can generate it, which also makes
    // UnlockState.Potions miss it and the compendium show it as Locked. EventPotionPool is what the base
    // game uses for a potion that is obtainable but never generated, such as Ambergris. See IBrewOnly
    private static void RegisterBrewOnlyPotions()
    {
        foreach (var type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            if (type.IsAbstract || !typeof(IBrewOnly).IsAssignableFrom(type)) continue;
            ModHelper.AddModelToPool(typeof(EventPotionPool), type);
        }
    }
}