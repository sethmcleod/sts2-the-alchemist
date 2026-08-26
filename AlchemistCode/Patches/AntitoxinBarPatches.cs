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
    // The pair sits inside the space one bar used to occupy, so the gap is only what keeps the two
    // sets of digits from touching
    // The panel frame is tight, so the pair sits closer there than under the combat sprite, where
    // the two sets of digits need room not to touch
    private const float PanelGap = 3f;
    private const float CombatGap = 8f;

    private static float GapFor(Node bar) => InPlayerPanel(bar) ? PanelGap : CombatGap;

    private static readonly Color TextColor = new("efe6ff");
    private static readonly Color TextOutline = new("2e0f52");
    // The same pair the health bar uses when Poison is lethal, reused for "this tick empties the bar"
    private static readonly Color DrainedColor = new("76FF40");
    private static readonly Color DrainedOutline = new("074700");
    // A plain empty bar is quiet, not a warning
    private static readonly Color EmptyColor = new("b9b3c4");
    private static readonly Color EmptyOutline = new("2a2433");

    private sealed class Parts
    {
        public Control Root = null!;
        public Control Foreground = null!;
        public NinePatchRect? Incoming;
        public Label Text = null!;
        public int LastKnown;
        public Tween? FadeTween;
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

    // The player panel's frame is a fixed size, so the two bars share the space one bar had rather
    // than growing the box: HP rises by half the added block and Antitoxin takes the half below
    private static readonly ConditionalWeakTable<Control, object> HpBaseY = new();

    private static float Lift(Control hp) => (hp.Size.Y + PanelGap) / 2f;

    // Only the player panel is boxed by a fixed golden frame. The combat bar hangs under the
    // creature with room below it, so it keeps its own position and the pair grows downward
    private static bool InPlayerPanel(Node bar)
    {
        Node? node = bar;
        while (node != null)
        {
            if (node is NCreatureStateDisplay) return false;
            node = node.GetParent();
        }
        return true;
    }

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

        var offset = new Vector2(0f, barHeight + CombatGap);
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
            // A gain gets a quiet pulse on the bar, softer than the splash an absorb makes
            if (inCombat && live > parts.LastKnown && GodotObject.IsInstanceValid(parts.Foreground))
            {
                var fg = parts.Foreground;
                var tween = fg.CreateTween();
                tween.TweenProperty(fg, "modulate", new Color(1.6f, 1.5f, 1.9f), 0.08);
                tween.TweenProperty(fg, "modulate", Colors.White, 0.45)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
            }
            if (inCombat) parts.LastKnown = live;

            if (__instance.HpBarContainer is not { } hp) return;
            if (InPlayerPanel(__instance))
            {
                if (!HpBaseY.TryGetValue(hp, out var baseY)) { baseY = hp.Position.Y; HpBaseY.Add(hp, baseY); }
                hp.Position = new Vector2(hp.Position.X, (float)baseY - Lift(hp));
            }

            if (hp.GetNodeOrNull<Label>("HpLabel") is { } hpLabel)
            {
                var hpSize = hpLabel.GetThemeFontSize("font_size");
                if (hpSize > 0 && parts.Text.GetThemeFontSize("font_size") != hpSize)
                    parts.Text.AddThemeFontSizeOverride("font_size", hpSize);

                // The party panel shows and hides its HP number by paths of its own (it starts the
                // label hidden and restores it on highlight), so the fade postfixes alone can strand
                // this label at a stale alpha. Outside our own fade, copy the HP label's alpha:
                // whatever the panel decided is correct for both numbers
                var fading = parts.FadeTween is { } t && GodotObject.IsInstanceValid(t) && t.IsRunning();
                if (!fading && !Mathf.IsEqualApprox(parts.Text.Modulate.A, hpLabel.Modulate.A))
                {
                    var m = parts.Text.Modulate;
                    m.A = hpLabel.Modulate.A;
                    parts.Text.Modulate = m;
                }
            }

            parts.Root.Visible = true;
            parts.Root.Position = new Vector2(hp.Position.X, hp.Position.Y + hp.Size.Y + GapFor(__instance));
            parts.Root.Size = hp.Size;

            // The real bar shrinks its foreground with a negative OffsetRight, so this matches it
            var full = parts.Root.GetNodeOrNull<Control>("HpForegroundContainer")?.Size.X ?? hp.Size.X;
            var current = inCombat ? live : parts.LastKnown;

            // The bar is the amount held, so the purple stays whole until the player has none. The
            // green overlay is the forecast: the slice of that capacity the next tick will need.
            // Amount, not CalculateTotalDamageNextTurn, because that already runs through Antitoxin's
            // own reduction and so reports only what gets past it
            var incoming = creature.GetPower<PoisonPower>()?.Amount ?? 0;
            var forecast = Mathf.Min(current, incoming);
            var freeRatio = current > 0 ? Mathf.Clamp((float)(current - forecast) / current, 0f, 1f) : 0f;

            // A NinePatchRect cannot render narrower than its own patch margins, so a foreground
            // shrunk to zero still leaves a purple stub. Hide it outright when the forecast covers all
            var covered = current == 0 || forecast >= current;
            // Green warns that the dose has reached the end of the capacity
            var warning = current > 0 && incoming >= current;
            parts.Foreground.Visible = current > 0 && !covered;
            parts.Foreground.SelfModulate = AlchemistModConfig.AntitoxinBarColor;
            parts.Foreground.OffsetRight = full * freeRatio - full;
            parts.Text.Text = $"{current}";

            parts.Text.AddThemeColorOverride("font_color",
                warning ? DrainedColor : current == 0 ? EmptyColor : TextColor);
            parts.Text.AddThemeColorOverride("font_outline_color",
                warning ? DrainedOutline : current == 0 ? EmptyOutline : TextOutline);

            if (parts.Incoming is { } green)
            {
                green.Visible = forecast > 0;
                if (forecast > 0)
                {
                    green.OffsetLeft = covered
                        ? 0f
                        : Mathf.Max(0f, full * freeRatio - green.PatchMarginLeft);
                    // Flush to the end of the bar: the forecast always fills the far side
                    green.OffsetRight = 0f;
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
            // MegaTryAddingTip de-duplicates, so a visible power supplying the same tip stays single
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
            // GetNodeOrNull, and checked: another mod replacing the bar scene must degrade this
            // patch to a no-op, not throw inside the shared display chain
            if (Shows(DisplayCreatureRef(__instance))
                && __instance.GetNodeOrNull<NHealthBar>("%HealthBar") is { } healthBar
                && Bars.TryGetValue(healthBar, out var parts)
                && GodotObject.IsInstanceValid(parts.Root))
                extra = parts.Root.Size.Y + CombatGap;

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
    // is drawn the same way, so it follows the same fade. Each new fade kills the previous tween the
    // way NHealthBar does with _hpLabelFadeTween; two live tweens on one property strand the label
    // at whichever alpha the stale one froze at
    [HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.FadeOutHpLabel))]
    public static class FadeOutAntitoxinLabel
    {
        public static void Postfix(NHealthBar __instance, float duration, float finalAlpha)
        {
            if (!Bars.TryGetValue(__instance, out var parts) || !GodotObject.IsInstanceValid(parts.Text)) return;
            parts.FadeTween?.Kill();
            parts.FadeTween = parts.Text.CreateTween();
            parts.FadeTween.TweenProperty(parts.Text, "modulate:a", finalAlpha, duration)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        }
    }

    [HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.FadeInHpLabel))]
    public static class FadeInAntitoxinLabel
    {
        public static void Postfix(NHealthBar __instance, float duration)
        {
            if (!Bars.TryGetValue(__instance, out var parts) || !GodotObject.IsInstanceValid(parts.Text)) return;
            parts.FadeTween?.Kill();
            parts.FadeTween = parts.Text.CreateTween();
            parts.FadeTween.TweenProperty(parts.Text, "modulate:a", 1f, duration);
        }
    }
}
