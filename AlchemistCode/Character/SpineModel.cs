using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using FileAccess = Godot.FileAccess;

namespace Alchemist.AlchemistCode.Character;

// The editor lacks the Spine GDExtension, so models are built from the raw files at runtime, never in a scene
internal static class SpineModel
{
    public const string SpriteClass = "SpineSprite";
    private const string AtlasClass = "SpineAtlasResource";
    private const string SkeletonFileClass = "SpineSkeletonFileResource";
    private const string SkeletonDataClass = "SpineSkeletonDataResource";

    private const string EmptyAnimationMethod = "set_empty_animation";

    private static readonly Dictionary<string, Resource> Loaded = [];

    public static bool Available => ClassDB.ClassExists(SpriteClass);

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

        // Atlas first: the data resource reads the skeleton as soon as it holds both
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

    // set_empty_animation has no binding, and the bound add_empty_animation waits behind the whole queue
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

    // Exports have added a folder prefix and a leading underscore, so match only the last part
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
