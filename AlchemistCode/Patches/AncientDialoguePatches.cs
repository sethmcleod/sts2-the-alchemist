using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

/// <summary>
/// BaseLib registers our per-character ancient dialogue (loc keys <c>{ANCIENT}.talk.ALCHEMIST-ALCHEMIST.*</c>)
/// into each ancient's <see cref="AncientDialogueSet.CharacterDialogues"/> — that part already works. But the
/// base <see cref="AncientDialogueSet.GetValidDialogues"/> returns the ancient's shared "firstVisitEver" scene
/// whenever it's the first time ANY character has met that ancient (totalVisits == 0), which PREEMPTS
/// character-specific dialogue. So on a fresh save the Alchemist saw the generic first-meeting intro instead
/// of its own greeting (e.g. Orobas' "Puppet puppet!!" / Tezcatara's "Do come in, dear!").
///
/// For the Alchemist only, prefer our own dialogue for the current visit whenever one exists — giving the
/// frog a bespoke first meeting with every ancient. Gated on the visiting character's id, so no other
/// character (or mod) is affected; a no-op when we have nothing for this visit (base behavior is kept).
/// </summary>
[HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues))]
public static class AncientFirstMeetingPatch
{
    // Custom models' Id.Entry carries the mod prefix (same as our card loc keys, e.g. ALCHEMIST-STRIKE_ALCHEMIST).
    private const string AlchemistCharEntry = "ALCHEMIST-ALCHEMIST";

    public static void Postfix(AncientDialogueSet __instance, ModelId characterId, int charVisits,
        ref IEnumerable<AncientDialogue> __result)
    {
        if (characterId.Entry != AlchemistCharEntry) return;
        if (!__instance.CharacterDialogues.TryGetValue(AlchemistCharEntry, out var ours)) return;

        var matching = ours.Where(d => d.VisitIndex == charVisits).ToList();
        if (matching.Count > 0)
            __result = matching; // our greeting for this visit wins over firstVisitEver / ANY
    }
}
