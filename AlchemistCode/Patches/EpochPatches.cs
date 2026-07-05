using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Epochs;
using AlchemistCharacter = Alchemist.AlchemistCode.Character.Alchemist;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Timeline.Epochs;

namespace Alchemist.AlchemistCode.Patches;

/// <summary>
/// Awards and displays the Alchemist's Timeline epochs. BaseLib's Skip* PREFIXES short-circuit the vanilla
/// epoch bookkeeping for custom characters (whose base switch would otherwise throw ArgumentOutOfRange),
/// but Harmony still runs our POSTFIXES — so we award from postfixes guarded by `Character is Alchemist`.
/// Private base members are reached via cached reflection that throws loudly if the game renames them.
/// </summary>
[HarmonyPatch]
public static class EpochPatches
{
    private const BindingFlags InstNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly MethodInfo MidRun = RequireInstance("TryObtainEpochMidRun");
    private static readonly MethodInfo PostRun = RequireInstance("TryObtainEpochPostRun");
    private static readonly MethodInfo GetElites =
        typeof(ProgressSaveManager).GetMethod("GetEliteEncounters", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("[Alchemist] ProgressSaveManager.GetEliteEncounters not found — base game changed.");

    private static MethodInfo RequireInstance(string name) =>
        typeof(ProgressSaveManager).GetMethod(name, InstNonPublic)
        ?? throw new InvalidOperationException($"[Alchemist] ProgressSaveManager.{name} not found — base game changed.");

    private static void AwardMidRun(ProgressSaveManager mgr, EpochModel epoch, Player player) =>
        MidRun.Invoke(mgr, new object[] { epoch, player });

    private static void AwardPostRun(ProgressSaveManager mgr, EpochModel epoch, SerializablePlayer sp, SerializableRun sr) =>
        PostRun.Invoke(mgr, new object[] { epoch, sp, sr });

    /// <summary>Master switch — OFF by default. While off, epochs never attach to the Timeline, never gate
    /// content, and never award, so they can't clutter the Timeline or collide with other mods.</summary>
    private static bool Enabled => AlchemistModConfig.EnableEpochs;

    /// <summary>Reveal every Alchemist epoch that has no progress entry yet. On a fresh save (feature just
    /// enabled) this makes all epochs active so no in-use content gets locked; after a "Reset Unlocks"
    /// the entries exist (NotObtained) so this skips them → milestone-progression mode takes over.</summary>
    private static void RevealAllIfFresh()
    {
        var progress = SaveManager.Instance?.Progress;
        if (progress == null) return;
        foreach (var type in EpochRegistration.AlchemistEpochTypes)
        {
            var id = EpochModel.GetId(type);
            if (!progress.HasEpoch(id))
                SaveManager.Instance.ObtainEpochOverride(id, EpochState.Revealed);
        }
    }

    private static bool IsAlchemist(Player p) => p?.Character is AlchemistCharacter;
    private static bool IsAlchemist(SerializablePlayer sp) =>
        ModelDb.GetById<CharacterModel>(sp.CharacterId) is AlchemistCharacter;

    // ── Ch2/3/4: beat Act 1/2/3 (mid-run, on boss kill) ──
    [HarmonyPatch(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")]
    [HarmonyPostfix]
    private static void AwardActEpoch(ProgressSaveManager __instance, Player localPlayer, int act)
    {
        if (!Enabled || !IsAlchemist(localPlayer)) return;
        EpochModel epoch = act switch
        {
            0 => EpochModel.Get<Alchemist2Epoch>(),
            1 => EpochModel.Get<Alchemist3Epoch>(),
            2 => EpochModel.Get<Alchemist4Epoch>(),
            _ => null,
        };
        if (epoch != null) AwardMidRun(__instance, epoch, localPlayer);
    }

    // ── Ch5: 15 Elites defeated ──
    [HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch")]
    [HarmonyPostfix]
    private static void AwardEliteEpoch(ProgressSaveManager __instance, Player localPlayer)
    {
        if (!Enabled || !IsAlchemist(localPlayer)) return;
        var elites = (HashSet<ModelId>)GetElites.Invoke(null, null)!;
        if (CountWins(localPlayer, elites) >= 15)
            AwardMidRun(__instance, EpochModel.Get<Alchemist5Epoch>(), localPlayer);
    }

    // ── Ch6: 15 Bosses defeated ──
    [HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch")]
    [HarmonyPostfix]
    private static void AwardBossEpoch(ProgressSaveManager __instance, Player localPlayer)
    {
        if (!Enabled || !IsAlchemist(localPlayer)) return;
        var bosses = ModelDb.Acts.SelectMany(a => a.AllBossEncounters.Select(e => e.Id)).ToHashSet();
        if (CountWins(localPlayer, bosses) >= 15)
            AwardMidRun(__instance, EpochModel.Get<Alchemist6Epoch>(), localPlayer);
    }

    // ── Ch7: Ascension 1 completed (post-run) ──
    [HarmonyPatch(typeof(ProgressSaveManager), "CheckAscensionOneCompleted")]
    [HarmonyPostfix]
    private static void AwardAscensionEpoch(ProgressSaveManager __instance, SerializablePlayer serializablePlayer, SerializableRun serializableRun)
    {
        if (Enabled && serializableRun.Ascension == 1 && IsAlchemist(serializablePlayer))
            AwardPostRun(__instance, EpochModel.Get<Alchemist7Epoch>(), serializablePlayer, serializableRun);
    }

    // ── Ch1: played a run — reveals the Alchemist's timeline (post-run) ──
    [HarmonyPatch(typeof(ProgressSaveManager), "PostRunUnlockCharacterEpochCheck")]
    [HarmonyPostfix]
    private static void AwardFirstRunEpoch(ProgressSaveManager __instance, SerializablePlayer serializablePlayer, SerializableRun serializableRun)
    {
        if (Enabled && IsAlchemist(serializablePlayer))
            AwardPostRun(__instance, EpochModel.Get<Alchemist1Epoch>(), serializablePlayer, serializableRun);
    }

    /// <summary>Total wins vs the given encounter set with the current character (mirrors the base loop; one helper for elites+bosses).</summary>
    private static int CountWins(Player player, HashSet<ModelId> encounterIds)
    {
        var character = player.Character.Id;
        var stats = SaveManager.Instance?.Progress?.EncounterStats;
        if (stats == null) return 0;
        var wins = 0;
        foreach (var e in stats.Values)
        {
            if (!encounterIds.Contains(e.Id)) continue;
            foreach (var f in e.FightStats)
                if (f.Character == character) { wins += f.Wins; break; }
        }
        return wins;
    }

    // ── Content gating: append our epoch ids to the base unlock lists (auto-collected by kind) ──
    [HarmonyPatch(typeof(SaveManager), "GetCardUnlockEpochIds")] [HarmonyPostfix]
    private static void GateCards(ref string[] __result) => Append(ref __result, EpochUnlockKind.Cards);

    [HarmonyPatch(typeof(SaveManager), "GetRelicUnlockEpochIds")] [HarmonyPostfix]
    private static void GateRelics(ref string[] __result) => Append(ref __result, EpochUnlockKind.Relics);

    [HarmonyPatch(typeof(SaveManager), "GetPotionUnlockEpochIds")] [HarmonyPostfix]
    private static void GatePotions(ref string[] __result) => Append(ref __result, EpochUnlockKind.Potions);

    private static void Append(ref string[] result, EpochUnlockKind kind)
    {
        if (!Enabled) return;              // feature off → don't gate any content
        RevealAllIfFresh();                // fresh save → reveal all so nothing in-use gets locked
        result = result.Concat(EpochRegistration.GatingEpochIds(kind)).ToArray();
    }

    // ── Portrait art for our epochs (base looks under game res dirs; point ours at the mod pck) ──
    private const string EpochImageDir = "res://Alchemist/images/epochs/";

    [HarmonyPatch(typeof(EpochModel), "ResolvedPortraitPath", MethodType.Getter)] [HarmonyPostfix]
    private static void OurPortrait(EpochModel __instance, ref string __result)
    {
        if (__instance is AlchemistEpoch) __result = EpochImageDir + __instance.Id.ToLowerInvariant() + ".png";
    }

    [HarmonyPatch(typeof(EpochModel), "PackedPortraitPath", MethodType.Getter)] [HarmonyPostfix]
    private static void OurPackedPortrait(EpochModel __instance, ref string __result)
    {
        if (__instance is AlchemistEpoch) __result = EpochImageDir + __instance.Id.ToLowerInvariant() + ".png";
    }

    // ── Put Ch1's slot on the timeline (via Neow's expansion, shown from game start) ──
    [HarmonyPatch(typeof(NeowEpoch), "GetTimelineExpansion")] [HarmonyPostfix]
    private static void AddFirstChapterSlot(ref EpochModel[] __result)
    {
        if (!Enabled) return;              // feature off → no Alchemist chapter on the Timeline
        RevealAllIfFresh();                // fresh save → show all chapters active
        var ch1 = EpochModel.Get<Alchemist1Epoch>();
        if (__result.All(e => e.Id != ch1.Id))
            __result = __result.Append(ch1).ToArray();
    }
}
