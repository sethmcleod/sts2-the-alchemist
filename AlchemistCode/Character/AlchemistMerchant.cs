using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Alchemist.AlchemistCode.Character;

/// <summary>
/// Puts the Spine model into the shop scene of the Alchemist.
/// </summary>
/// <remarks>
/// The shop needs no rig of its own. Every base game character pairs a shop atlas with its own
/// combat skeleton and plays relaxed_loop from it, thus this reuses the combat skeleton whole.
///
/// NMerchantCharacter._Ready wraps GetChild(0) in a MegaSprite, which throws unless that child is
/// a SpineSprite, and then plays relaxed_loop looped from a random point. The sprite thus has to
/// be the first child, and the game drives it from there.
/// </remarks>
internal static class AlchemistMerchant
{
    public const string ScenePath = $"{MainFile.ResPath}/scenes/merchant/alchemist_merchant.tscn";

    // The shop stands the character close to the camera, far larger than the 296 px of combat.
    // The defect is the largest of the base characters at 538 px (2339 units at 0.23), and the
    // Alchemist reads a little smaller than that. The scale comes from the skeleton at run time
    private const float ModelHeight = 515f;

    // Every base merchant scene leaves its sprite at the origin and lets the rig place itself, but
    // the rigs disagree about where that origin sits: the ironclad keeps 73 units of art below it
    // and the defect 287, while the Alchemist keeps 7. Anchoring the feet instead of the origin is
    // what puts the model on the rug with the others
    private static readonly Vector2 ArtBottomCenter = new(-30f, 45f);

    // The box of the rig at the time of writing, used only if the skeleton reports none
    private static readonly Rect2 FallbackBounds = new(-312f, -7f, 594f, 822f);

    public static void Attach(Node node)
    {
        if (node is not NMerchantCharacter merchant) return;

        var data = AlchemistVisuals.SkeletonData();
        if (data == null) return;

        var box = SpineModel.Bounds(data, FallbackBounds);
        var scale = ModelHeight / box.Size.Y;
        if (SpineModel.CreateSprite(data, scale) is not { } sprite) return;

        // Spine y grows upward and Godot y downward, thus the bottom edge of the box flips sign
        var bottom = -box.Position.Y * scale;
        var middleX = (box.Position.X + box.Size.X / 2f) * scale;
        sprite.Position = ArtBottomCenter - new Vector2(middleX, bottom);

        sprite.Name = "SpineSprite";
        merchant.AddChild(sprite);

        // _Ready reads GetChild(0) and wraps it in a MegaSprite without checking the class first
        merchant.MoveChild(sprite, 0);
    }
}
