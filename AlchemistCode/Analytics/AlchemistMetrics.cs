using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Alchemist.AlchemistCode.Badges;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Epochs;
using Alchemist.AlchemistCode.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Runs.Metrics;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;

namespace Alchemist.AlchemistCode.Analytics;

// The game raises ModManager.OnMetricsUpload in place of its own upload for a modded run, and only
// when the player allows uploads. This adds the mod's own switch, keeps Alchemist runs, and sends
// one row: the vanilla summary plus the Alchemist counters
internal static class AlchemistMetrics
{
    // Vanilla skips runs shorter than this many floors
    private const int RunLengthThreshold = 5;
    // Bumped when a counter is added or a key changes meaning. The export reads it to re-key older
    // rows, and to tell a counter this client does not send from a zero
    private const int Schema = 3;

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

        // Entry ids must serialize in English whatever language the player uses
        LocManager.Instance.StartOverridingLanguageAsEnglish();
        try
        {
            var metrics = VanillaRunMetrics.Build(run, isVictory, localPlayer, allPoints);
            var row = Row(run, isVictory, metrics, Payload(run, isVictory, localPlayer, allPoints));
            // Logged so a player can find their own runs and keep them out of the export
            MainFile.Logger.Info($"Uploading Alchemist run analytics (player {metrics.PlayerId})...");
            RunMetricsUploader.Upload(row.ToJsonString(), "Alchemist run");
        }
        finally
        {
            LocManager.Instance.StopOverridingLanguageAsEnglish();
        }
    }

    // The columns match tools/analytics/schema.sql. The promoted ones are what the export filters on
    private static JsonObject Row(SerializableRun run, bool isVictory, RunMetrics metrics, JsonObject payload) => new()
    {
        ["mod_version"] = ModVersion(),
        ["game_version"] = metrics.BuildId,
        ["victory"] = isVictory,
        ["ascension"] = run.Ascension,
        ["floor"] = metrics.FloorReached,
        ["playtime"] = (int)metrics.RunPlaytime,
        ["player_hash"] = metrics.PlayerId,
        ["epochs"] = payload["epochs"]!.AsArray().Count,
        ["data"] = VanillaRunMetrics.ToJson(metrics),
        ["alchemist"] = payload,
    };

    // What the vanilla summary cannot see
    private static JsonObject Payload(SerializableRun run, bool isVictory, SerializablePlayer player,
        List<MapPointHistoryEntry> allPoints) => new()
    {
        ["epochs"] = new JsonArray(ObtainedEpochs().Select(id => (JsonNode)id).ToArray()),
        ["potions_sold"] = PotionSaleCounter.CountFor(player),
        ["potions_used"] = new JsonArray(allPoints
            .SelectMany(e => e.GetEntry(player.NetId).PotionUsed)
            .Select(id => (JsonNode)id.Entry).ToArray()),
        ["brews"] = allPoints.Sum(e => e.GetEntry(player.NetId).RestSiteChoices
            .Count(id => id == BrewRestSiteOption.BrewOptionId)),
        ["deck_themes"] = DeckThemes(player),
        ["schema"] = Schema,
        ["mixes"] = new JsonObject
        {
            ["bursting"] = RunCounters.CountFor(player, RunCounters.MixBursting),
            ["fuming"] = RunCounters.CountFor(player, RunCounters.MixFuming),
            ["syrupy"] = RunCounters.CountFor(player, RunCounters.MixSyrupy),
            ["zesty"] = RunCounters.CountFor(player, RunCounters.MixZesty),
            ["acrid"] = RunCounters.CountFor(player, RunCounters.MixAcrid),
            ["sparkling"] = RunCounters.CountFor(player, RunCounters.MixSparkling),
            ["compound"] = RunCounters.CountFor(player, RunCounters.MixCompound),
        },
        ["poison"] = new JsonObject
        {
            ["gained"] = RunCounters.CountFor(player, RunCounters.PoisonGained),
            ["absorbed"] = RunCounters.CountFor(player, RunCounters.PoisonAbsorbed),
            ["bled"] = RunCounters.CountFor(player, RunCounters.PoisonBled),
            ["peak"] = RunCounters.CountFor(player, RunCounters.PoisonPeak),
        },
        ["antitoxin"] = new JsonObject
        {
            ["peak"] = RunCounters.CountFor(player, RunCounters.AntitoxinPeak),
        },
        ["badges"] = BadgesJson(run, isVictory, player),
        ["tally"] = TallyJson(player),
        ["acts"] = ActsJson(run, player),
        ["config"] = new JsonObject
        {
            ["enable_epochs"] = AlchemistModConfig.EnableEpochs,
            ["keep_pools_separate"] = AlchemistModConfig.KeepPoolsSeparate,
        },
    };

    // Every badge this mod adds, with the tier the run earned or "none". This is ScoreUtility.GetBadges'
    // rule, applied through CustomBadge because GetBadges' signature differs between the game's branches
    // Null on any failure, so the row still uploads and the export falls back to the thresholds
    private static JsonObject? BadgesJson(SerializableRun run, bool isVictory, SerializablePlayer player)
    {
        try
        {
            var badges = new JsonObject();
            foreach (var type in typeof(AlchemistMetrics).Assembly.GetTypes())
            {
                if (type.IsAbstract || !type.IsSubclassOf(typeof(CustomBadge))) continue;
                var badge = (CustomBadge)Activator.CreateInstance(type)!;
                var earned = (!badge.RequiresWin || isVictory)
                             && (!badge.MultiplayerOnly || run.Players.Count != 1)
                             && badge.IsObtained(run, player);
                var tier = earned ? badge.Rarity(run, player) : BadgeRarity.None;
                badges[badge.Id] = tier.ToString().ToLowerInvariant();
            }
            return badges;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Could not read the run's badges for analytics: {e.Message}");
            return null;
        }
    }

    private static JsonObject TallyJson(SerializablePlayer player)
    {
        var tally = new JsonObject();
        foreach (var (key, count) in RunCounters.TallyFor(player).OrderBy(kv => kv.Key))
            tally[key] = count;
        return tally;
    }

    // Turns follow the vanilla convention of TurnsTaken + 1
    private static JsonArray ActsJson(SerializableRun run, SerializablePlayer player)
    {
        var acts = new JsonArray();
        for (var actIndex = 0; actIndex < run.MapPointHistory.Count; actIndex++)
        {
            var fights = run.MapPointHistory[actIndex]
                .Where(e => e.Rooms.Last().RoomType.IsCombatRoom()).ToList();
            acts.Add(new JsonObject
            {
                ["act"] = actIndex + 1,
                ["fights"] = fights.Count,
                ["turns"] = fights.Sum(e => e.Rooms.Last().TurnsTaken + 1),
                ["damage"] = fights.Sum(e => Math.Min(e.GetEntry(player.NetId).DamageTaken, player.MaxHp)),
            });
        }
        return acts;
    }

    // In timeline order
    private static List<string> ObtainedEpochs()
    {
        if (!EpochRegistration.Supported) return new List<string>();
        var progress = SaveManager.Instance.Progress;
        return EpochRegistration.AlchemistEpochTypes
            .Select(EpochModel.GetId)
            .Where(progress.IsEpochObtained)
            .ToList();
    }

    // Alchemist cards per theme in the final deck, duplicates included. The export picks the
    // dominant theme from these counts
    private static JsonObject DeckThemes(SerializablePlayer player)
    {
        Dictionary<CardTheme, int> counts = new();
        foreach (var card in player.Deck)
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
}
