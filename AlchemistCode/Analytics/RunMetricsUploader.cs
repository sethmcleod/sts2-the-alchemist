using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Helpers;

namespace Alchemist.AlchemistCode.Analytics;

// Fire-and-forget JSON POST to a Supabase REST (PostgREST) insert endpoint. A failure is logged and
// dropped: analytics must never touch gameplay, so nothing here awaits, retries, or throws
internal static class RunMetricsUploader
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };

    public static void Upload(string json, string context)
    {
        if (!AnalyticsEndpoint.IsConfigured)
        {
            MainFile.Logger.Info($"Analytics upload for '{context}' skipped: no endpoint configured.");
            return;
        }
        TaskHelper.RunSafely(Post(json, context));
    }

    private static async Task Post(string json, string context)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, AnalyticsEndpoint.RunsUrl);
        request.Headers.Add("apikey", AnalyticsEndpoint.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AnalyticsEndpoint.PublishableKey);
        request.Headers.Add("Prefer", "return=minimal");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await Client.SendAsync(request);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            MainFile.Logger.Warn($"Analytics upload for '{context}' failed (network): {e.Message}");
            return;
        }

        if (response.IsSuccessStatusCode)
        {
            MainFile.Logger.Info($"Analytics for '{context}' uploaded.");
            return;
        }
        var body = await response.Content.ReadAsStringAsync();
        MainFile.Logger.Warn($"Analytics upload for '{context}' failed with {response.StatusCode}: {body}");
    }
}
