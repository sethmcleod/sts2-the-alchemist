using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Alchemist.AlchemistCode.Patches;

// The grid's own column count splits a six-card Mix pick into rows of five and one
[HarmonyPatch(typeof(NCardGrid), "Columns", MethodType.Getter)]
public static class MixPickerGridPatch
{
    // Set by Mixing.Choose around its selection call
    public static HashSet<CardModel>? Options;
    public static int? Columns;

    public static bool IsPickerGrid(NCardGrid grid)
    {
        if (Options is not { } options) return false;
        var any = false;
        foreach (var holder in grid.CurrentlyDisplayedCardHolders)
        {
            if (holder.CardModel is not { } card || !options.Contains(card)) return false;
            any = true;
        }
        return any;
    }

    public static bool Prefix(NCardGrid __instance, ref int __result)
    {
        if (Columns is not { } columns || !IsPickerGrid(__instance)) return true;
        __result = columns;
        return false;
    }

    public static void Clear()
    {
        Options = null;
        Columns = null;
    }
}
