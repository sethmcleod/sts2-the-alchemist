using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using FileAccess = Godot.FileAccess;

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

    private const string EmptyAnimationMethod = "set_empty_animation";

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
    /// Drops everything on a track and mixes the track back out over <paramref name="mix"/> seconds,
    /// thus the bones it held return to whatever the tracks under it pose.
    /// </summary>
    /// <remarks>
    /// MegaAnimationState binds add_empty_animation, which goes on the end of the queue of a track
    /// and thus waits for every entry in front of it. Nothing it binds ends a track early.
    /// set_empty_animation is the Spine call that replaces the entry playing now, throws the rest of
    /// the queue away, and fades the track out. The binding cannot reach it, thus the call goes
    /// straight to the Spine object. The Variant it returns holds a fresh wrapper, which the using
    /// releases on this thread (see the note on MegaSpineBinding).
    /// </remarks>
    public static void FadeOutTrack(MegaAnimationState state, int track, float mix)
    {
        if (state.BoundObject is not { } native || !native.HasMethod(EmptyAnimationMethod))
        {
            MainFile.Logger.Info(
                $"The Spine animation state has no {EmptyAnimationMethod}. Track {track} keeps playing.");
            return;
        }

        using var _ = native.Call(EmptyAnimationMethod, track, mix);
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

    /// <summary>
    /// Where the image of an atlas region sits on its page, in UV, or null if the atlas holds no such
    /// region or stores it turned. The rectangle spans the whole image, the whitespace the packer
    /// stripped included, thus a point given as a fraction of the image maps straight onto the page.
    /// </summary>
    /// <remarks>
    /// This reads the atlas text: a name after a blank line starts a page, any other name a region,
    /// and the fields under a name are its own. A new export can pack a region elsewhere on the
    /// page, and this follows it.
    /// </remarks>
    public static Rect2? RegionUv(string atlasPath, string region)
    {
        var pageSize = Vector2.Zero;
        float[]? bounds = null, offsets = null;
        var turned = false;

        string? current = null;
        var pageNext = true;
        foreach (var raw in FileAccess.GetFileAsString(atlasPath).Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                pageNext = true;
                continue;
            }

            var colon = line.IndexOf(':');
            if (colon < 0)
            {
                if (current == region) break;
                current = pageNext ? null : line;
                pageNext = false;
                continue;
            }

            var field = line[..colon].Trim();
            var value = line[(colon + 1)..];
            if (current == null && field == "size" && Numbers(value) is [var width, var height])
                pageSize = new Vector2(width, height);
            else if (current == region && field == "bounds") bounds = Numbers(value);
            else if (current == region && field == "offsets") offsets = Numbers(value);
            else if (current == region && field == "rotate") turned = value.Trim() is not ("false" or "0");
        }

        if (bounds is not [var x, var y, var w, var h] || pageSize.X <= 0f || pageSize.Y <= 0f || turned)
            return null;

        // Spine counts the stripped margin from the bottom of the image, and the page from its top
        var (left, bottom, fullWidth, fullHeight) =
            offsets is [var ox, var oy, var ow, var oh] ? (ox, oy, ow, oh) : (0f, 0f, w, h);
        var top = fullHeight - h - bottom;
        return new Rect2(
            (x - left) / pageSize.X, (y - top) / pageSize.Y,
            fullWidth / pageSize.X, fullHeight / pageSize.Y);

        static float[]? Numbers(string text)
        {
            var parts = text.Split(',');
            var numbers = new float[parts.Length];
            for (var i = 0; i < parts.Length; i++)
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i]))
                    return null;
            return numbers;
        }
    }

    private static bool ReadFailed(Resource resource, string method, string path)
    {
        var error = (Error)resource.Call(method, path).AsInt64();
        if (error == Error.Ok) return false;

        MainFile.Logger.Error($"Could not read {path} ({error}).");
        return true;
    }
}
