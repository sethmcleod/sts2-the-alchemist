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

    // Alchemist1 is the parent of every other Alchemist epoch (see Alchemist1Epoch.GetTimelineExpansion),
    // but it is only awarded post-run while Alchemist2..6 are awarded mid-run. A run that ends without the
    // post-run pass, such as leaving a multiplayer game part way through, leaves a child obtained under an
    // unobtained parent. The Timeline lays its slots out by walking down from NeowEpoch, so that child can
    // never be placed, and therefore never revealed, and GetDiscoveredEpochCount never returns to zero.
    // NMainMenu disables Singleplayer, Multiplayer and Compendium while that count is above zero and no run
    // save exists, so the player is locked in the Timeline for good. Obtaining the parent first keeps the
    // chain whole, so every epoch we award can actually be revealed
    private static void AwardMidRun(ProgressSaveManager mgr, EpochModel epoch, Player player)
    {
        var root = EpochModel.Get<Alchemist1Epoch>();
        if (epoch.Id != root.Id && !mgr.Progress.IsEpochObtained(root.Id))
            MidRun.Invoke(mgr, new object[] { root, player });
        MidRun.Invoke(mgr, new object[] { epoch, player });
    }

    private static void AwardPostRun(ProgressSaveManager mgr, EpochModel epoch, SerializablePlayer sp, SerializableRun sr) =>
        PostRun.Invoke(mgr, new object[] { epoch, sp, sr });

    // Supported is the "does this build of the game have an epoch registry" half. Without it our
    // epochs were never registered, so appending their ids would gate content behind epochs the
    // Timeline can never show
    private static bool Enabled => AlchemistModConfig.EnableEpochs && EpochRegistration.Supported;

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

    // On a first Alchemist run an Act boss awards Alchemist2..4 mid-run, but the root Alchemist1 only lands
    // post-run, so the child is Obtained while its parent is not. Vanilla GetRevealableEpochs is a BFS from
    // NeowEpoch that needs each parent obtained, so it never reaches the child, and TryObtainEpochInternal
    // then logs a warning and fires a Sentry capture on that guaranteed path. Union in our own obtained
    // epochs so the check passes. Scoped to ours, so no other caller sees an unearned epoch, and it
    // self-heals once Alchemist1 is obtained. Mirrors RitsuLib's
    // ProgressSaveManagerGetRevealableEpochsModTemplatePatch
    [HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.GetRevealableEpochs))]
    [HarmonyPostfix]
    private static void RevealObtainedAlchemistEpochs(ProgressSaveManager __instance, ref IEnumerable<SerializableEpoch> __result)
    {
        if (!Enabled) return;

        // Never report an epoch as revealable while its parent is not obtained. The Timeline could not
        // place it, so counting it here would keep GetDiscoveredEpochCount above zero for good and lock
        // the main menu. AwardMidRun keeps the parent ahead of the child; this is the backstop
        if (!__instance.Progress.IsEpochObtained(EpochModel.GetId<Alchemist1Epoch>())) return;

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

    // With the Timeline feature off, strip our epochs from every slot batch so they do not render. The
    // saved epoch states are untouched, so progress returns when the feature is turned back on. A prefix,
    // because the async body reads the list
    [HarmonyPatch(typeof(NTimelineScreen), "AddEpochSlots")]
    [HarmonyPrefix]
    private static void HideAlchemistEpochsWhenDisabled(List<EpochSlotData> slotsToAdd)
    {
        if (Enabled) return;
        slotsToAdd.RemoveAll(s => s.Model is AlchemistEpoch);
    }
}
