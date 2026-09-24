using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Alchemist.AlchemistCode.Patches;

// NCardGrid centers a row on the width of a FULL row (Columns * card width), so a selection with
// fewer cards than fit sits left of center. That never shows on the screens the base game fills
// (library, deck views), but the four-card Mix pick floats alone on a wide grid. Shift only while
// the Mix picker is open and shows fewer cards than columns. A content check is not enough: the
// library filtered to "Mix" also shows only Mix tokens
[HarmonyPatch(typeof(NCardGrid), "UpdateGridPositions")]
public static class MixSelectCenterPatch
{
    private static readonly MethodInfo ColumnsGetter =
        AccessTools.PropertyGetter(typeof(NCardGrid), "Columns");
    private static readonly FieldInfo CardSize = AccessTools.Field(typeof(NCardGrid), "_cardSize");

    public static void Postfix(NCardGrid __instance)
    {
        if (!MixPickerGridPatch.PickerOpen) return;
        var holders = __instance.CurrentlyDisplayedCardHolders.ToList();
        if (holders.Count == 0) return;

        var columns = (int)ColumnsGetter.Invoke(__instance, null)!;
        if (holders.Count >= columns) return;

        var cardSize = (Vector2)CardSize.GetValue(__instance)!;
        var shift = (columns - holders.Count) * (cardSize.X + 40f) * 0.5f;
        foreach (var holder in holders)
            holder.Position += new Vector2(shift, 0);
    }
}
