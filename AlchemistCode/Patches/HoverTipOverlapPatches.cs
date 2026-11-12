using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace Alchemist.AlchemistCode.Patches;

// Center-aligned potion tips let the overflow fix slide card previews over the text
[HarmonyPatch(typeof(NHoverTipSet), nameof(NHoverTipSet.SetAlignment), typeof(Control), typeof(HoverTipAlignment))]
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
        var right = text.GlobalPosition.X + text.Size.X + Gap;
        var x = right + cards.Size.X <= viewport.X ? right : text.GlobalPosition.X - cards.Size.X - Gap;
        var y = Mathf.Max(0f, Mathf.Min(text.GlobalPosition.Y, viewport.Y - cards.Size.Y));
        cards.GlobalPosition = new Vector2(Mathf.Max(0f, x), y);
    }
}
