using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace Alchemist.AlchemistCode.Patches;

// A potion's hover tips align Center: the text tips sit under the potion and the card previews sit
// under the text. Three previews are taller than the screen below the belt, so the vertical overflow
// correction slides the card column up over the text. The relic alignment already handles this by
// moving the cards beside the text when the two rects meet; this gives the Center case the same rule
[HarmonyPatch(typeof(NHoverTipSet), nameof(NHoverTipSet.SetAlignment))]
public static class HoverTipOverlapPatch
{
    private static readonly FieldInfo TextContainer =
        AccessTools.Field(typeof(NHoverTipSet), "_textHoverTipContainer");
    private static readonly FieldInfo CardContainer =
        AccessTools.Field(typeof(NHoverTipSet), "_cardHoverTipContainer");

    private const float Gap = 10f;

    public static void Postfix(NHoverTipSet __instance, HoverTipAlignment alignment)
    {
        if (alignment != HoverTipAlignment.Center) return;
        if (TextContainer.GetValue(__instance) is not Control text
            || CardContainer.GetValue(__instance) is not Control cards) return;
        if (cards.GetChildCount() == 0 || !text.GetRect().Intersects(cards.GetRect())) return;
        if (NGame.Instance is not { } game) return;

        var viewport = game.GetViewportRect().Size;
        // Beside the text, on whichever side has room; the potion belt is at the top, so the
        // column starts level with the text and only shifts up if it would run off the bottom
        var right = text.GlobalPosition.X + text.Size.X + Gap;
        var x = right + cards.Size.X <= viewport.X ? right : text.GlobalPosition.X - cards.Size.X - Gap;
        var y = Mathf.Max(0f, Mathf.Min(text.GlobalPosition.Y, viewport.Y - cards.Size.Y));
        cards.GlobalPosition = new Vector2(Mathf.Max(0f, x), y);
    }
}
