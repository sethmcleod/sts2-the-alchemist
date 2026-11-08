using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Alchemist.AlchemistCode.Config;
using Alchemist.AlchemistCode.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Alchemist.AlchemistCode.Patches;

public static class AntitoxinBarPatches
{
    private const float PanelGap = 3f;
    private const float CombatGap = 8f;
    private const float PanelBarShrink = 2f;
    private const float PanelDrop = 3f;

    private static float GapFor(Node bar) => InPlayerPanel(bar) ? PanelGap : CombatGap;

    private static readonly Color TextColor = new("efe6ff");
    private static readonly Color TextOutline = new("2e0f52");
    private static readonly Color DrainedColor = new("76FF40");
    private static readonly Color DrainedOutline = new("074700");
    // Must match Minty Spire's incoming-damage label
    private const float ForecastPad = 6f;
    private const float ForecastWidth = 20f;
    private const int ForecastFallbackSize = 14;

    private static System.Reflection.PropertyInfo? _mintySize;
    private static bool _mintyLooked;

    private static int ForecastFontSize()
    {
        if (!_mintyLooked)
        {
            _mintyLooked = true;
            _mintySize = AccessTools.TypeByName("MintySpire2.MintySpire2Code.Config")
                ?.GetProperty("IncomingDamageSize");
        }
        return _mintySize?.GetValue(null) is int size ? size : ForecastFallbackSize;
    }
    private static readonly Color PulseBright = new(1.35f, 1.35f, 1.35f);
    private static readonly Color EmptyColor = new("b9b3c4");
    private static readonly Color EmptyOutline = new("2a2433");

    private sealed class Parts
    {
        public Control Root = null!;
        public Control Foreground = null!;
        public NinePatchRect? Incoming;
        public Label Text = null!;
        public Label? Forecast;
        public int LastKnown;
        public Tween? FadeTween;
        public Tween? PulseTween;
    }

    private static readonly ConditionalWeakTable<NHealthBar, Parts> Bars = new();

    internal static Control? BarNode(NHealthBar bar) =>
        Bars.TryGetValue(bar, out var parts) && GodotObject.IsInstanceValid(parts.Root) && parts.Root.Visible
            ? parts.Root
            : null;

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
            // NHealthBar is pooled, so _Ready can run again on a built instance
            if (Bars.TryGetValue(__instance, out var existing))
            {
                if (GodotObject.IsInstanceValid(existing.Root)) return;
                Bars.Remove(__instance);
            }

            if (hp.Duplicate((int)Node.DuplicateFlags.UseInstantiation) is not Control clone) return;
            clone.Name = "AlchemistAntitoxinBar";
            clone.Visible = false;
            hp.GetParent()?.AddChild(clone);
            // After HpBarContainer, not last: the block shield is a later sibling and must draw over this bar
            hp.GetParent()?.MoveChild(clone, hp.GetIndex() + 1);

            foreach (var path in new[] { "BlockOutline", "InfinityTex",
                         "HpForegroundContainer/Mask/DoomForeground",
                         "HpForegroundContainer/Mask/HpMiddleground" })
                Hide(clone, path);

            var fg = clone.GetNodeOrNull<Control>("HpForegroundContainer/Mask/HpForeground");
            var label = clone.GetNodeOrNull<Label>("HpLabel");
            if (fg == null || label == null) { clone.QueueFree(); return; }

            if (fg is CanvasItem fgItem) fgItem.SelfModulate = AlchemistModConfig.AntitoxinBarColor;

            label.AddThemeColorOverride("font_color", TextColor);
            label.AddThemeColorOverride("font_outline_color", TextOutline);
            label.VerticalAlignment = VerticalAlignment.Center;

            var incoming = clone.GetNodeOrNull<NinePatchRect>("HpForegroundContainer/Mask/PoisonForeground");
            if (incoming != null) incoming.Visible = false;

