using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Alchemist.AlchemistCode.Patches;

// The grid picks its column count from its width, which puts a six-card Mix picker on one row of five
// and one of one. While the picker is open the count is forced, so the six sit in two rows of three,
// centred by the grid's own layout
[HarmonyPatch(typeof(NCardGrid), "Columns", MethodType.Getter)]
public static class MixPickerGridPatch
{
    // Both set by Mixing.Choose around its selection call. Columns is the forced count, null when the
    // grid's own choice is fine
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
