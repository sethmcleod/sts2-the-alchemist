using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace Alchemist.AlchemistCode.Character;

internal static class AlchemistRestSite
{
    private const string ModelDir = $"{MainFile.ResPath}/animations/rest_site/alchemist";
    private const string AtlasPath = $"{ModelDir}/alchemist_rest_site.atlas";
    private const string SkeletonPath = $"{ModelDir}/alchemist_rest_site.skel";

    public const string TexturePath = $"{ModelDir}/alchemist_rest_site.png";

    public const string ScenePath = $"{MainFile.ResPath}/scenes/rest_site/alchemist_rest_site.tscn";

    // Underdocks has no loop of its own, and Glory's carries the same blue light
    private static readonly Dictionary<Type, string> LoopLeaves = new()
    {
        [typeof(Overgrowth)] = "overgrowth_loop",
        [typeof(Underdocks)] = "glory_loop",
        [typeof(Hive)] = "hive_loop",
        [typeof(Glory)] = "glory_loop",
    };

    private static readonly Dictionary<Type, string> Loops = [];

    public static string? LoopFor(ActModel act) => Loops.GetValueOrDefault(act.GetType());

    private const float ModelHeight = 519f;
    private static readonly Vector2 ArtCenter = new(0f, -7.5f);

    private static readonly Rect2 FallbackBounds = new(-234f, -224f, 442f, 583f);

    private static readonly string[] HeadBones = ["rest_head"];
    private const string HeadSlot = "rest_head";

    private static readonly string[] OverlaySlots = ["rest_shadow", "rest_light"];

    private static readonly Rainbow.Rig RainbowRig = new(
        Rainbow.Parts.Robe | Rainbow.Parts.Skin,
        Rainbow.Slots(
            (Rainbow.Parts.Skin | Rainbow.Parts.Eyes, ["rest_head"]),
            (Rainbow.Parts.None, ["rest_bowl"])));

    // Takes Node on purpose: BaseLib "as"-casts the action to Action<Node>, and a narrower one casts to null
    public static void Attach(Node node)
    {
        if (node is not NRestSiteCharacter character) return;

        var data = SpineModel.Load(AtlasPath, SkeletonPath);
        if (data == null) return;

        BigHead.Apply(data, HeadBones);

        foreach (var (act, leaf) in LoopLeaves)
            if (SpineModel.ResolveAnimation(data, leaf) is { } loop)
                Loops[act] = loop;

        var box = SpineModel.Bounds(data, FallbackBounds);
        var scale = ModelHeight / box.Size.Y;

        if (SpineModel.CreateSprite(data, scale) is not { } sprite) return;
        Rainbow.Apply(sprite, data, RainbowRig);

        var middle = new Vector2(
            (box.Position.X + box.Size.X / 2f) * scale,
            -(box.Position.Y + box.Size.Y / 2f) * scale);
        sprite.Position = ArtCenter - middle;

        sprite.Name = "SpineSprite";
        character.AddChild(sprite);

        character.MoveChild(sprite, 0);

        // Waiting for the skeleton needs the tree, which the sprite joins only after this returns
        if (BigHead.Enabled)
            sprite.Ready += () => sprite.RunWhenSpineReady(new MegaSprite(sprite),
                _ => BigHead.DrawUnderHead(sprite, data, HeadSlot, OverlaySlots));
    }
}
