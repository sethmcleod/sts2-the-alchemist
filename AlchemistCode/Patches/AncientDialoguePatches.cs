using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// Two fixes to the dialogue the base game picks for the Alchemist:
//
// 1. Base GetValidDialogues returns the shared "firstVisitEver" scene when totalVisits == 0, which
//    preempts our character-specific dialogue. Prefer our own dialogue for this visit.
// 2. Past the last visit we wrote, the base method finds no VisitIndex match, and its repeating pool
//    is empty for us: BaseLib builds our dialogues from loc keys and cannot mark one IsRepeating. The
//    Architect also forbids the character-agnostic pool. That left a veteran Alchemist with an empty
//    set, and Rng.NextItem returns null on empty. Reuse our last written visit instead, so the
//    evergreen conversation carries every later win
[HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues))]
public static class AncientFirstMeetingPatch
{
    private const string AlchemistCharEntry = "ALCHEMIST-ALCHEMIST";

    public static void Postfix(AncientDialogueSet __instance, ModelId characterId, int charVisits,
        ref IEnumerable<AncientDialogue> __result)
    {
        if (characterId.Entry != AlchemistCharEntry) return;
        if (!__instance.CharacterDialogues.TryGetValue(AlchemistCharEntry, out var ours)) return;

        var matching = ours.Where(d => d.VisitIndex == charVisits).ToList();
        if (matching.Count == 0)
        {
            // The highest visit we wrote at or below this one. A dialogue with no VisitIndex counts as 0
            var fallback = ours
                .Where(d => (d.VisitIndex ?? 0) <= charVisits)
                .OrderByDescending(d => d.VisitIndex ?? 0)
                .FirstOrDefault();
            if (fallback == null) return;
            matching = ours.Where(d => (d.VisitIndex ?? 0) == (fallback.VisitIndex ?? 0)).ToList();
        }

        __result = matching;
    }
}
