using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Alchemist.AlchemistCode.Character;

/// <summary>
/// Builds the Alchemist combat model from the raw Spine files at run time.
/// </summary>
/// <remarks>
/// MegaDot does not contain the Spine GDExtension, thus the Godot editor cannot import an .atlas or
/// a .skel file and cannot open a scene that holds a SpineSprite node. The game does load that
/// extension. The mod carries the three raw Spine files in the pck (see the include_filter in
/// export_presets.cfg) and makes the resource chain through ClassDB at run time. spine-godot gives
/// load_from_atlas_file and load_from_file for this use.
///
/// A scene is the other option: a hand-written text .tscn can declare a SpineSprite root and let
/// the game resolve the types when it loads the scene. That needs
/// export/convert_text_resources_to_binary = false in project.godot, because the editor cannot
/// convert a scene whose classes it does not know. Downfall uses that method. This mod keeps the
/// export default, because that flag applies to every scene in the project.
/// </remarks>
internal static class AlchemistVisuals
{
    // The GDExtension classes have no C# binding, thus each one is used through ClassDB and Call
    private const string SpriteClass = "SpineSprite";
    private const string AtlasClass = "SpineAtlasResource";
    private const string SkeletonFileClass = "SpineSkeletonFileResource";
    private const string SkeletonDataClass = "SpineSkeletonDataResource";

    private const string ModelDir = $"{MainFile.ResPath}/animations/characters/alchemist";
    private const string AtlasPath = $"{ModelDir}/alchemist_model.atlas";
    private const string SkeletonPath = $"{ModelDir}/alchemist_model.skel";

    /// <summary>
    /// The atlas page. The character puts it in ExtraAssetPaths, thus the game caches the texture
    /// with the other character assets and the first combat does not read it from the pck.
    /// </summary>
    public const string TexturePath = $"{ModelDir}/alchemist_model.png";

    /// <summary>The only animation in the skeleton.</summary>
    public const string IdleAnimation = "idle_loop";

    // The art is painted at 1:1 with the skeleton units, thus the model stays unscaled and sharp.
    // The skeleton measures 228 x 307 units, which is near the height of the ironclad on screen
    // (1185 units at 0.28 scale)
    private const float ModelScale = 1f;

    // The feet of the model sit at y = 0 and Godot y increases downward, thus the art occupies
    // y -296 to 0. The skeleton is 8 units wider on the left, where the staff is
    private static readonly Vector2 BoundsPosition = new(-118, -296);
    private static readonly Vector2 BoundsSize = new(228, 296);
    private static readonly Vector2 CenterPosition = new(0, -170);
    private static readonly Vector2 IntentPosition = new(0, -300);

    // One skeleton serves all the sprites. The game makes the visuals again for the game over
    // screen and the unlock screen, and a re-read of the files for each one is waste
    private static Resource? _skeletonData;

    /// <summary>
    /// Returns the model, or null if the Spine files do not load. Null makes BaseLib use
    /// CustomVisualPath, which the character points at the base game fallback scene.
    /// </summary>
    public static NCreatureVisuals? Create()
    {
        var sprite = CreateSprite();
        if (sprite == null) return null;

        var visuals = new NCreatureVisuals();
        visuals.AddUnique(sprite, "Visuals");

        var bounds = new Control
        {
            Position = BoundsPosition,
            Size = BoundsSize,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        visuals.AddUnique(bounds, "Bounds");

        visuals.AddUnique(new Marker2D { Position = CenterPosition }, "CenterPos");
        visuals.AddUnique(new Marker2D { Position = IntentPosition }, "IntentPos");

        // AddFormVfx throws if this holder is absent, and a Form power adds its vfx to the player
        visuals.AddUnique(new Control(), "FormVfx");

        return visuals;
    }

    private static Node2D? CreateSprite()
    {
        var data = SkeletonData();
        if (data == null) return null;

        if (ClassDB.Instantiate(SpriteClass).As<Node2D>() is not { } sprite)
        {
            MainFile.Logger.Error($"Could not make a {SpriteClass}. The Alchemist uses the fallback model.");
            return null;
        }

        sprite.Set("skeleton_data_res", data);
        sprite.Scale = new Vector2(ModelScale, ModelScale);
        return sprite;
    }

    private static Resource? SkeletonData()
    {
        if (_skeletonData != null) return _skeletonData;

        if (!ClassDB.ClassExists(SpriteClass))
        {
            MainFile.Logger.Error(
                "The Spine GDExtension is not loaded. The Alchemist uses the fallback model.");
            return null;
        }

        var atlas = ClassDB.Instantiate(AtlasClass).As<Resource>();
        var skeletonFile = ClassDB.Instantiate(SkeletonFileClass).As<Resource>();
        var data = ClassDB.Instantiate(SkeletonDataClass).As<Resource>();
        if (atlas == null || skeletonFile == null || data == null)
        {
            MainFile.Logger.Error("Could not make the Spine resources. The Alchemist uses the fallback model.");
            return null;
        }

        if (ReadFailed(atlas, "load_from_atlas_file", AtlasPath)) return null;
        if (ReadFailed(skeletonFile, "load_from_file", SkeletonPath)) return null;

        // The data resource reads the skeleton when it holds both halves, thus the atlas goes first
        data.Set("atlas_res", atlas);
        data.Set("skeleton_file_res", skeletonFile);

        if (!data.Call("is_skeleton_data_loaded").AsBool())
        {
            MainFile.Logger.Error(
                "The Alchemist skeleton did not load from its atlas and skel. It uses the fallback model.");
            return null;
        }

        _skeletonData = data;
        return data;
    }

    private static bool ReadFailed(Resource resource, string method, string path)
    {
        var error = (Error)resource.Call(method, path).AsInt64();
        if (error == Error.Ok) return false;

        MainFile.Logger.Error($"Could not read {path} ({error}). The Alchemist uses the fallback model.");
        return true;
    }
}
