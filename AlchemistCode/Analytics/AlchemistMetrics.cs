using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Alchemist.AlchemistCode.Badges;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Cards.AncientsAwakened;
using Alchemist.AlchemistCode.Cards.Hero;
using Alchemist.AlchemistCode.Cards.ReshuffledSpire;
using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Epochs;
using Alchemist.AlchemistCode.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Runs.Metrics;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;
using Steamworks;

namespace Alchemist.AlchemistCode.Analytics;

internal static class AlchemistMetrics
{
    // Vanilla skips runs shorter than this many floors
    private const int RunLengthThreshold = 5;
    private const int BlitzRunLengthThreshold = 3;
    // Bump when a counter is added or a key changes meaning
    private const int Schema = 5;

    private static readonly HashSet<string> UploadedModes = new() { "Standard", "Dread", "Blitz" };

    private static readonly PropertyInfo? CustomGameProperty = typeof(SerializableRun).GetProperty("IsCustomGame");
    private static readonly PropertyInfo? BlitzChoicesProperty =
        typeof(PlayerMapPointHistoryEntry).GetProperty("BlitzMerchantChoices");
    private static readonly MethodInfo? DominionMethod = typeof(EncounterModel).GetMethod("GetDominion");

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
        var mode = run.GameMode.ToString();
        if (!UploadedModes.Contains(mode) || CustomGameProperty?.GetValue(run) is true) return;

        var localPlayer = run.Players.FirstOrDefault(p => p.NetId == localPlayerId);
        if (localPlayer?.CharacterId == null || localPlayer.CharacterId != ModelDb.Character<Character.Alchemist>().Id)
            return;

        var allPoints = run.MapPointHistory.SelectMany(act => act).ToList();
        if (allPoints.Count < (mode == "Blitz" ? BlitzRunLengthThreshold : RunLengthThreshold)) return;

        var language = LocManager.Instance.Language;
        // Entry ids must serialize in English whatever language the player uses
        LocManager.Instance.StartOverridingLanguageAsEnglish();
        try
        {
            var metrics = VanillaRunMetrics.Build(run, isVictory, localPlayer, allPoints);
            var payload = Payload(run, isVictory, localPlayer, allPoints, language);
            payload["mode"] = mode.ToLowerInvariant();
            if (mode == "Blitz") payload["blitz"] = BlitzJson(run, localPlayer);
            var row = Row(run, isVictory, metrics, payload);
            MainFile.Logger.Info($"Uploading Alchemist run analytics (player {metrics.PlayerId})...");
            RunMetricsUploader.Upload(row.ToJsonString(), "Alchemist run");
        }
        finally
        {
            LocManager.Instance.StopOverridingLanguageAsEnglish();
        }
    }

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

    private static JsonObject Payload(SerializableRun run, bool isVictory, SerializablePlayer player,
        List<MapPointHistoryEntry> allPoints, string language) => new()
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
            ["enable_epochs"] = EffectiveSettings.EnableEpochsFor(run.Players.Count > 1),
            ["keep_pools_separate"] = EffectiveSettings.KeepPoolsSeparateFor(run.Players.Count > 1),
            ["secrets_unlocked"] = SecretUnlocks.IsUnlocked,
            ["hero_cards_without_expansion"] = AlchemistModConfig.HeroCardsWithoutExpansion,
            ["reshuffled_cards_without_mod"] = AlchemistModConfig.ReshuffledCardsWithoutMod,
        },
        ["expansions"] = new JsonObject
        {
            ["hero_expansion"] = HeroExpansion.IsLoaded,
            ["ancients_awakened"] = AncientsAwakenedMod.IsLoaded,
            ["reshuffled_spire"] = ReshuffledSpireMod.IsLoaded,
        },
        ["client"] = new JsonObject
        {
            ["language"] = language,
            ["platform"] = Platform(),
        },
    };

    private static string Platform() =>
        SteamInitializer.Initialized && SteamUtils.IsSteamRunningOnSteamDeck() ? "Steam Deck" : Godot.OS.GetName();

    // Not ScoreUtility.GetBadges: its signature differs between the game branches
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

    // TurnsTaken + 1 is how vanilla counts turns
    private static JsonArray ActsJson(SerializableRun run, SerializablePlayer player)
    {
        var acts = new JsonArray();
        for (var actIndex = 0; actIndex < run.MapPointHistory.Count; actIndex++)
        {
            var points = run.MapPointHistory[actIndex];
            var fights = points.Where(e => e.Rooms.Last().RoomType.IsCombatRoom()).ToList();
            var rest = new JsonObject();
            foreach (var choice in points.SelectMany(e => e.GetEntry(player.NetId).RestSiteChoices))
                rest[choice] = (rest[choice]?.GetValue<int>() ?? 0) + 1;
            var act = new JsonObject
            {
                ["act"] = actIndex + 1,
                ["fights"] = fights.Count,
                ["turns"] = fights.Sum(e => e.Rooms.Last().TurnsTaken + 1),
                ["damage"] = fights.Sum(e => Math.Min(e.GetEntry(player.NetId).DamageTaken, player.MaxHp)),
                ["rest"] = rest,
            };
            if (actIndex < run.Acts.Count && run.Acts[actIndex].SerializableRooms is { } rooms)
            {
                if (rooms.BossId is { } boss) act["boss"] = boss.Entry;
                if (rooms.SecondBossId is { } second) act["second_boss"] = second.Entry;
                if (run.GameMode.ToString() == "Dread" && DominionOf(rooms.SecondBossId ?? rooms.BossId) is { } dominion)
                    act["dominion"] = dominion;
            }
            acts.Add(act);
        }
        return acts;
    }

    private static string? DominionOf(ModelId? boss) =>
        boss != null && ModelDb.GetByIdOrNull<EncounterModel>(boss) is { } encounter
            && DominionMethod?.Invoke(encounter, null) is AbstractModel dominion
            ? dominion.Id.Entry
            : null;

    private static JsonArray BlitzJson(SerializableRun run, SerializablePlayer player)
    {
        var picks = new JsonArray();
        if (BlitzChoicesProperty == null) return picks;
        for (var actIndex = 0; actIndex < run.MapPointHistory.Count; actIndex++)
        {
            foreach (var point in run.MapPointHistory[actIndex])
            {
                if (BlitzChoicesProperty.GetValue(point.GetEntry(player.NetId)) is not IEnumerable choices) continue;
                foreach (var choice in choices)
                {
                    var type = choice.GetType();
                    var pick = new JsonObject
                    {
                        ["act"] = actIndex + 1,
                        ["type"] = type.GetProperty("Type")?.GetValue(choice)?.ToString(),
                        ["picked"] = type.GetField("wasPicked")?.GetValue(choice) is true,
                    };
                    if (type.GetField("modelId")?.GetValue(choice) is ModelId model) pick["model"] = model.Entry;
                    picks.Add(pick);
                }
            }
        }
        return picks;
    }

    private static List<string> ObtainedEpochs()
    {
        if (!EpochRegistration.Supported) return new List<string>();
        var progress = SaveManager.Instance.Progress;
        return EpochRegistration.AlchemistEpochTypes
            .Select(EpochModel.GetId)
            .Where(progress.IsEpochObtained)
            .ToList();
    }

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
