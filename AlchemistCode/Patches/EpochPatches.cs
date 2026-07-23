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
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Timeline.Epochs;

namespace Alchemist.AlchemistCode.Patches;

// The Skip* prefixes in BaseLib stop the vanilla epoch bookkeeping for a custom character. Harmony still
// runs our postfixes, so this class awards the epochs from a postfix
[HarmonyPatch]
public static class EpochPatches
{
    private const BindingFlags InstNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly MethodInfo MidRun = RequireInstance("TryObtainEpochMidRun");
    private static readonly MethodInfo PostRun = RequireInstance("TryObtainEpochPostRun");
    private static readonly MethodInfo GetElites =
        typeof(ProgressSaveManager).GetMethod("GetEliteEncounters", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("[Alchemist] ProgressSaveManager.GetEliteEncounters not found; base game changed.");

    private static MethodInfo RequireInstance(string name) =>
        typeof(ProgressSaveManager).GetMethod(name, InstNonPublic)
        ?? throw new InvalidOperationException($"[Alchemist] ProgressSaveManager.{name} not found; base game changed.");

    private static void AwardMidRun(ProgressSaveManager mgr, EpochModel epoch, Player player) =>
        MidRun.Invoke(mgr, new object[] { epoch, player });

    private static void AwardPostRun(ProgressSaveManager mgr, EpochModel epoch, SerializablePlayer sp, SerializableRun sr) =>
        PostRun.Invoke(mgr, new object[] { epoch, sp, sr });

    private static bool Enabled => AlchemistModConfig.EnableEpochs;

    private static bool IsAlchemist(Player p) => p?.Character is AlchemistCharacter;

    // Use OrNull, not GetById. If the character mod of a save is uninstalled, GetById throws
    // ModelNotFoundException out of the post-run unlock path that this class postfixes
    private static bool IsAlchemist(SerializablePlayer sp) =>
        sp.CharacterId != null && ModelDb.GetByIdOrNull<CharacterModel>(sp.CharacterId) is AlchemistCharacter;

    [HarmonyPatch(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")]
    [HarmonyPostfix]
    private static void AwardActEpoch(ProgressSaveManager __instance, Player localPlayer, int act)
    {
        if (!Enabled || !IsAlchemist(localPlayer)) return;
        EpochModel? epoch = act switch
        {
            0 => EpochModel.Get<Alchemist2Epoch>(),
            1 => EpochModel.Get<Alchemist3Epoch>(),
            2 => EpochModel.Get<Alchemist4Epoch>(),
            _ => null,
        };
        if (epoch != null) AwardMidRun(__instance, epoch, localPlayer);
    }

    [HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch")]
    [HarmonyPostfix]
    private static void AwardEliteEpoch(ProgressSaveManager __instance, Player localPlayer)
    {
        if (!Enabled || !IsAlchemist(localPlayer)) return;
        var elites = (HashSet<ModelId>)GetElites.Invoke(null, null)!;
        if (CountWins(localPlayer, elites) >= 15)
            AwardMidRun(__instance, EpochModel.Get<Alchemist5Epoch>(), localPlayer);
    }

    [HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch")]
    [HarmonyPostfix]
    private static void AwardBossEpoch(ProgressSaveManager __instance, Player localPlayer)
    {
        if (!Enabled || !IsAlchemist(localPlayer)) return;
        var bosses = ModelDb.Acts.SelectMany(a => a.AllBossEncounters.Select(e => e.Id)).ToHashSet();
        if (CountWins(localPlayer, bosses) >= 15)
            AwardMidRun(__instance, EpochModel.Get<Alchemist6Epoch>(), localPlayer);
    }

    [HarmonyPatch(typeof(ProgressSaveManager), "CheckAscensionOneCompleted")]
    [HarmonyPostfix]
    private static void AwardAscensionEpoch(ProgressSaveManager __instance, SerializablePlayer serializablePlayer, SerializableRun serializableRun)
    {
        if (Enabled && serializableRun.Ascension == 1 && IsAlchemist(serializablePlayer))
            AwardPostRun(__instance, EpochModel.Get<Alchemist7Epoch>(), serializablePlayer, serializableRun);
    }

    [HarmonyPatch(typeof(ProgressSaveManager), "PostRunUnlockCharacterEpochCheck")]
    [HarmonyPostfix]
    private static void AwardFirstRunEpoch(ProgressSaveManager __instance, SerializablePlayer serializablePlayer, SerializableRun serializableRun)
    {
        if (Enabled && IsAlchemist(serializablePlayer))
            AwardPostRun(__instance, EpochModel.Get<Alchemist1Epoch>(), serializablePlayer, serializableRun);
    }

    // On a player's first Alchemist run, an Act boss kill awards Alchemist2..4 mid-run through
    // AwardActEpoch, but the root Alchemist1 epoch is only awarded post-run. So at award time the child is
    // Obtained while its parent is not, and vanilla GetRevealableEpochs, a BFS from NeowEpoch that needs
    // each parent obtained, does not reach it. TryObtainEpochInternal then logs a warning and fires a
    // Sentry capture on that guaranteed first-run path. Union our own obtained epochs into the result so
    // the check passes. This self-heals after the first run, once Alchemist1 is obtained. It is scoped to
    // our obtained epochs, so the other callers of GetRevealableEpochs see no unearned epoch. This mirrors
    // RitsuLib's ProgressSaveManagerGetRevealableEpochsModTemplatePatch
    [HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.GetRevealableEpochs))]
    [HarmonyPostfix]
    private static void RevealObtainedAlchemistEpochs(ProgressSaveManager __instance, ref IEnumerable<SerializableEpoch> __result)
    {
        if (!Enabled) return;
        var list = __result.ToList();
        var seen = new HashSet<string>(list.Select(e => e.Id));
        var added = false;
        foreach (var epoch in __instance.Progress.Epochs)
        {
            if (epoch.State != EpochState.Obtained && epoch.State != EpochState.ObtainedNoSlot) continue;
            if (!seen.Add(epoch.Id)) continue;
            EpochModel model;
            try { model = EpochModel.Get(epoch.Id); }
            catch { continue; } // an id from an uninstalled mod does not resolve; leave it out
            if (model is AlchemistEpoch)
            {
                list.Add(epoch);
                added = true;
            }
        }
        if (added) __result = list;
    }

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

    [HarmonyPatch(typeof(SaveManager), "GetCardUnlockEpochIds")] [HarmonyPostfix]
    private static void GateCards(ref string[] __result) => Append(ref __result, EpochUnlockKind.Cards);

    [HarmonyPatch(typeof(SaveManager), "GetRelicUnlockEpochIds")] [HarmonyPostfix]
    private static void GateRelics(ref string[] __result) => Append(ref __result, EpochUnlockKind.Relics);

    [HarmonyPatch(typeof(SaveManager), "GetPotionUnlockEpochIds")] [HarmonyPostfix]
    private static void GatePotions(ref string[] __result) => Append(ref __result, EpochUnlockKind.Potions);

    private static void Append(ref string[] result, EpochUnlockKind kind)
    {
        if (!Enabled) return;
        result = result.Concat(EpochRegistration.GatingEpochIds(kind)).ToArray();
    }

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

    [HarmonyPatch(typeof(NeowEpoch), "GetTimelineExpansion")] [HarmonyPostfix]
    private static void AddFirstChapterSlot(ref EpochModel[] __result)
    {
        if (!Enabled) return;
        var ch1 = EpochModel.Get<Alchemist1Epoch>();
        if (__result.All(e => e.Id != ch1.Id))
            __result = __result.Append(ch1).ToArray();
    }

    // If the Timeline feature is off, remove our epochs from every slot batch. They then do not appear
    // on the Timeline. Both the full rebuild and the reveal animations use this method. This does not
    // change the saved epoch states, so the previous progress returns when you turn the feature on
    // again. This prefix runs before the async body reads the list
    [HarmonyPatch(typeof(NTimelineScreen), "AddEpochSlots")]
    [HarmonyPrefix]
    private static void HideAlchemistEpochsWhenDisabled(List<EpochSlotData> slotsToAdd)
    {
        if (Enabled) return;
        slotsToAdd.RemoveAll(s => s.Model is AlchemistEpoch);
    }
}
