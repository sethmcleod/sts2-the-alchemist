using MegaCrit.Sts2.Core.Runs;

namespace Alchemist.AlchemistCode.Config;

internal static class EffectiveSettings
{
    internal static bool EnableEpochs => AlchemistModConfig.EnableEpochs || InMultiplayerRun;

    internal static bool KeepPoolsSeparate => AlchemistModConfig.KeepPoolsSeparate || InMultiplayerRun;

    private static bool InMultiplayerRun => RunManager.Instance?.DebugOnlyGetState() is { Players.Count: > 1 };
}
