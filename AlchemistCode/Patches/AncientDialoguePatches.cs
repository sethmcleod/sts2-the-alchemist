using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// Two fixes to the dialogue the base game picks for the Alchemist:
//
// 1. GetValidDialogues returns the shared "firstVisitEver" scene at totalVisits == 0, preempting our
//    character-specific dialogue.
// 2. Past the last visit we wrote, the base game would keep our last conversation in play. Its loc keys
//    carry the "r" suffix, so PopulateLines marks it IsRepeating and AddRepeatingDialogues offers it on
//    every later visit. Hand off to the base game's own repeating pool instead
[HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues))]
public static class AncientFirstMeetingPatch
{
    private const string AlchemistCharEntry = "ALCHEMIST-ALCHEMIST";

    // An ancient from another mod usually writes dialogue only for the base cast, so the
    // Alchemist gets an empty pool. NEventRoom picks with Rng.NextItem, which returns null on an
    // empty list, and dereferences the pick: an empty pool is a black screen, not a silent skip.
    // One neutral shared line keeps any unknown ancient loadable
    private static List<AncientDialogue>? _silentMeeting;

    private static List<AncientDialogue> SilentMeeting
    {
        get
        {
            if (_silentMeeting == null)
            {
                var dialogue = new AncientDialogue("");
                dialogue.PopulateLines("ALCHEMIST-SILENT_MEETING", "ANY", 0);
                _silentMeeting = new List<AncientDialogue> { dialogue };
            }
            return _silentMeeting;
        }
    }

    public static void Postfix(AncientDialogueSet __instance, ModelId characterId, int charVisits,
        ref IEnumerable<AncientDialogue> __result)
    {
        if (characterId.Entry != AlchemistCharEntry) return;
        if (!__instance.CharacterDialogues.TryGetValue(AlchemistCharEntry, out var ours))
        {
            if (!__result.Any()) __result = SilentMeeting;
            return;
        }

        // A visit we wrote a conversation for. Ours wins, even on the first visit ever
        var matching = ours.Where(d => d.VisitIndex == charVisits).ToList();
        if (matching.Count > 0)
        {
            __result = matching;
            return;
        }

        // Our conversations are used up, so let the ancient fall back to its character-agnostic pool the
        // way it does for the base characters (Neow greets you with "..I've... ..brought... ..you back..."
        // rather than saying our last line again, which also costs a click before the boon options)
        var agnostic = __result.Where(d => !ours.Contains(d)).ToList();
        if (agnostic.Count > 0)
        {
            __result = agnostic;
            return;
        }

        // The ancient has no pool to fall back to, because the Architect allows no character-agnostic
        // dialogue. Rng.NextItem returns null on an empty list, so keep the last visit we wrote.
        // A dialogue with no VisitIndex counts as 0
        var last = ours
            .Where(d => (d.VisitIndex ?? 0) <= charVisits)
            .OrderByDescending(d => d.VisitIndex ?? 0)
            .FirstOrDefault();
        if (last == null)
        {
            if (!__result.Any()) __result = SilentMeeting;
            return;
        }
        __result = ours.Where(d => (d.VisitIndex ?? 0) == (last.VisitIndex ?? 0)).ToList();
    }
}
