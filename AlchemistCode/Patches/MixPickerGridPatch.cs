using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Alchemist.AlchemistCode.Patches;

// The grid picks its column count from its width, which puts a six-card Mix picker (Everflowing
// Chalice) on one row of five and one of one. While the picker is open the count is forced, so the
// six sit in two rows of three, centred by the grid's own layout.
//
// The getter is shared by every NCardGrid (deck view, pile views, rewards, library), so the force is
// keyed to the picker's own grid: one that shows only the option cards Mixing.Choose made. A global
// flag alone leaked: opening the deck view while the picker was up, or leaving the fight before the
// pick resolved (the await never finishes, so its finally never clears the flag), left every grid
// stuck at three columns until the game restarted
[HarmonyPatch(typeof(NCardGrid), "Columns", MethodType.Getter)]
public static class MixPickerGridPatch
{
    // Set by Mixing.Choose around its selection call and cleared in its finally. Options holds the
    // exact card instances the picker shows; Columns is the forced count, null when the grid's own
    // choice is fine
    public static HashSet<CardModel>? Options;
    public static int? Columns;

    // True only for the grid showing the picker: every card on it is one of the options. The options
    // are fresh clones, so the deck view, pile views and the library (which show the canonical Mix
    // tokens) never match
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
