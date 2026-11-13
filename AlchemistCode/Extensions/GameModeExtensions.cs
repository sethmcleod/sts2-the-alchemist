using MegaCrit.Sts2.Core.Runs;

namespace Alchemist.AlchemistCode.Extensions;

public static class GameModeExtensions
{
    public static bool IsBlitz(this IRunState runState) => runState.GameMode.ToString() == "Blitz";
}
