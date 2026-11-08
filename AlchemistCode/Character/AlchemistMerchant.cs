using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Alchemist.AlchemistCode.Character;

internal static class AlchemistMerchant
{
    public const string ScenePath = $"{MainFile.ResPath}/scenes/merchant/alchemist_merchant.tscn";

    private const float ModelHeight = 473f;

    private static readonly Vector2 ArtBottomCenter = new(-30f, 45f);

    private static readonly Rect2 FallbackBounds = new(-312f, -7f, 594f, 822f);

    public static void Attach(Node node)
    {
        if (node is not NMerchantCharacter merchant) return;

        var data = AlchemistVisuals.SkeletonData();
        if (data == null) return;

        BigHead.Apply(data, AlchemistVisuals.HeadBones);

        var box = SpineModel.Bounds(data, FallbackBounds);
        var scale = ModelHeight / box.Size.Y;
        if (SpineModel.CreateSprite(data, scale) is not { } sprite) return;
        Rainbow.Apply(sprite, data, AlchemistVisuals.RainbowRig);

        // Spine y grows up, Godot y grows down
        var bottom = -box.Position.Y * scale;
        var middleX = (box.Position.X + box.Size.X / 2f) * scale;
        sprite.Position = ArtBottomCenter - new Vector2(middleX, bottom);

        sprite.Name = "SpineSprite";
        merchant.AddChild(sprite);

        // NMerchantCharacter._Ready wraps GetChild(0) in a MegaSprite and throws unless it's a SpineSprite
        merchant.MoveChild(sprite, 0);
    }
}
