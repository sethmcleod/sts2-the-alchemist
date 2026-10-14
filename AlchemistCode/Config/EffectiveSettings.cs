using MegaCrit.Sts2.Core.Runs;

namespace Alchemist.AlchemistCode.Config;

internal static class EffectiveSettings
{
    internal static bool EnableEpochs => EnableEpochsFor(InMultiplayerRun);

    internal static bool KeepPoolsSeparate => KeepPoolsSeparateFor(InMultiplayerRun);

    internal static bool EnableEpochsFor(bool multiplayer) => AlchemistModConfig.EnableEpochs || multiplayer;

    internal static bool KeepPoolsSeparateFor(bool multiplayer) => AlchemistModConfig.KeepPoolsSeparate || multiplayer;

    private static bool InMultiplayerRun => RunManager.Instance?.DebugOnlyGetState() is { Players.Count: > 1 };
}
