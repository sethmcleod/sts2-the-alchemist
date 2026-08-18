using System.Runtime.CompilerServices;
using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Collections.Generic;
using System.Linq;

namespace Alchemist.AlchemistCode.Patches;

// A purple Antitoxin bar under the health bar. It is a duplicate of the game's own HpBarContainer rather
// than hand-built rectangles, so it inherits the nine-patch textures, the tapered ends, the exact width
// and the track colour for free, and it keeps matching if the base art changes. Only the filled part is
// recoloured; the empty track is left as the health bar draws it
public static class AntitoxinBarPatches
{
    // Clears the HP number, which is drawn larger than its own 16px bar and overflows it. Tightened
    // as far as the two sets of digits allow without touching
    private const float Gap = 10f;

    private static readonly Color TextColor = new("efe6ff");
    private static readonly Color TextOutline = new("2e0f52");
    // The same pair the health bar uses when Poison is lethal, reused for "this tick empties the bar"
    private static readonly Color DrainedColor = new("76FF40");
    private static readonly Color DrainedOutline = new("074700");

    private sealed class Parts
    {
        public Control Root = null!;
        public Control Foreground = null!;
        public NinePatchRect? Incoming;
        public Label Text = null!;
        public int LastKnown;
    }

    private static readonly ConditionalWeakTable<NHealthBar, Parts> Bars = new();

    private static readonly AccessTools.FieldRef<NHealthBar, Creature> CreatureRef =
        AccessTools.FieldRefAccess<NHealthBar, Creature>("_creature");

    internal static bool Shows(Creature? creature) =>
        creature is { IsAlive: true }
        && (creature.GetPowerAmount<AntitoxinPower>() > 0
            || creature.Player?.Character is Character.Alchemist);

    private static void Hide(Node parent, string path)
    {
        if (parent.GetNodeOrNull<CanvasItem>(path) is { } node) node.Visible = false;
    }

