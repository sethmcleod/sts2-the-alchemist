namespace Alchemist.AlchemistCode.Analytics;

// Where run analytics go. The key is Supabase's publishable (anon) key. It is meant to ship in the
// DLL: row level security limits it to INSERT on the runs table and nothing else. Leave both empty to
// disable uploads without touching any other code
internal static class AnalyticsEndpoint
{
    public const string RunsUrl = "https://qgvpsvjvgpfweeouufbk.supabase.co/rest/v1/runs";
    public const string PublishableKey = "sb_publishable_G3cTYhJsFsjVWS-f9fdRXQ_pzPosKUr";

    public static bool IsConfigured => RunsUrl.Length > 0 && PublishableKey.Length > 0;
}
