using System.Runtime.CompilerServices;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Extensions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.addons.mega_text;

namespace Alchemist.AlchemistCode.Patches;

// A fermentation counter in the top right corner of every Ferment card, mirroring the energy
// cost in the top left. "Overlay" is the base game's word for what sits on top of a card (the
// affliction overlays live in NCard's OverlayContainer). The counter is a duplicate of the card scene's
// own EnergyIcon subtree, so it inherits the size, the font, the outline and the auto-size label for
// free; only the offsets are mirrored. It shows the turns fermented, and only in combat
[HarmonyPatch(typeof(NCard))]
public static class FermentOverlayPatches
{
    private sealed class Overlay
    {
        public required TextureRect Icon;
        public required MegaLabel Label;
    }

    private static readonly ConditionalWeakTable<NCard, Overlay> Overlays = new();

    // The base game's quest energy diamond (ui_atlas card/energy_quest, 74x74), per-pixel HSV shifted
    // to the Ferment keyword's green: hue +95 degrees, saturation x0.70, value x1.5. Kept unflipped
    // so the lighting matches the energy orb beside it
    private static Texture2D? _iconTexture;

    private static Texture2D IconTexture => _iconTexture ??=
        ResourceLoader.Load<Texture2D>("ui/cards/ferment_overlay.png".ImagePath(), null, ResourceLoader.CacheMode.Reuse);

    // The EnergyIcon sits at offsets -166..-102 from the card's centre; the mirror is 102..166
    private const float MirrorLeft = 102f;
    private const float MirrorRight = 166f;

    // The energy label runs 32 down to 22. The diamond narrows toward its points, so one digit
    // sits one step smaller; two digits shrink further on their own
    private const int MaxFontSize = 28;

    // A dark tint of the diamond's own green (its edge measures about 3B6536)
    private static readonly Color OutlineColor = new("24421F");

    // Built on first use rather than in _Ready. With a _Ready postfix the hand cards got the overlay
    // but the card library's cards did not: the game prewarms a pool of 30 card nodes, and the
    // pooled ones do not pass through the postfix. UpdateVisuals runs on every card in every view
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

        // The unplayable slash has no meaning here
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

    // UpdateVisuals is the one refresh every view goes through, including the end of turn tick
    [HarmonyPatch(nameof(NCard.UpdateVisuals))] [HarmonyPostfix]
    private static void Refresh(NCard __instance)
    {
        if (GetOrAddOverlay(__instance) is not { } overlay) return;

        // Combat only: outside it there is no count to show, and a bare icon says nothing the
        // keyword line does not (the compendium, rewards and deck view have no combat state)
        if (__instance.Model is not AlchemistCard { IsFermentInline: true, IsMutable: true, CombatState: not null } card
            || __instance.Visibility != ModelVisibility.Visible)
        {
            overlay.Icon.Visible = false;
            return;
        }

        overlay.Icon.Visible = true;
        overlay.Label.SetTextAutoSize(card.FermentTurns.ToString());
    }
}
