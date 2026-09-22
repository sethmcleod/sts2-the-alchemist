using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Alchemist.AlchemistCode.Patches;

// The grid picks its column count from its width, which puts a six-card Mix picker on one row of five
// and one of one. While the picker is open the count is forced, so the six sit in two rows of three,
// centred by the grid's own layout. No other screen is open during that await, so nothing else sees it
[HarmonyPatch(typeof(NCardGrid), "Columns", MethodType.Getter)]
public static class MixPickerGridPatch
{
    // Both set by Mixing.Choose around its selection call. PickerOpen is true for the whole await;
    // Columns is the forced count, null when the grid's own choice is fine
    public static bool PickerOpen;
    public static int? Columns;

    public static bool Prefix(ref int __result)
    {
        if (Columns is not { } columns) return true;
        __result = columns;
        return false;
    }
}
