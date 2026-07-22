using Alchemist.AlchemistCode.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace Alchemist.AlchemistCode.Patches;

// The base game hand glow has three colors, all fixed in NHandCardHolder.UpdateCard: cyan for a playable
// card, gold for a card with a bonus now, and red for a warning. A Seep card needs the opposite message of
// gold, "leave this in hand", so it gets a fourth color, a deep green
public static class SeepGlow
{
    public static readonly Color Color = new(0.157f, 0.5f, 0.071f, 0.98f);

    // UpdateCard returns early in three cases, and a postfix still runs after each one. Repeat those
    // conditions here, or the glow paints on a card that the base game left alone
    public static bool ShouldPaint(NHandCardHolder holder)
    {
        if (!holder.IsNodeReady() || holder.CardNode == null) return false;
        if (!CombatManager.Instance.IsInProgress) return false;
        // A selection prompt drives the glow itself, where the color means "selectable"
        if (holder.InSelectMode) return false;
        if (holder.CardNode.Model is not AlchemistCard card || !card.ShouldGlowSeep) return false;
        // Match the phase gate of the gold and the red glow
        return card.Owner?.PlayerCombatState?.Phase == PlayerTurnPhase.Play;
    }
}

// Paint after the base method, so the green replaces the color it chose. AnimShow also covers the card
// that the player cannot pay for: Seep does not care about energy, and that card is the one most likely
// to stay in hand, so it glows where the base game shows nothing
[HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard))]
public static class SeepGlowUpdateCardPatch
{
    public static void Postfix(NHandCardHolder __instance)
    {
        if (!SeepGlow.ShouldPaint(__instance)) return;
        __instance.CardNode!.CardHighlight.AnimShow();
        __instance.CardNode.CardHighlight.Modulate = SeepGlow.Color;
    }
}
