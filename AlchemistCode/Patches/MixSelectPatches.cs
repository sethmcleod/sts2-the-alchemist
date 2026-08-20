using System.Reflection;
using Alchemist.AlchemistCode.Cards.Token;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Alchemist.AlchemistCode.Patches;

// NCardGrid centers a row on the width of a FULL row (Columns * card width), so a selection with
// fewer cards than fit sits left of center. That never shows on the screens the base game fills
// (library, deck views), but the four-card Mix pick floats alone on a wide grid. Shift only that
// case: every displayed card is a Mix token, and there are fewer of them than columns
[HarmonyPatch(typeof(NCardGrid), "UpdateGridPositions")]
public static class MixSelectCenterPatch
{
    private static readonly MethodInfo ColumnsGetter =
        AccessTools.PropertyGetter(typeof(NCardGrid), "Columns");
    private static readonly FieldInfo CardSize = AccessTools.Field(typeof(NCardGrid), "_cardSize");

    public static void Postfix(NCardGrid __instance)
    {
        var holders = __instance.CurrentlyDisplayedCardHolders.ToList();
        if (holders.Count == 0) return;
        if (!holders.All(h => h.CardModel is BurstingMix or FumingMix or SyrupyMix or ZestyMix))
            return;

        var columns = (int)ColumnsGetter.Invoke(__instance, null)!;
        if (holders.Count >= columns) return;

        var cardSize = (Vector2)CardSize.GetValue(__instance)!;
        var shift = (columns - holders.Count) * (cardSize.X + 40f) * 0.5f;
        foreach (var holder in holders)
            holder.Position += new Vector2(shift, 0);
    }
}
