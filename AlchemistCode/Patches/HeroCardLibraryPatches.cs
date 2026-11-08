using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Saves;

namespace Alchemist.AlchemistCode.Patches;

// Rewards, the compendium and combat generators all read this, so absent cross-mod cards drop here
[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards))]
public static class HeroCardPoolPatches
{
    public static void Postfix(CardPoolModel __instance, CardMultiplayerConstraint multiplayerConstraint,
        ref IEnumerable<CardModel> __result)
    {
        if (__instance is not (Character.AlchemistCardPool or EventCardPool or StatusCardPool)) return;
        var multiplayer = multiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly;
        __result = __result.Where(card => CrossMod.Offered(card, multiplayer)).ToList();
    }
}

// The library opens any discovered card on click, even one locked because its mod is off
public static class HeroCardLibraryPatches
{
    private static bool Gated(CardModel card) => !CrossMod.Offered(card);

    [HarmonyPatch(typeof(NCardLibrary), "ShowCardDetail")]
    public static class ShowCardDetail
    {
        public static bool Prefix(NCardHolder holder, NCardLibraryGrid ____grid, object ____viewUpgrades,
            ref Godot.Control ____lastHoveredControl)
        {
            if (holder.CardModel is not { } card) return true;
            if (Gated(card)) return false;
            var discovered = SaveManager.Instance.Progress.DiscoveredCards;
            if (!discovered.Contains(card.Id)) return false;
            ____lastHoveredControl = holder;
            var list = ____grid.VisibleCards.Where(c => discovered.Contains(c.Id) && !Gated(c)).ToList();
            var viewUpgrades = Traverse.Create(____viewUpgrades).Property<bool>("IsTicked").Value;
            NGame.Instance?.GetInspectCardScreen().Open(list, list.IndexOf(card), viewUpgrades);
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
            if (holder.CardNode?.Model is { } model && Gated(model))
                holder.Hitbox.MouseDefaultCursorShape = Godot.Control.CursorShape.Arrow;
    }
}
