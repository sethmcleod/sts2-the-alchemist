namespace Alchemist.AlchemistCode.Analytics;

// The website's upload endpoint (site/api/runs.ts). It needs no key: it checks each run and can only
// add runs. Leave it empty to turn uploads off
internal static class AnalyticsEndpoint
{
    public const string RunsUrl = "https://alchemist.fyi/api/runs";

    public static bool IsConfigured => RunsUrl.Length > 0;
}
