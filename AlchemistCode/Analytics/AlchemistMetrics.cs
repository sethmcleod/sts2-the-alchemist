using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Alchemist.AlchemistCode.Badges;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Epochs;
using Alchemist.AlchemistCode.Relics;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Runs.Metrics;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.GameInfo;

namespace Alchemist.AlchemistCode.Analytics;

// Ships Alchemist run analytics through the game's own ModManager.OnMetricsUpload hook. The game only
// raises it on a release build with the player's "Upload Data" setting on, full console off, the run
// not abandoned, and not their first ever run, and when a run is modded it raises the hook INSTEAD of
// uploading to MegaCrit. So consent and dev-noise filtering are upstream; this adds a mod config
// toggle on top and keeps only Alchemist runs
internal static class AlchemistMetrics
{
    // The vanilla floor threshold below which a run does not count
    private const int RunLengthThreshold = 5;

    // The shape vanilla's MetricUtilities serializes: camelCase, fields included (the metric structs
    // are field-based), ModelId flattened to its entry string
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new ModelIdMetricsConverter() },
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void Initialize()
    {
        ModManager.OnMetricsUpload += OnMetricsUpload;
    }

    private static void OnMetricsUpload(SerializableRun run, bool isVictory, ulong localPlayerId)
    {
        try
        {
            Handle(run, isVictory, localPlayerId);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Failed to build Alchemist run analytics: {e}");
        }
    }

    private static void Handle(SerializableRun run, bool isVictory, ulong localPlayerId)
    {
        if (!AlchemistModConfig.AnalyticsEnabled)
        {
            MainFile.Logger.Info("Alchemist analytics upload skipped: disabled in mod config.");
            return;
        }
        if (run.GameMode != GameMode.Standard) return;

        var localPlayer = run.Players.FirstOrDefault(p => p.NetId == localPlayerId);
        if (localPlayer?.CharacterId == null || localPlayer.CharacterId != ModelDb.Character<Character.Alchemist>().Id)
            return;

        var allPoints = run.MapPointHistory.SelectMany(act => act).ToList();
        if (allPoints.Count < RunLengthThreshold) return;

        // Entry ids must serialize in English whatever language the player runs
        LocManager.Instance.StartOverridingLanguageAsEnglish();
        try
        {
            var metrics = BuildRunMetrics(run, isVictory, localPlayerId, localPlayer, allPoints);
            Upload(run, isVictory, localPlayer, allPoints, metrics);
        }
        finally
        {
            LocManager.Instance.StopOverridingLanguageAsEnglish();
        }
    }

    // Mirrors the payload vanilla builds in MetricUtilities.UploadRunMetricsInternal, so the same
    // downstream tooling reads both
    private static RunMetrics BuildRunMetrics(SerializableRun run, bool isVictory, ulong localPlayerId,
        SerializablePlayer localPlayer, List<MapPointHistoryEntry> allPoints)
    {
        var killedBy = ModelId.none;
        var lastPoint = run.MapPointHistory.LastOrDefault()?.LastOrDefault();
        if (!isVictory && lastPoint != null && lastPoint.Rooms.Last().RoomType.IsCombatRoom())
            killedBy = lastPoint.Rooms.Last().ModelId!;

        var encounters = allPoints
            .Where(e => e.Rooms.Last().RoomType.IsCombatRoom())
            .Select(e => new EncounterMetric(e.Rooms.Last().ModelId!.Entry,
                Math.Min(e.GetEntry(localPlayerId).DamageTaken, localPlayer.MaxHp),
                e.Rooms.Last().TurnsTaken + 1))
            .ToList();
        var cardChoices = allPoints
            .Where(e => e.GetEntry(localPlayerId).CardChoices.Count > 0)
            .Select(e => new CardChoiceMetric(e.GetEntry(localPlayerId).CardChoices))
            .ToList();
        var ancientChoices = allPoints
            .Where(e => e.MapPointType == MapPointType.Ancient && e.GetEntry(localPlayerId).AncientChoices.Count > 0)
            .Select(e => new AncientMetric(e, e.GetEntry(localPlayerId)))
            .ToList();

        List<ActWinMetric> actWins = new();
        List<EventChoiceMetric> eventChoices = new();
        for (var actIndex = 0; actIndex < run.MapPointHistory.Count; actIndex++)
        {
            foreach (var entry in run.MapPointHistory[actIndex])
            {
                if (entry.Rooms.First().RoomType == RoomType.Event
                    && entry.GetEntry(localPlayerId).EventChoices.Count != 0
                    && entry.MapPointType != MapPointType.Ancient)
                    eventChoices.Add(new EventChoiceMetric(entry, localPlayerId, run.Acts[actIndex]));
            }
            var win = actIndex < run.MapPointHistory.Count - 1 || isVictory;
            actWins.Add(new ActWinMetric(run.Acts[actIndex].Id!.Entry, win));
        }

        var progress = SaveManager.Instance.Progress;
        var mine = allPoints.Select(e => e.GetEntry(localPlayerId)).ToList();
        return new RunMetrics
        {
            Ascension = run.Ascension,
            TotalPlaytime = progress.TotalPlaytime,
            TotalWinRate = progress.NumberOfRuns > 0 ? (float)progress.Wins / progress.NumberOfRuns : 0f,
            NumReloads = run.NumReloads,
            BuildId = ReleaseInfoManager.Instance.ReleaseInfo?.Version ?? "NON-RELEASE-VERSION",
            BuildType = PlatformUtil.GetPlatformBranch().ToName(),
            PlayerId = HashPlayerId(progress.UniqueId),
            Character = localPlayer.CharacterId!,
            NumPlayers = run.Players.Count,
            Team = run.Players.Count > 1
                ? run.Players.Select(p => p.CharacterId).OfType<ModelId>().ToList()
                : new List<ModelId>(),
            Win = isVictory,
            FloorReached = allPoints.Count,
            KilledByEncounter = killedBy,
            Deck = localPlayer.Deck.Select(c => c.Id).OfType<ModelId>(),
            Relics = localPlayer.Relics.Select(r => r.Id).OfType<ModelId>(),
            RunPlaytime = run.WinTime > 0 ? run.WinTime : run.RunTime,
            Encounters = encounters,
            CardChoices = cardChoices,
            EventChoices = eventChoices,
            AncientChoices = ancientChoices,
            ActWins = actWins,
            CampfireUpgrades = allPoints.Where(e => e.MapPointType == MapPointType.RestSite)
                .SelectMany(e => e.GetEntry(localPlayerId).UpgradedCards).Select(c => c.Entry).ToList(),
            RelicBuys = mine.SelectMany(s => s.BoughtRelics).Select(r => r.Entry).ToList(),
            PotionBuys = mine.SelectMany(s => s.BoughtPotions).Select(p => p.Entry).ToList(),
            ColorlessBuys = mine.SelectMany(s => s.BoughtColorless).Select(c => c.Entry).ToList(),
            PotionDiscards = mine.SelectMany(s => s.PotionDiscarded).Select(p => p.Entry).ToList(),
        };
    }

    // One Supabase row: promoted columns for cheap SQL, the vanilla-shaped payload as jsonb, and an
    // alchemist object beside it for what vanilla cannot see
    private static void Upload(SerializableRun run, bool isVictory, SerializablePlayer localPlayer,
        List<MapPointHistoryEntry> allPoints, RunMetrics metrics)
    {
        var epochs = ObtainedEpochs();
        JsonObject row = new()
        {
            ["mod_version"] = ModVersion(),
            ["game_version"] = metrics.BuildId,
            ["victory"] = isVictory,
            ["ascension"] = run.Ascension,
            ["floor"] = metrics.FloorReached,
            ["playtime"] = (int)metrics.RunPlaytime,
            ["player_hash"] = metrics.PlayerId,
            ["epochs"] = epochs.Count,
            ["data"] = JsonNode.Parse(JsonSerializer.Serialize(metrics, JsonOptions)),
            ["alchemist"] = new JsonObject
            {
                ["epochs"] = new JsonArray(epochs.Select(id => (JsonNode)id).ToArray()),
                ["potions_sold"] = PotionSaleCounter.CountFor(localPlayer),
                ["brews"] = allPoints.Sum(e => e.GetEntry(localPlayer.NetId).RestSiteChoices
                    .Count(id => id == BrewRestSiteOption.BrewOptionId)),
                ["deck_themes"] = DeckThemes(localPlayer),
                ["config"] = new JsonObject
                {
                    ["enable_epochs"] = AlchemistModConfig.EnableEpochs,
                    ["keep_pools_separate"] = AlchemistModConfig.KeepPoolsSeparate,
                },
            },
        };
        // The hash is logged so you can find your own runs and keep them out of the exports
        MainFile.Logger.Info($"Uploading Alchemist run analytics (player {metrics.PlayerId})...");
        RunMetricsUploader.Upload(row.ToJsonString(), "Alchemist run");
    }

    // The Alchemist epoch ids the player has earned, in timeline order. Two players on the same mod
    // version can have different card pools, so this is what makes their pick rates comparable
    private static List<string> ObtainedEpochs()
    {
        if (!EpochRegistration.Supported) return new List<string>();
        var progress = SaveManager.Instance.Progress;
        return EpochRegistration.AlchemistEpochTypes
            .Select(EpochModel.GetId)
            .Where(progress.IsEpochObtained)
            .ToList();
    }

    // Count of Alchemist cards per theme in the final deck, duplicates included. The dashboard picks
    // the dominant theme from this rather than shipping the rule in the DLL
    private static JsonObject DeckThemes(SerializablePlayer localPlayer)
    {
        Dictionary<CardTheme, int> counts = new();
        foreach (var card in localPlayer.Deck)
        {
            if (card.Id is not { } id || ModelDb.GetByIdOrNull<CardModel>(id) is not AlchemistCard model) continue;
            var attr = (CardThemeAttribute?)Attribute.GetCustomAttribute(model.GetType(), typeof(CardThemeAttribute));
            if (attr == null) continue;
            foreach (var theme in attr.Themes)
            {
                if (theme == CardTheme.None) continue;
                counts[theme] = counts.GetValueOrDefault(theme) + 1;
            }
        }
        JsonObject result = new();
        foreach (var (theme, count) in counts) result[theme.ToString().ToLowerInvariant()] = count;
        return result;
    }

    private static string ModVersion() =>
        ModManager.GetLoadedMods().FirstOrDefault(m => m.manifest?.id == MainFile.ModId)?.manifest?.version ?? "unknown";

    // The game's UniqueId is already an anonymous install id, never the Steam id. Ship only a
    // truncated SHA-256 of it: enough to dedupe and group by player, useless for anything else
    private static string HashPlayerId(string uniqueId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(uniqueId));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }
}
