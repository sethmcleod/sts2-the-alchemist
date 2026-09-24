namespace Alchemist.AlchemistCode.Analytics;

// The publishable key is safe to ship: row level security lets it insert rows and do nothing else.
// Leave both empty to turn uploads off
internal static class AnalyticsEndpoint
{
    public const string RunsUrl = "https://qgvpsvjvgpfweeouufbk.supabase.co/rest/v1/runs";
    public const string PublishableKey = "sb_publishable_G3cTYhJsFsjVWS-f9fdRXQ_pzPosKUr";

    public static bool IsConfigured => RunsUrl.Length > 0 && PublishableKey.Length > 0;
}
