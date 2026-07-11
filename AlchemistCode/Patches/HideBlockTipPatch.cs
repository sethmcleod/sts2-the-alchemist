using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Cards;

namespace Alchemist.AlchemistCode.Patches;

// CardModel.HoverTips auto-adds a Block tooltip to every card that gains Block. On our busy Infuse cards that
// is redundant clutter, so cards can opt out via AlchemistCard.HideBlockTooltip. Identify the Block tip by Id,
// the same way the base game distinguishes its static tips
[HarmonyPatch(typeof(CardModel), "get_HoverTips")]
public static class HideBlockTipPatch
{
    private static string? _blockTipId;
    private static string BlockTipId => _blockTipId ??= HoverTipFactory.Static(StaticHoverTip.Block).Id;

    public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance is AlchemistCard { HideBlockTooltip: true })
            __result = __result.Where(t => t.Id != BlockTipId).ToList();
    }
}
