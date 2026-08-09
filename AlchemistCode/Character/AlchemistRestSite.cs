using Godot;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace Alchemist.AlchemistCode.Character;

/// <summary>
/// Puts the Spine model into the rest site scene of the Alchemist.
/// </summary>
/// <remarks>
/// A mod scene cannot hold a SpineSprite (see <see cref="SpineModel"/>), thus the scene at
/// CustomRestSiteAnimPath holds only the Control nodes and this class adds the sprite after
/// BaseLib turns that scene into an NRestSiteCharacter. RestSiteModelPatch registers it.
///
/// The game drives the model by itself from there. NRestSiteCharacter._Ready walks its direct
/// children for nodes of class SpineSprite, plays the loop of the current act on each one, and
/// starts it at a random point in that loop so two characters never sway together. The same walk
/// flips the sprite to seat the character on the other side of the fire.
/// </remarks>
internal static class AlchemistRestSite
{
    private const string ModelDir = $"{MainFile.ResPath}/animations/rest_site/alchemist";
    private const string AtlasPath = $"{ModelDir}/alchemist_rest_site.atlas";
    private const string SkeletonPath = $"{ModelDir}/alchemist_rest_site.skel";

    /// <summary>The atlas page, listed in ExtraAssetPaths so it caches with the character.</summary>
    public const string TexturePath = $"{ModelDir}/alchemist_rest_site.png";

    public const string ScenePath = $"{MainFile.ResPath}/scenes/rest_site/alchemist_rest_site.tscn";

    // The game picks the loop from the act index alone, thus both halves of act 1 get the green
    // overgrowth light. The Underdocks is lit blue, and the act 3 loop already carries that light
    private const string UnderdocksAnimationLeaf = "glory_loop";

    /// <summary>The loop to play in the Underdocks, or null if the skeleton holds none.</summary>
    public static string? UnderdocksAnimation { get; private set; }

    // The box the still image filled, which sat correctly at the fire. The scale and the offset
    // below both come from the skeleton at run time, thus a rig whose size or origin moves between
    // exports still lands here
    private const float ModelHeight = 519f;
    private static readonly Vector2 ArtCenter = new(0f, -7.5f);

    // The box of the rig at the time of writing, used only if the skeleton reports none
    private static readonly Rect2 FallbackBounds = new(-234f, -224f, 442f, 583f);

    /// <summary>
    /// Takes a Node rather than an NRestSiteCharacter on purpose. BaseLib stores the action as
    /// Action&lt;Node&gt; through an "as" cast, and Action is contravariant, thus an
    /// Action&lt;NRestSiteCharacter&gt; casts to null there and never runs.
    /// </summary>
    public static void Attach(Node node)
    {
        if (node is not NRestSiteCharacter character) return;

        var data = SpineModel.Load(AtlasPath, SkeletonPath);
        if (data == null) return;

        UnderdocksAnimation = SpineModel.ResolveAnimation(data, UnderdocksAnimationLeaf);

        // The two rigs hang their art very differently around the origin: the ironclad reaches from
        // -137 to 265 across and the Alchemist from -234 to 208, thus the sprite needs its own
        // offset as well as a scale, or the model sits left of and above the seat
        var box = SpineModel.Bounds(data, FallbackBounds);
        var scale = ModelHeight / box.Size.Y;

        if (SpineModel.CreateSprite(data, scale) is not { } sprite) return;

        // Godot y grows downward and Spine y grows upward, thus the middle of the box flips sign
        var middle = new Vector2(
            (box.Position.X + box.Size.X / 2f) * scale,
            -(box.Position.Y + box.Size.Y / 2f) * scale);
        sprite.Position = ArtCenter - middle;

        sprite.Name = "SpineSprite";
        character.AddChild(sprite);

        // The base game rest site scenes put the sprite before the Control nodes
        character.MoveChild(sprite, 0);
    }
}
