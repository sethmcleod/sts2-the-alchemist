using System.Runtime.CompilerServices;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Extensions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.addons.mega_text;

namespace Alchemist.AlchemistCode.Patches;

[HarmonyPatch(typeof(NCard))]
public static class FermentOverlayPatches
{
    private sealed class Overlay
    {
        public required TextureRect Icon;
        public required MegaLabel Label;
        public Tween? PulseTween;
    }

    private static readonly ConditionalWeakTable<NCard, Overlay> Overlays = new();

    private static Texture2D? _iconTexture;

    private static Texture2D IconTexture => _iconTexture ??=
        ResourceLoader.Load<Texture2D>("ui/cards/ferment_overlay.png".ImagePath(), null, ResourceLoader.CacheMode.Reuse);

    // Mirrors EnergyIcon's base offsets of -166..-102
    private const float MirrorLeft = 102f;
    private const float MirrorRight = 166f;

    private const int MaxFontSize = 28;

    private static readonly Color OutlineColor = new("24421F");

    private static readonly Color FlashColor = new("8EDB5C");

    private const float PulseScale = 1.3f;

    private static readonly AccessTools.FieldRef<NHandCardHolder, Control> HolderFlash =
        AccessTools.FieldRefAccess<NHandCardHolder, Control>("_flash");

    // Not in _Ready: the prewarmed card node pool never passes through that postfix
    private static Overlay? GetOrAddOverlay(NCard card)
    {
        if (Overlays.TryGetValue(card, out var overlay)) return overlay;
        if (card.GetNodeOrNull<TextureRect>("%EnergyIcon") is not { } energy) return null;

        var icon = (TextureRect)energy.Duplicate();
        icon.Name = "FermentOverlay";
        icon.UniqueNameInOwner = false;
        icon.Visible = false;
        icon.OffsetLeft = MirrorLeft;
        icon.OffsetRight = MirrorRight;
        icon.Texture = IconTexture;

        if (icon.GetNodeOrNull<Node>("UnplayableEnergyIcon") is { } slash)
        {
            icon.RemoveChild(slash);
            slash.QueueFree();
        }

        var label = icon.GetNode<MegaLabel>("EnergyLabel");
        label.Name = "FermentOverlayLabel";
        label.UniqueNameInOwner = false;
        label.MaxFontSize = MaxFontSize;
        label.AddThemeColorOverride("font_color", StsColors.cream);
        label.AddThemeColorOverride("font_outline_color", OutlineColor);

        energy.AddSibling(icon);
        overlay = new Overlay { Icon = icon, Label = label };
        Overlays.Add(card, overlay);
        return overlay;
    }

    [HarmonyPatch(nameof(NCard.UpdateVisuals))] [HarmonyPostfix]
    private static void Refresh(NCard __instance)
    {
        if (GetOrAddOverlay(__instance) is not { } overlay) return;

        if (__instance.Model is not AlchemistCard { IsFermentInline: true, IsMutable: true, CombatState: not null } card
            || __instance.Visibility != ModelVisibility.Visible)
        {
            overlay.Icon.Visible = false;
            return;
        }

        overlay.Icon.Visible = true;
        overlay.Label.SetTextAutoSize(card.FermentTurns.ToString());
    }

    internal static void PlayFermentPulse(AlchemistCard card)
    {
        if (!card.IsMutable || NCard.FindOnTable(card) is not { } node) return;
        FlashHolder(card);
        PulseIcon(node);
    }

    private static void FlashHolder(AlchemistCard card)
    {
        if (NCombatRoom.Instance?.Ui?.Hand.GetCardHolder(card) is not NHandCardHolder holder) return;
        holder.Flash();
        var flash = HolderFlash(holder);
        if (GodotObject.IsInstanceValid(flash))
            flash.Modulate = FlashColor with { A = flash.Modulate.A };
    }

    private static void PulseIcon(NCard node)
    {
        if (GetOrAddOverlay(node) is not { } overlay || !GodotObject.IsInstanceValid(overlay.Icon)) return;
        var icon = overlay.Icon;
        overlay.PulseTween?.Kill();
        icon.PivotOffset = icon.Size / 2f;
        icon.Scale = Vector2.One;
        var tween = icon.CreateTween();
        tween.TweenProperty(icon, "scale", Vector2.One * PulseScale, 0.12)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(icon, "scale", Vector2.One, 0.3)
            .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        overlay.PulseTween = tween;
    }
}
