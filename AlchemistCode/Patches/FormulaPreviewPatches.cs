using Alchemist.AlchemistCode.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// AddExtraArgsToDescription gets no target or preview mode, so formula previews would skip enemy powers
[HarmonyPatch(typeof(CardModel), nameof(CardModel.UpdateDynamicVarPreview))]
public static class FormulaPreviewPatches
{
    public static void Postfix(CardModel __instance, CardPreviewMode previewMode, Creature? target)
    {
        if (__instance is not AlchemistCard card) return;
        // Must match the game's runGlobalHooks rule in the patched method
        var runsHooks = card.CombatState != null
            && (card.Pile?.Type is PileType.Hand or PileType.Play
                || card.UpgradePreviewType == CardUpgradePreviewType.Combat);
        card.RecordPreviewContext(previewMode, target, runsHooks);
    }
}
