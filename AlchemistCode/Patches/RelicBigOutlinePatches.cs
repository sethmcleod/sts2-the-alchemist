using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Rewards;

namespace Alchemist.AlchemistCode.Patches;

// The base game's big relic art has a black band at half alpha baked around the silhouette. Our
// big art ships clean, with the silhouette as a separate big/<name>_outline.png, so the band is
// composed here at the three places the game shows a big relic icon. The composite is keyed by
// the art path, so the Yummy Cookie override from BaseLib picks it up like any Alchemist relic.
// These are patched at the display methods rather than the BigIcon getter, which is small enough
// for the JIT to inline past a Harmony detour
public static class RelicBigOutlinePatches
{
    private const float BandAlpha = 0.5f;
    private const string OutlineSuffix = "_outline";
    private static readonly Dictionary<string, Texture2D> Cache = new();
    private static readonly System.Reflection.MethodInfo BigIconPathGetter =
        AccessTools.PropertyGetter(typeof(RelicModel), "BigIconPath");

    public static Texture2D? Composite(RelicModel relic)
    {
        if (BigIconPathGetter.Invoke(relic, null) is not string art) return null;
        if (Cache.TryGetValue(art, out var cached)) return cached;
        if (ComposeImage(art) is not { } image) return null;
        var texture = ImageTexture.CreateFromImage(image);
        Cache[art] = texture;
        return texture;
    }

    // The art with the half-alpha band under it, or null when the art is not ours or has no outline
    public static Image? ComposeImage(string art)
    {
        if (!art.StartsWith(MainFile.ResPath) || !art.EndsWith(".png")) return null;
        var outlinePath = art[..^4] + OutlineSuffix + ".png";
        if (!ResourceLoader.Exists(outlinePath)) return null;
        var artImage = ResourceLoader.Load<Texture2D>(art, null, ResourceLoader.CacheMode.Reuse)?.GetImage();
        var outlineImage = ResourceLoader.Load<Texture2D>(outlinePath, null, ResourceLoader.CacheMode.Reuse)?.GetImage();
        if (artImage == null || outlineImage == null) return null;
        if (artImage.IsCompressed()) artImage.Decompress();
        if (outlineImage.IsCompressed()) outlineImage.Decompress();
        artImage.Convert(Image.Format.Rgba8);
        outlineImage.Convert(Image.Format.Rgba8);
        var band = Image.CreateEmpty(outlineImage.GetWidth(), outlineImage.GetHeight(), false, Image.Format.Rgba8);
        for (var y = 0; y < band.GetHeight(); y++)
            for (var x = 0; x < band.GetWidth(); x++)
                band.SetPixel(x, y, new Color(0, 0, 0, outlineImage.GetPixel(x, y).A * BandAlpha));
        band.BlendRect(artImage, new Rect2I(0, 0, artImage.GetWidth(), artImage.GetHeight()), Vector2I.Zero);
        return band;
    }

    [HarmonyPatch(typeof(NRelic), "Reload")]
    public static class RelicNode
    {
        public static void Postfix(NRelic __instance, NRelic.IconSize ____iconSize, RelicModel? ____model)
        {
            if (____iconSize != NRelic.IconSize.Large || ____model == null || !__instance.IsNodeReady()) return;
            if (Composite(____model) is { } texture) __instance.Icon.Texture = texture;
        }
    }

    [HarmonyPatch(typeof(NInspectRelicScreen), "UpdateRelicDisplay")]
    public static class InspectScreen
    {
        public static void Postfix(IReadOnlyList<RelicModel> ____relics, int ____index, TextureRect ____relicImage)
        {
            var relic = ____relics[____index];
            // The locked placeholder is not the relic's own art, and stays
            if (____relicImage.Texture != relic.BigIcon) return;
            if (Composite(relic) is { } texture) ____relicImage.Texture = texture;
        }
    }

    [HarmonyPatch(typeof(RelicReward), nameof(RelicReward.CreateIcon))]
    public static class RewardIcon
    {
        public static void Postfix(RelicReward __instance, TextureRect __result)
        {
            if (__instance.Relic is { } relic && Composite(relic) is { } texture) __result.Texture = texture;
        }
    }
}
