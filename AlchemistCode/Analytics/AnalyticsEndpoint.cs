namespace Alchemist.AlchemistCode.Analytics;

internal static class AnalyticsEndpoint
{
    public const string RunsUrl = "https://alchemist.fyi/api/runs";

    public static bool IsConfigured => RunsUrl.Length > 0;
}
