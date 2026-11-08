using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Rewards;

namespace Alchemist.AlchemistCode.Patches;

// Base big relic art bakes in a half-alpha black band; ours is composed from big/<name>_outline.png
// Patched at the display methods, since the JIT inlines the BigIcon getter past a detour
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
            // Leaves the locked placeholder alone
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