    [HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar._Ready))]
    public static class Build
    {
        public static void Postfix(NHealthBar __instance)
        {
            if (__instance.HpBarContainer is not { } hp) return;
            // NHealthBar is pooled, so _Ready can run again on an instance we already built for
            if (Bars.TryGetValue(__instance, out var existing))
            {
                if (GodotObject.IsInstanceValid(existing.Root)) return;
                Bars.Remove(__instance);
            }

            if (hp.Duplicate((int)Node.DuplicateFlags.UseInstantiation) is not Control clone) return;
            clone.Name = "AlchemistAntitoxinBar";
            clone.Visible = false;
            hp.GetParent()?.AddChild(clone);

            // The duplicate carries the whole HP apparatus. Everything that is not the bar itself goes
            foreach (var path in new[] { "BlockOutline", "InfinityTex",
                         "HpForegroundContainer/Mask/DoomForeground",
                         "HpForegroundContainer/Mask/HpMiddleground" })
                Hide(clone, path);

            var fg = clone.GetNodeOrNull<Control>("HpForegroundContainer/Mask/HpForeground");
            var label = clone.GetNodeOrNull<Label>("HpLabel");
            if (fg == null || label == null) { clone.QueueFree(); return; }

            if (fg is CanvasItem fgItem) fgItem.SelfModulate = AlchemistModConfig.AntitoxinBarColor;

            // The duplicate inherited the HP label's red-on-cream styling at full size, which both clashes
            // with the purple and collides with the HP number above it
            label.AddThemeColorOverride("font_color", TextColor);
            label.AddThemeColorOverride("font_outline_color", TextOutline);
            label.AddThemeFontSizeOverride("font_size", 22);
            label.VerticalAlignment = VerticalAlignment.Center;

            // The health bar's own poison overlay is already the right green, so it is reused as-is to
            // show the slice of Antitoxin the incoming tick will eat
            var incoming = clone.GetNodeOrNull<NinePatchRect>("HpForegroundContainer/Mask/PoisonForeground");
            if (incoming != null) incoming.Visible = false;

            Bars.Add(__instance, new Parts
            {
                Root = clone, Foreground = fg, Text = label, Incoming = incoming,
            });
        }
    }

    private static readonly AccessTools.FieldRef<NCreatureStateDisplay, NPowerContainer> PowersRef =
        AccessTools.FieldRefAccess<NCreatureStateDisplay, NPowerContainer>("_powerContainer");

    private static readonly AccessTools.FieldRef<NPowerContainer, Vector2?> PowerOriginRef =
        AccessTools.FieldRefAccess<NPowerContainer, Vector2?>("_originalPosition");

    private static readonly ConditionalWeakTable<NPowerContainer, object> Shifted = new();

    // Called from Refresh, not _Ready: HpBarContainer has no size until the first layout pass.
    // UpdatePositions rebuilds Position from the cached _originalPosition on every power Add and Remove,
    // so the cached origin has to move too or the row snaps back the first time a power lands
    private static void ShiftPowersOnce(NHealthBar bar, float barHeight)
    {
        if (barHeight <= 0f) return;
        Node? node = bar;
        while (node != null && node is not NCreatureStateDisplay) node = node.GetParent();
        if (node is not NCreatureStateDisplay display) return;
        if (PowersRef(display) is not { } powers) return;
        if (Shifted.TryGetValue(powers, out _)) return;
        Shifted.Add(powers, new object());

        var offset = new Vector2(0f, barHeight + Gap);
        powers.Position += offset;
        if (PowerOriginRef(powers) is { } origin) PowerOriginRef(powers) = origin + offset;
    }

    [HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.RefreshValues))]
    public static class Refresh
    {
        public static void Postfix(NHealthBar __instance)
        {
            if (!Bars.TryGetValue(__instance, out var parts)) return;
            if (!GodotObject.IsInstanceValid(parts.Root)) return;
            var creature = CreatureRef(__instance);
            if (!Shows(creature)) { parts.Root.Visible = false; return; }

            // Combat teardown removes every power, so the live amount drops to 0 while the end of
            // combat is still on screen. Hold the last in-combat value instead of blanking the bar
            var inCombat = CombatManager.Instance?.IsInProgress ?? false;
            var live = creature.GetPowerAmount<AntitoxinPower>();
            if (inCombat) parts.LastKnown = live;

            var hp = __instance.HpBarContainer;
            parts.Root.Visible = true;
            parts.Root.Position = new Vector2(hp.Position.X, hp.Position.Y + hp.Size.Y + Gap);
            parts.Root.Size = hp.Size;

            // The real bar shrinks its foreground with a negative OffsetRight, so this matches it
            var full = parts.Root.GetNodeOrNull<Control>("HpForegroundContainer")?.Size.X ?? hp.Size.X;
            var current = inCombat ? live : parts.LastKnown;
            var max = AntitoxinPower.MaxFor(creature);
            var ratio = max > 0 ? Mathf.Clamp((float)current / max, 0f, 1f) : 0f;
            // What the next Poison tick will spend. Amount, not CalculateTotalDamageNextTurn, because
            // that already runs through Antitoxin's own reduction and so reports what gets past it
            var incoming = creature.GetPower<PoisonPower>()?.Amount ?? 0;
            var spent = Mathf.Min(current, incoming);
            var afterRatio = max > 0 ? Mathf.Clamp((float)(current - spent) / max, 0f, 1f) : 0f;

            // A NinePatchRect cannot render narrower than its own patch margins, so a foreground shrunk
            // to zero still leaves a purple stub. Hide it outright at zero and when the tick takes it all
            var drainedFully = current > 0 && spent >= current;
            parts.Foreground.Visible = current > 0 && !drainedFully;
            parts.Foreground.SelfModulate = AlchemistModConfig.AntitoxinBarColor;
            parts.Foreground.OffsetRight = full * afterRatio - full;
            parts.Text.Text = $"{current}/{max}";

            parts.Text.AddThemeColorOverride("font_color", drainedFully ? DrainedColor : TextColor);
            parts.Text.AddThemeColorOverride("font_outline_color",
                drainedFully ? DrainedOutline : TextOutline);

            if (parts.Incoming is { } green)
            {
                green.Visible = spent > 0;
                if (spent > 0)
                {
                    green.OffsetLeft = drainedFully
                        ? 0f
                        : Mathf.Max(0f, full * afterRatio - green.PatchMarginLeft);
                    green.OffsetRight = full * ratio - full;
                }
            }
            ShiftPowersOnce(__instance, hp.Size.Y);
        }
    }


    // Both Antitoxin powers are hidden so the bar is the only place the number appears, and
    // PowerModel.HoverTips goes empty the moment a power is invisible. Put the tips back on the creature,
    // which is where a player hovers to ask what the purple bar is
    [HarmonyPatch(typeof(Creature), nameof(Creature.HoverTips), MethodType.Getter)]
    public static class KeepTips
    {
        public static void Postfix(Creature __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (!Shows(__instance)) return;
            var tips = __result.ToList();
            // MegaTryAddingTip de-duplicates, so a visible power supplying the same tip stays single.
            // TipFor, not FromPower: the hovered creature is the only thing that knows its own limit
            tips.MegaTryAddingTip(AntitoxinPower.TipFor(__instance));
            tips.MegaTryAddingTip(HoverTipFactory.FromPower<PoisonPower>());
            __result = tips;
        }
    }

    private static readonly AccessTools.FieldRef<NCreatureStateDisplay, Control> HpHitboxRef =
        AccessTools.FieldRefAccess<NCreatureStateDisplay, Control>("_hpBarHitbox");

    private static readonly AccessTools.FieldRef<NCreatureStateDisplay, Creature> DisplayCreatureRef =
        AccessTools.FieldRefAccess<NCreatureStateDisplay, Creature>("_creature");

    private static readonly ConditionalWeakTable<Control, object> HitboxBaseHeight = new();

    // The HP bar has its own hitbox and the Antitoxin bar is drawn below it, outside that box, so the
    // tips only appeared on the health half. Grow the box to cover both. SetCreatureBounds runs more
    // than once, so the untouched height is cached and the result recomputed rather than accumulated
    [HarmonyPatch(typeof(NCreatureStateDisplay), nameof(NCreatureStateDisplay.SetCreatureBounds))]
    public static class ExtendHpHover
    {
        public static void Postfix(NCreatureStateDisplay __instance)
        {
            if (HpHitboxRef(__instance) is not { } hitbox) return;
            if (!HitboxBaseHeight.TryGetValue(hitbox, out var cached))
            {
                cached = hitbox.Size.Y;
                HitboxBaseHeight.Add(hitbox, cached);
            }
            var baseHeight = (float)cached;

            var extra = 0f;
            if (Shows(DisplayCreatureRef(__instance))
                && Bars.TryGetValue(__instance.GetNodeOrNull<NHealthBar>("%HealthBar")!, out var parts)
                && GodotObject.IsInstanceValid(parts.Root))
                extra = parts.Root.Size.Y + Gap;

            hitbox.Size = new Vector2(hitbox.Size.X, baseHeight + extra);

            // The nameplate sits under the HP bar, exactly where the Antitoxin bar now is, so on hover the
            // name printed over the purple. Move it below the bar. Same cache-and-recompute as the hitbox
            if (NameplateRef(__instance) is not { } nameplate) return;
            if (!NameplateBaseY.TryGetValue(nameplate, out var cachedY))
            {
                cachedY = nameplate.Position.Y;
                NameplateBaseY.Add(nameplate, cachedY);
            }
            nameplate.Position = new Vector2(nameplate.Position.X, (float)cachedY + extra);
        }
    }

    private static readonly AccessTools.FieldRef<NCreatureStateDisplay, Control> NameplateRef =
        AccessTools.FieldRefAccess<NCreatureStateDisplay, Control>("_nameplateContainer");

    private static readonly ConditionalWeakTable<Control, object> NameplateBaseY = new();

    // Hovering the bars fades the HP number so the bar underneath can be read. The Antitoxin number
    // is drawn the same way, so it follows the same fade
    [HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.FadeOutHpLabel))]
    public static class FadeOutAntitoxinLabel
    {
        public static void Postfix(NHealthBar __instance, float duration, float finalAlpha)
        {
            if (!Bars.TryGetValue(__instance, out var parts) || !GodotObject.IsInstanceValid(parts.Text)) return;
            parts.Text.CreateTween().TweenProperty(parts.Text, "modulate:a", finalAlpha, duration)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        }
    }

    [HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.FadeInHpLabel))]
    public static class FadeInAntitoxinLabel
    {
        public static void Postfix(NHealthBar __instance, float duration)
        {
            if (!Bars.TryGetValue(__instance, out var parts) || !GodotObject.IsInstanceValid(parts.Text)) return;
            parts.Text.CreateTween().TweenProperty(parts.Text, "modulate:a", 1f, duration);
        }
    }
}
