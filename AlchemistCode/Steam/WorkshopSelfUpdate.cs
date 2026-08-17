using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Transport.Steam;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform.Steam;
using Steamworks;

namespace Alchemist.AlchemistCode.Steam;

// Steam's local Workshop manifest can desync and report a stale install as current, so players keep
// running an old build and report bugs that are already fixed. The game never re-checks. So at boot
// this asks the Workshop servers directly for the item's last-update time (with Steam's cached answer
// disabled), compares it against the local install timestamp, and forces a high-priority re-download
// when the install is behind. New files only load on the next boot, so the player is told to restart
// through the game's own confirmation popup. Adapted from The Witch's port of RitsuLib.
//
// Debug flags: --alchemist-test-update-popup shows the restart popup at once, and
// --alchemist-force-workshop-download[=ITEMID] skips the staleness gate (the id lets a local mods/
// build target the live item, since it has no Workshop path to parse an id from)
internal static class WorkshopSelfUpdate
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StartGrace = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan PopupRetry = TimeSpan.FromSeconds(1);

    private const uint InProgress = (uint)(EItemState.k_EItemStateNeedsUpdate
        | EItemState.k_EItemStateDownloading | EItemState.k_EItemStateDownloadPending);

    private static readonly Regex WorkshopPath = new(@"/workshop/content/\d+/(\d+)(?:/|$)");

    public static void Initialize()
    {
        if (CommandLineHelper.HasArg("alchemist-test-update-popup"))
        {
            MainFile.Logger.Info("[WorkshopSelfUpdate] test flag: showing the restart popup.");
            TaskHelper.RunSafely(ShowRestartPopup());
            return;
        }
        TaskHelper.RunSafely(Run(CommandLineHelper.HasArg("alchemist-force-workshop-download")));
    }

    private static async Task Run(bool force)
    {
        if (!SteamInitializer.Initialized)
        {
            MainFile.Logger.Info("[WorkshopSelfUpdate] Steam is not initialized; skipping.");
            return;
        }

        PublishedFileId_t itemId;
        var forcedId = force ? CommandLineHelper.GetValue("alchemist-force-workshop-download") : null;
        if (!string.IsNullOrWhiteSpace(forcedId) && ulong.TryParse(forcedId, out var parsed))
        {
            itemId = new PublishedFileId_t(parsed);
            MainFile.Logger.Info($"[WorkshopSelfUpdate] Forced item id {parsed} from the command line.");
        }
        else
        {
            // Matched by manifest id rather than assembly, which is spelled differently per game branch
            var mod = ModManager.Mods.FirstOrDefault(m => m.manifest?.id == MainFile.ModId);
            if (mod == null)
            {
                MainFile.Logger.Info("[WorkshopSelfUpdate] Own mod entry not found; skipping.");
                return;
            }
            if (mod.modSource != ModSource.SteamWorkshop)
            {
                MainFile.Logger.Info("[WorkshopSelfUpdate] Not a Workshop install; skipping.");
                return;
            }
            // Workshop installs live at .../steamapps/workshop/content/<appid>/<itemid>/...
            var match = WorkshopPath.Match(mod.path.Replace('\\', '/'));
            if (!match.Success)
            {
                MainFile.Logger.Warn($"[WorkshopSelfUpdate] No Workshop item id in path '{mod.path}'; skipping.");
                return;
            }
            itemId = new PublishedFileId_t(ulong.Parse(match.Groups[1].Value));
        }

        var remoteUpdated = await QueryRemoteUpdateTime(itemId);
        if (remoteUpdated == 0) return; // failure already logged

        var state = SteamUGC.GetItemState(itemId);
        var haveInfo = SteamUGC.GetItemInstallInfo(itemId, out _, out _, 256u, out var localTimestamp);
        var needsUpdate = (state & (uint)EItemState.k_EItemStateNeedsUpdate) != 0;
        var remoteNewer = haveInfo && remoteUpdated > localTimestamp;
        MainFile.Logger.Info($"[WorkshopSelfUpdate] Item {itemId.m_PublishedFileId}: state={state}, local={(haveInfo ? localTimestamp : 0)}, remote={remoteUpdated}.");
        if (!needsUpdate && !remoteNewer && !force)
        {
            MainFile.Logger.Info("[WorkshopSelfUpdate] Install is up to date.");
            return;
        }

        MainFile.Logger.Info("[WorkshopSelfUpdate] Install is stale; requesting a high-priority Workshop download.");
        if (!SteamUGC.DownloadItem(itemId, bHighPriority: true))
        {
            MainFile.Logger.Warn("[WorkshopSelfUpdate] Steam rejected the download request.");
            return;
        }
        if (await WaitForDownload(itemId, localTimestamp))
            await ShowRestartPopup();
    }

    // Server truth. Returns 0 on failure
    private static async Task<uint> QueryRemoteUpdateTime(PublishedFileId_t itemId)
    {
        var query = SteamUGC.CreateQueryUGCDetailsRequest(new[] { itemId }, 1u);
        try
        {
            SteamUGC.SetAllowCachedResponse(query, 0u);
            using SteamCallResult<SteamUGCQueryCompleted_t> call =
                new(SteamUGC.SendQueryUGCRequest(query), SteamInitializer.DisconnectToken);
            var done = await call.Task;
            if (done.m_eResult != EResult.k_EResultOK
                || !SteamUGC.GetQueryUGCResult(done.m_handle, 0u, out var details))
            {
                MainFile.Logger.Warn($"[WorkshopSelfUpdate] Workshop query failed: {done.m_eResult}.");
                return 0;
            }
            return details.m_rtimeUpdated;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"[WorkshopSelfUpdate] Workshop query failed: {e.Message}");
            return 0;
        }
        finally
        {
            SteamUGC.ReleaseQueryUGCRequest(query);
        }
    }

    // Done when the in-progress flags clear AND the install timestamp changed, which is the only proof
    // that new files are on disk. If Steam never starts within the grace period it ignored the request
    private static async Task<bool> WaitForDownload(PublishedFileId_t itemId, uint initialTimestamp)
    {
        var started = DateTime.UtcNow;
        var sawActivity = false;
        while (DateTime.UtcNow - started < DownloadTimeout)
        {
            await Task.Delay(PollInterval, SteamInitializer.DisconnectToken);
            var inProgress = (SteamUGC.GetItemState(itemId) & InProgress) != 0;
            sawActivity |= inProgress;
            SteamUGC.GetItemInstallInfo(itemId, out _, out _, 256u, out var timestamp);
            if (!inProgress && timestamp != initialTimestamp)
            {
                MainFile.Logger.Info($"[WorkshopSelfUpdate] Download complete; install timestamp {initialTimestamp} -> {timestamp}.");
                return true;
            }
            if (!inProgress && !sawActivity && DateTime.UtcNow - started > StartGrace)
            {
                MainFile.Logger.Warn("[WorkshopSelfUpdate] Steam never started the download; giving up.");
                return false;
            }
        }
        MainFile.Logger.Warn("[WorkshopSelfUpdate] Timed out waiting for the Workshop download.");
        return false;
    }

    // LocManager initializes after mods, and NModalContainer can exist before it, so wait on both or
    // the popup renders its placeholder text
    private static async Task ShowRestartPopup()
    {
        while (NModalContainer.Instance == null || LocManager.Instance == null)
            await Task.Delay(PopupRetry, SteamInitializer.DisconnectToken);

        var popup = NGenericPopup.Create();
        if (popup == null) return;
        NModalContainer.Instance.Add(popup);
        var quitNow = await popup.WaitForConfirmation(
            new LocString("settings_ui", "ALCHEMIST-WORKSHOP_UPDATE.body"),
            new LocString("settings_ui", "ALCHEMIST-WORKSHOP_UPDATE.header"),
            new LocString("settings_ui", "ALCHEMIST-WORKSHOP_UPDATE.later"),
            new LocString("settings_ui", "ALCHEMIST-WORKSHOP_UPDATE.quit"));
        if (quitNow)
        {
            MainFile.Logger.Info("[WorkshopSelfUpdate] Player chose to quit and apply the update.");
            NGame.Instance?.Quit();
        }
    }
}
