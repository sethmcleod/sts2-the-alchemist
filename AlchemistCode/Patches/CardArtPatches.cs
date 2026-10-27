using System;
using Godot;
using HarmonyLib;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Config;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace Alchemist.AlchemistCode.Patches;

// The game draws every card from one portrait texture, whatever the card's scale. The inspect screen
// (compendium, deck view, shop, card rewards) shows one card at twice its size, and that is the only
// view where the 1000x760 copy of the final art is sharper than the 500x380 copy every other view uses
[HarmonyPatch]
public static class CardArtPatches
{
    private static readonly StringName AlchemistCards = "alchemist_cards";

    [HarmonyPatch(typeof(NCard), "UpdatePortrait")] [HarmonyPostfix]
    private static void AfterUpdatePortrait(NCard __instance)
    {
        if (__instance.Model is not AlchemistCard card) return;
        if (!__instance.IsInGroup(AlchemistCards)) __instance.AddToGroup(AlchemistCards);
        if (card.BigPortraitPath is not { } big || __instance.GetParent() is not NInspectCardScreen) return;
        var portrait = __instance.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait != null)
            portrait.Texture = ResourceLoader.Load<Texture2D>(big, null, ResourceLoader.CacheMode.Reuse);
    }

    // NCard keeps its portrait until UpdatePortrait runs again, so a Beta Art switch would only show on the
    // next screen. This repaints the Alchemist cards already in the tree
    private static Action<NCard>? _updatePortrait;

    private static Action<NCard>? UpdatePortrait => _updatePortrait ??=
        AccessTools.Method(typeof(NCard), "UpdatePortrait") is { } method
            ? AccessTools.MethodDelegate<Action<NCard>>(method)
            : null;

    private static bool _lastUseBetaArt;
    private static string _lastBetaArtExceptions = "";

    public static void RememberArtSource()
    {
        _lastUseBetaArt = AlchemistModConfig.UseBetaArt;
        _lastBetaArtExceptions = AlchemistModConfig.BetaArtExceptions;
    }

    public static void RefreshPortraitsIfArtSourceChanged()
    {
        if (_lastUseBetaArt != AlchemistModConfig.UseBetaArt) BetaArtChoices.Clear();
        else if (_lastBetaArtExceptions == AlchemistModConfig.BetaArtExceptions) return;
        RememberArtSource();
        Repaint(null);
    }

    public static void RepaintCard(string cardId)
    {
        RememberArtSource();
        Repaint(cardId);
    }

    private static void Repaint(string? cardId)
    {
        if (Engine.GetMainLoop() is not SceneTree tree || UpdatePortrait is not { } update) return;
        foreach (var node in tree.GetNodesInGroup(AlchemistCards))
        {
            if (node is NCard { Model: AlchemistCard model } card && (cardId == null || model.Id.Entry == cardId))
                update(card);
        }
    }
}
