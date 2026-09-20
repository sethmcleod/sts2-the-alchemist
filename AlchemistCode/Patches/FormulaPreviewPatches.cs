using Alchemist.AlchemistCode.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Patches;

// The game previews a card's damage vars with the hovered target and the preview mode, both handed to
// UpdateDynamicVarPreview by the card node. A formula preview ({FormulaDamage}) is a string argument
// built later, in AddExtraArgsToDescription, which gets neither. Without the target, every enemy-side
// power is skipped. This records the context on the card so the formula runs the same hooks as a native
// damage var would
[HarmonyPatch(typeof(CardModel), nameof(CardModel.UpdateDynamicVarPreview))]
public static class FormulaPreviewPatches
{
    public static void Postfix(CardModel __instance, CardPreviewMode previewMode, Creature? target)
    {
        if (__instance is not AlchemistCard card) return;
        // The same rule the game applies to runGlobalHooks inside the patched method
        var runsHooks = card.CombatState != null
            && (card.Pile?.Type is PileType.Hand or PileType.Play
                || card.UpgradePreviewType == CardUpgradePreviewType.Combat);
        card.RecordPreviewContext(previewMode, target, runsHooks);
    }
}
