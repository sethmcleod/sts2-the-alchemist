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
// 2. Past the last visit we wrote there is no VisitIndex match, and our repeating pool is empty: BaseLib
//    builds our dialogues from loc keys and cannot mark one IsRepeating, and the Architect forbids the
//    character-agnostic pool. Rng.NextItem returns null on empty, so reuse the last written visit
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
