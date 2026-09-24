using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Runs.Metrics;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.GameInfo;

namespace Alchemist.AlchemistCode.Analytics;

// The run summary the base game uploads for an unmodded run, built the way
// MetricUtilities.UploadRunMetricsInternal builds it, so one set of tools reads both
internal static class VanillaRunMetrics
{
    // Vanilla's serializer settings: camelCase, fields included, a ModelId written as its entry string
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new ModelIdMetricsConverter() },
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static RunMetrics Build(SerializableRun run, bool isVictory, SerializablePlayer localPlayer,
        List<MapPointHistoryEntry> allPoints)
    {
        var playerId = localPlayer.NetId;
        var killedBy = ModelId.none;
        var lastPoint = run.MapPointHistory.LastOrDefault()?.LastOrDefault();
        if (!isVictory && lastPoint != null && lastPoint.Rooms.Last().RoomType.IsCombatRoom())
            killedBy = lastPoint.Rooms.Last().ModelId!;

        var encounters = allPoints
            .Where(e => e.Rooms.Last().RoomType.IsCombatRoom())
            .Select(e => new EncounterMetric(e.Rooms.Last().ModelId!.Entry,
                Math.Min(e.GetEntry(playerId).DamageTaken, localPlayer.MaxHp),
                e.Rooms.Last().TurnsTaken + 1))
            .ToList();
        var cardChoices = allPoints
            .Where(e => e.GetEntry(playerId).CardChoices.Count > 0)
            .Select(e => new CardChoiceMetric(e.GetEntry(playerId).CardChoices))
            .ToList();
        var ancientChoices = allPoints
            .Where(e => e.MapPointType == MapPointType.Ancient && e.GetEntry(playerId).AncientChoices.Count > 0)
            .Select(e => new AncientMetric(e, e.GetEntry(playerId)))
            .ToList();

        List<ActWinMetric> actWins = new();
        List<EventChoiceMetric> eventChoices = new();
        for (var actIndex = 0; actIndex < run.MapPointHistory.Count; actIndex++)
        {
            foreach (var entry in run.MapPointHistory[actIndex])
            {
                if (entry.Rooms.First().RoomType == RoomType.Event
                    && entry.GetEntry(playerId).EventChoices.Count != 0
                    && entry.MapPointType != MapPointType.Ancient)
                    eventChoices.Add(new EventChoiceMetric(entry, playerId, run.Acts[actIndex]));
            }
            var won = actIndex < run.MapPointHistory.Count - 1 || isVictory;
            actWins.Add(new ActWinMetric(run.Acts[actIndex].Id!.Entry, won));
        }

        var progress = SaveManager.Instance.Progress;
        var mine = allPoints.Select(e => e.GetEntry(playerId)).ToList();
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
                .SelectMany(e => e.GetEntry(playerId).UpgradedCards).Select(c => c.Entry).ToList(),
            RelicBuys = mine.SelectMany(s => s.BoughtRelics).Select(r => r.Entry).ToList(),
            PotionBuys = mine.SelectMany(s => s.BoughtPotions).Select(p => p.Entry).ToList(),
            ColorlessBuys = mine.SelectMany(s => s.BoughtColorless).Select(c => c.Entry).ToList(),
            PotionDiscards = mine.SelectMany(s => s.PotionDiscarded).Select(p => p.Entry).ToList(),
        };
    }

    public static JsonNode? ToJson(RunMetrics metrics) =>
        JsonNode.Parse(JsonSerializer.Serialize(metrics, JsonOptions));

    // UniqueId is an anonymous install id, never the Steam id. Only a truncated hash of it is sent
    private static string HashPlayerId(string uniqueId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(uniqueId));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }
}
