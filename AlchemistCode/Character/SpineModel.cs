using Godot;

namespace Alchemist.AlchemistCode.Character;

/// <summary>
/// Reads a Spine skeleton from raw files at run time and makes SpineSprite nodes from it.
/// </summary>
/// <remarks>
/// MegaDot does not contain the Spine GDExtension, thus the Godot editor cannot import an .atlas or
/// a .skel file and cannot open a scene that holds a SpineSprite node. The game does load that
/// extension. The mod carries the raw Spine files in the pck (see the include_filter in
/// export_presets.cfg) and makes the resource chain through ClassDB here. spine-godot gives
/// load_from_atlas_file and load_from_file for this use.
///
/// A scene is the other option: a hand-written text .tscn can declare a SpineSprite root and let
/// the game resolve the types when it loads the scene. That needs
/// export/convert_text_resources_to_binary = false in project.godot, because the editor cannot
/// convert a scene whose classes it does not know. Downfall uses that method. This mod keeps the
/// export default, because that flag applies to every scene in the project.
/// </remarks>
internal static class SpineModel
{
    // The GDExtension classes have no C# binding, thus each one is used through ClassDB and Call
    public const string SpriteClass = "SpineSprite";
    private const string AtlasClass = "SpineAtlasResource";
    private const string SkeletonFileClass = "SpineSkeletonFileResource";
    private const string SkeletonDataClass = "SpineSkeletonDataResource";

    // One skeleton serves every sprite made from it. The game makes the combat visuals again for
    // the game over screen and the unlock screen, and a re-read of the files for each one is waste
    private static readonly Dictionary<string, Resource> Loaded = [];

    /// <summary>True when the game has the Spine GDExtension.</summary>
    public static bool Available => ClassDB.ClassExists(SpriteClass);

    /// <summary>
    /// Returns the skeleton for a pair of raw Spine files, or null if either one does not read.
    /// </summary>
    public static Resource? Load(string atlasPath, string skeletonPath)
    {
        if (Loaded.TryGetValue(skeletonPath, out var cached)) return cached;

        if (!Available)
        {
            MainFile.Logger.Error($"The Spine GDExtension is not loaded. {skeletonPath} stays unused.");
            return null;
        }

        var atlas = ClassDB.Instantiate(AtlasClass).As<Resource>();
        var skeletonFile = ClassDB.Instantiate(SkeletonFileClass).As<Resource>();
        var data = ClassDB.Instantiate(SkeletonDataClass).As<Resource>();
        if (atlas == null || skeletonFile == null || data == null)
        {
            MainFile.Logger.Error($"Could not make the Spine resources for {skeletonPath}.");
            return null;
        }

        if (ReadFailed(atlas, "load_from_atlas_file", atlasPath)) return null;
        if (ReadFailed(skeletonFile, "load_from_file", skeletonPath)) return null;

        // The data resource reads the skeleton when it holds both halves, thus the atlas goes first
        data.Set("atlas_res", atlas);
        data.Set("skeleton_file_res", skeletonFile);

        if (!data.Call("is_skeleton_data_loaded").AsBool())
        {
            MainFile.Logger.Error($"The skeleton in {skeletonPath} did not load from its atlas and skel.");
            return null;
        }

        Loaded[skeletonPath] = data;
        return data;
    }

    /// <summary>Returns a SpineSprite that draws the skeleton at the given scale.</summary>
    public static Node2D? CreateSprite(Resource data, float scale)
    {
        if (ClassDB.Instantiate(SpriteClass).As<Node2D>() is not { } sprite)
        {
            MainFile.Logger.Error($"Could not make a {SpriteClass}.");
            return null;
        }

        sprite.Set("skeleton_data_res", data);
        sprite.Scale = new Vector2(scale, scale);
        return sprite;
    }

    /// <summary>
    /// The height of the box around the setup pose, or <paramref name="fallback"/> if the skeleton
    /// does not report one. A rig that changes size between exports thus needs no code change.
    /// </summary>
    /// <summary>
    /// The box around the setup pose, in skeleton units, or <paramref name="fallback"/> if the
    /// skeleton reports none. Its y is the bottom edge and Spine y grows upward, the opposite of
    /// Godot. A rig whose origin moves between exports thus needs no code change.
    /// </summary>
    public static Rect2 Bounds(Resource data, Rect2 fallback)
    {
        foreach (var method in (string[])["get_x", "get_y", "get_width", "get_height"])
            if (!data.HasMethod(method)) return fallback;

        var box = new Rect2(
            data.Call("get_x").AsSingle(), data.Call("get_y").AsSingle(),
            data.Call("get_width").AsSingle(), data.Call("get_height").AsSingle());

        if (box.Size.Y > 1f) return box;

        MainFile.Logger.Info($"A skeleton reported a box of {box}. Using the fallback of {fallback}.");
        return fallback;
    }

    public static float Height(Resource data, float fallback)
    {
        if (!data.HasMethod("get_height")) return fallback;

        return Sane(data.Call("get_height").AsSingle(), fallback);
    }

    /// <summary>
    /// The part of that box above the origin. The y of the box is its bottom edge, which can sit
    /// below the origin, for example where a shadow reaches past the feet.
    /// </summary>
    public static float AboveOrigin(Resource data, float fallback)
    {
        if (!data.HasMethod("get_height") || !data.HasMethod("get_y")) return fallback;

        return Sane(data.Call("get_height").AsSingle() + data.Call("get_y").AsSingle(), fallback);
    }

    private static float Sane(float value, float fallback)
    {
        if (value > 1f) return value;

        MainFile.Logger.Info($"A skeleton reported a size of {value}. Using the fallback of {fallback}.");
        return fallback;
    }

    /// <summary>
    /// Returns the full name of the animation whose last part is <paramref name="leaf"/>, or null.
    /// </summary>
    /// <remarks>
    /// The artist names these, thus the match ignores a folder in front and a leading underscore.
    /// Both have appeared already: main/idle_loop, then _blink because Spine refuses a name that
    /// holds a slash.
    /// </remarks>
    public static string? ResolveAnimation(Resource data, string leaf)
    {
        if (!data.HasMethod("get_animations")) return null;

        foreach (var entry in data.Call("get_animations").AsGodotArray())
        {
            if (entry.AsGodotObject() is not { } animation) continue;

            var name = animation.Call("get_name").AsString();
            var tail = name[(name.LastIndexOf('/') + 1)..].TrimStart('_');
            if (string.Equals(tail, leaf, StringComparison.OrdinalIgnoreCase)) return name;
        }

        return null;
    }

    private static bool ReadFailed(Resource resource, string method, string path)
    {
        var error = (Error)resource.Call(method, path).AsInt64();
        if (error == Error.Ok) return false;

        MainFile.Logger.Error($"Could not read {path} ({error}).");
        return true;
    }
}
