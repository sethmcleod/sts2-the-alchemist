using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// The shared firstVisitEver scene would preempt ours at 0 visits, and our last conversation would
// repeat forever (its keys end in "r"), so used-up visits fall back to the base pool
[HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues))]
public static class AncientFirstMeetingPatch
{
    private const string AlchemistCharEntry = "ALCHEMIST-ALCHEMIST";

    // NEventRoom dereferences Rng.NextItem's pick, so an empty pool is a black screen
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

        var matching = ours.Where(d => d.VisitIndex == charVisits).ToList();
        if (matching.Count > 0)
        {
            __result = matching;
            return;
        }

        var agnostic = __result.Where(d => !ours.Contains(d)).ToList();
        if (agnostic.Count > 0)
        {
            __result = agnostic;
            return;
        }

        // The Architect has no agnostic pool, so keep our last visit; a missing VisitIndex counts as 0
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