            var forecast = new Label
            {
                Name = "PoisonForecastLabel",
                Visible = false,
                Text = "",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            if (ResourceLoader.Load<Font>("res://fonts/kreon_bold.ttf") is { } forecastFont)
                forecast.AddThemeFontOverride("font", forecastFont);
            forecast.AddThemeColorOverride("font_color", AlchemistModConfig.PoisonForecastColor);
            clone.AddChild(forecast);

            Bars.Add(__instance, new Parts
            {
                Root = clone,
                Foreground = fg,
                Text = label,
                Incoming = incoming,
                Forecast = forecast,
            });
        }
    }

    private static readonly AccessTools.FieldRef<NCreatureStateDisplay, NPowerContainer> PowersRef =
        AccessTools.FieldRefAccess<NCreatureStateDisplay, NPowerContainer>("_powerContainer");

    private static readonly AccessTools.FieldRef<NPowerContainer, Vector2?> PowerOriginRef =
        AccessTools.FieldRefAccess<NPowerContainer, Vector2?>("_originalPosition");

    private static readonly ConditionalWeakTable<NPowerContainer, object> Shifted = new();

    private static readonly ConditionalWeakTable<Control, object> HpBaseY = new();

    private static float Lift(Control hp) => (hp.Size.Y + PanelGap) / 2f - PanelDrop;

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

    // UpdatePositions rebuilds from _originalPosition, so that cached origin has to move too
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

    private static void StopPulse(Parts parts)
    {
        parts.PulseTween?.Kill();
        parts.PulseTween = null;
        if (parts.Incoming is { } green && GodotObject.IsInstanceValid(green))
            green.Modulate = Colors.White;
    }

    [HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.RefreshValues))]
    public static class Refresh
    {
        public static void Postfix(NHealthBar __instance)
        {
            if (!Bars.TryGetValue(__instance, out var parts)) return;
            if (!GodotObject.IsInstanceValid(parts.Root)) return;
            var creature = CreatureRef(__instance);
            if (!Shows(creature)) { parts.Root.Visible = false; StopPulse(parts); return; }

            if (!AlchemistModConfig.ShowAllyAntitoxinBars && InPlayerPanel(__instance)
                && !MegaCrit.Sts2.Core.Context.LocalContext.IsMe(creature))
            {
                parts.Root.Visible = false;
                StopPulse(parts);
                if (__instance.HpBarContainer is { } stock)
                {
                    if (HpBaseY.TryGetValue(stock, out var stockY))
                        stock.Position = new Vector2(stock.Position.X, (float)stockY);
                    if (__instance.GetNodeOrNull<Control>("%BlockContainer") is { } stockShield
                        && HpBaseY.TryGetValue(stockShield, out var stockShieldY))
                        stockShield.Position = new Vector2(stockShield.Position.X, (float)stockShieldY);
                }
                return;
            }

            // Combat teardown removes every power, so hold the last in-combat value
            var inCombat = CombatManager.Instance?.IsInProgress ?? false;
            var live = creature.GetPowerAmount<AntitoxinPower>();
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

                if (__instance.GetNodeOrNull<Control>("%BlockContainer") is { } shield)
                {
                    if (!HpBaseY.TryGetValue(shield, out var shieldY))
                    {
                        shieldY = shield.Position.Y;
                        HpBaseY.Add(shield, shieldY);
                    }
                    shield.Position = new Vector2(shield.Position.X, (float)shieldY - Lift(hp));
                }
            }

            if (hp.GetNodeOrNull<Label>("HpLabel") is { } hpLabel)
            {
                var hpSize = hpLabel.GetThemeFontSize("font_size");
                if (hpSize > 0 && parts.Text.GetThemeFontSize("font_size") != hpSize)
                    parts.Text.AddThemeFontSizeOverride("font_size", hpSize);

                // The party panel shows and hides the HP number on its own paths, so copy its alpha outside our fade
                var fading = parts.FadeTween is { } t && GodotObject.IsInstanceValid(t) && t.IsRunning();
                if (!fading && !Mathf.IsEqualApprox(parts.Text.Modulate.A, hpLabel.Modulate.A))
                {
                    var m = parts.Text.Modulate;
                    m.A = hpLabel.Modulate.A;
                    parts.Text.Modulate = m;
                }
                var forecastAlpha = InPlayerPanel(__instance) ? 1f : hpLabel.Modulate.A;
                if (!fading && parts.Forecast is { } fcl && GodotObject.IsInstanceValid(fcl)
                    && !Mathf.IsEqualApprox(fcl.Modulate.A, forecastAlpha))
                {
                    var m2 = fcl.Modulate;
                    m2.A = forecastAlpha;
                    fcl.Modulate = m2;
                }
            }

            parts.Root.Visible = true;
            parts.Root.Position = new Vector2(hp.Position.X, hp.Position.Y + hp.Size.Y + GapFor(__instance));
            parts.Root.Size = InPlayerPanel(__instance)
                ? new Vector2(hp.Size.X, hp.Size.Y - PanelBarShrink)
                : hp.Size;

            var full = parts.Root.GetNodeOrNull<Control>("HpForegroundContainer")?.Size.X ?? hp.Size.X;
            var current = inCombat ? live : parts.LastKnown;

            // Amount, not CalculateTotalDamageNextTurn, which already applies Antitoxin's reduction
            var poison = creature.GetPower<PoisonPower>();
            var incoming = poison?.Amount ?? 0;
            var forecast = Mathf.Min(current, incoming);
            var freeRatio = current > 0 ? Mathf.Clamp((float)(current - forecast) / current, 0f, 1f) : 0f;

            var damageNext = inCombat && poison != null ? poison.CalculateTotalDamageNextTurn() : 0;

            // A NinePatchRect can't render narrower than its patch margins, so it's hidden outright
            var covered = current == 0 || forecast >= current;
            var warning = current > 0 && damageNext > 0;
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
                    green.OffsetRight = 0f;
                }
            }

            if (parts.Forecast is { } number && GodotObject.IsInstanceValid(number))
            {
                var showForecast = damageNext > 0 && AlchemistModConfig.ShowPoisonForecast;
                number.Visible = showForecast;
                if (showForecast)
                {
                    var fontSize = ForecastFontSize();
                    if (number.GetThemeFontSize("font_size") != fontSize)
                        number.AddThemeFontSizeOverride("font_size", fontSize);
                    var fontHeight = number.GetThemeFont("font")?.GetHeight(fontSize) ?? fontSize;
                    number.AddThemeColorOverride("font_color", AlchemistModConfig.PoisonForecastColor);
                    number.Text = $"←{damageNext}";
                    number.Size = new Vector2(ForecastWidth, fontHeight);
                    number.Position = new Vector2(hp.Size.X + ForecastPad, (hp.Size.Y - fontHeight) / 2f);
                }
            }

            var overlayLive = parts.Incoming is { } o && GodotObject.IsInstanceValid(o) && o.Visible;
            if (warning && overlayLive)
            {
                var running = parts.PulseTween is { } p && GodotObject.IsInstanceValid(p) && p.IsRunning();
                if (!running)
                {
                    var target = parts.Incoming!;
                    parts.PulseTween = target.CreateTween().SetLoops();
                    parts.PulseTween.TweenProperty(target, "modulate", PulseBright, 0.7)
                        .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                    parts.PulseTween.TweenProperty(target, "modulate", Colors.White, 0.7)
                        .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                }
            }
            else
            {
                StopPulse(parts);
            }
            ShiftPowersOnce(__instance, hp.Size.Y);
        }
    }

    // PowerModel.HoverTips is empty for a hidden power, so the Antitoxin tips go on the creature
    [HarmonyPatch(typeof(Creature), nameof(Creature.HoverTips), MethodType.Getter)]
    public static class KeepTips
    {
        public static void Postfix(Creature __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (!Shows(__instance)) return;
            var tips = __result.ToList();
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

    // SetCreatureBounds runs more than once, so the base height is cached and recomputed
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
            // Must stay a no-op, not a throw, when another mod replaces the bar scene
            if (Shows(DisplayCreatureRef(__instance))
                && __instance.GetNodeOrNull<NHealthBar>("%HealthBar") is { } healthBar
                && Bars.TryGetValue(healthBar, out var parts)
                && GodotObject.IsInstanceValid(parts.Root))
                extra = parts.Root.Size.Y + CombatGap;

            hitbox.Size = new Vector2(hitbox.Size.X, baseHeight + extra);

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

    // Each fade kills the previous tween, or two live tweens strand the label at a stale alpha
    [HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.FadeOutHpLabel))]
    public static class FadeOutAntitoxinLabel
    {
        public static void Postfix(NHealthBar __instance, float duration, float finalAlpha)
        {
            if (!Bars.TryGetValue(__instance, out var parts) || !GodotObject.IsInstanceValid(parts.Text)) return;
            parts.FadeTween?.Kill();
            parts.FadeTween = parts.Text.CreateTween().SetParallel();
            parts.FadeTween.TweenProperty(parts.Text, "modulate:a", finalAlpha, duration)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
            if (!InPlayerPanel(__instance) && parts.Forecast is { } forecast
                && GodotObject.IsInstanceValid(forecast))
                parts.FadeTween.TweenProperty(forecast, "modulate:a", finalAlpha, duration)
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
            parts.FadeTween = parts.Text.CreateTween().SetParallel();
            parts.FadeTween.TweenProperty(parts.Text, "modulate:a", 1f, duration);
            if (!InPlayerPanel(__instance) && parts.Forecast is { } forecast
                && GodotObject.IsInstanceValid(forecast))
                parts.FadeTween.TweenProperty(forecast, "modulate:a", 1f, duration);
        }
    }
}
