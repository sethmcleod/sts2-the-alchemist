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
    [HarmonyPatch(typeof(NInspectCardScreen), "UpdateCardDisplay")] [HarmonyPostfix]
    private static void UseBigPortrait(NCard ____card)
    {
        if (____card?.Model is not AlchemistCard card || card.BigPortraitPath is not { } big) return;
        var portrait = ____card.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait != null)
            portrait.Texture = ResourceLoader.Load<Texture2D>(big, null, ResourceLoader.CacheMode.Reuse);
    }

    // NCard keeps its portrait until UpdatePortrait runs again, so a switch of the Use Beta Art setting
    // would only show on the next screen. This repaints the cards already in the tree
    private static Action<NCard>? _updatePortrait;

    private static Action<NCard>? UpdatePortrait => _updatePortrait ??=
        AccessTools.Method(typeof(NCard), "UpdatePortrait") is { } method
            ? AccessTools.MethodDelegate<Action<NCard>>(method)
            : null;

    private static bool? _lastUseBetaArt;

    public static void RefreshPortraitsIfArtSourceChanged()
    {
        if (_lastUseBetaArt == AlchemistModConfig.UseBetaArt) return;
        _lastUseBetaArt = AlchemistModConfig.UseBetaArt;
        if (Engine.GetMainLoop() is SceneTree tree) Repaint(tree.Root);
    }

    private static void Repaint(Node node)
    {
        if (node is NCard { Model: not null } card) UpdatePortrait?.Invoke(card);
        foreach (var child in node.GetChildren()) Repaint(child);
    }
}
