using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Alchemist.AlchemistCode.Patches;

// NCardGrid centers a row on the width of a full row, so a short Mix pick sits left of center
[HarmonyPatch(typeof(NCardGrid), "UpdateGridPositions")]
public static class MixSelectCenterPatch
{
    private static readonly MethodInfo ColumnsGetter =
        AccessTools.PropertyGetter(typeof(NCardGrid), "Columns");
    private static readonly FieldInfo CardSize = AccessTools.Field(typeof(NCardGrid), "_cardSize");

    public static void Postfix(NCardGrid __instance)
    {
        if (!MixPickerGridPatch.IsPickerGrid(__instance)) return;
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
