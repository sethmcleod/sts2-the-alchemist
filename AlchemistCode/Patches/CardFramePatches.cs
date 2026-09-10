using Alchemist.AlchemistCode.Character;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Alchemist.AlchemistCode.Patches;

// A card draws four tinted pieces: the frame, tinted by the pool's hue material, and the portrait border,
// the title banner and the type plaque, tinted by the rarity's banner material. The painted Alchemist set
// is at its final colours, so each piece gets its texture and no material. The frame texture arrives
// through BaseLib (AlchemistCardPool.CustomFrame). This patch sets the three pieces BaseLib has no hook
// for and clears the material on all four. It runs after the two methods that assign the base pieces:
// Reload, on a model or visibility change, and UpdateVisuals, on every refresh in a pile. Both are large
// methods, so the JIT does not inline them past the patch
[HarmonyPatch(typeof(NCard))]
public static class CardFramePatches
{
    // The scene's own plaque texture, so a node that moves from an Alchemist card to another card gets it back
    private static Texture2D? _basePlaque;

    [HarmonyPostfix, HarmonyPatch("Reload")]
    public static void AfterReload(NCard __instance) => Apply(__instance);

    [HarmonyPostfix, HarmonyPatch(nameof(NCard.UpdateVisuals))]
    public static void AfterUpdateVisuals(NCard __instance) => Apply(__instance);

    private static void Apply(NCard card)
    {
        if (!card.IsNodeReady() || card.Model == null) return;

        var plaque = card.GetNode<NinePatchRect>("%TypePlaque");
        _basePlaque ??= plaque.Texture;

        var set = card.Model.VisualCardPool is AlchemistCardPool ? AlchemistCardFrames.For(card.Model) : null;
        if (set == null)
        {
            // The base methods reassign the other pieces themselves. The plaque texture is set by the scene only
            if (plaque.Texture != _basePlaque) plaque.Texture = _basePlaque;
            return;
        }

        card.GetNode<TextureRect>("%Frame").Material = null;

        var border = card.GetNode<TextureRect>("%PortraitBorder");
        border.Texture = set.Border;
        border.Material = null;

        var banner = card.GetNode<TextureRect>("%TitleBanner");
        banner.Texture = set.Banner;
        banner.Material = null;

        plaque.Texture = set.Plaque;
        plaque.Material = null;
    }
}
