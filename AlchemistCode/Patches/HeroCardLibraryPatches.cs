using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Saves;

namespace Alchemist.AlchemistCode.Patches;

// Every pool's unlocked list is the one place the reward roll, the compendium's lock state and the
// in-combat generators all read, so this is where a cross-mod card whose mod is absent leaves the game
[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards))]
public static class HeroCardPoolPatches
{
    public static void Postfix(ref IEnumerable<CardModel> __result)
    {
        __result = __result.Where(CrossMod.Offered).ToList();
    }
}

// The card library marks a card Locked when no pool unlocks it, but opens it on click whenever the
// profile has discovered it. The base game never has a discovered card that is locked, so it never
// guards the click. A Hero Expansion card seen with the mod on and viewed with it off is exactly
// that, so this keeps the locked ones closed: no click, no paging onto them, no hand cursor
public static class HeroCardLibraryPatches
{
    private static bool Gated(CardModel card) => !CrossMod.Offered(card);

    [HarmonyPatch(typeof(NCardLibrary), "ShowCardDetail")]
    public static class ShowCardDetail
    {
        // The private fields arrive by Harmony injection: three underscores plus the field's own leading
        // underscore. The tickbox type stays opaque, so Traverse reads it
        public static bool Prefix(NCardHolder holder, NCardLibraryGrid ____grid, object ____viewUpgrades,
            ref Godot.Control ____lastHoveredControl)
        {
            if (Gated(holder.CardModel)) return false;
            var discovered = SaveManager.Instance.Progress.DiscoveredCards;
            if (!discovered.Contains(holder.CardModel.Id)) return false;
            ____lastHoveredControl = holder;
            var list = ____grid.VisibleCards.Where(c => discovered.Contains(c.Id) && !Gated(c)).ToList();
            var viewUpgrades = Traverse.Create(____viewUpgrades).Property<bool>("IsTicked").Value;
            NGame.Instance.GetInspectCardScreen().Open(list, list.IndexOf(holder.CardModel), viewUpgrades);
            return false;
        }
    }

    [HarmonyPatch(typeof(NCardLibraryGrid), "InitGrid")]
    public static class InitGrid
    {
        public static void Postfix(List<List<NGridCardHolder>> ____cardRows) =>
            PlainCursor(____cardRows.SelectMany(r => r));
    }

    [HarmonyPatch(typeof(NCardLibraryGrid), "AssignCardsToRow")]
    public static class AssignCardsToRow
    {
        public static void Postfix(List<NGridCardHolder> row) => PlainCursor(row);
    }

    private static void PlainCursor(IEnumerable<NGridCardHolder> holders)
    {
        foreach (var holder in holders)
            if (holder.CardNode != null && Gated(holder.CardNode.Model))
                holder.Hitbox.MouseDefaultCursorShape = Godot.Control.CursorShape.Arrow;
    }
}
