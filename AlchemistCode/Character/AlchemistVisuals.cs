using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
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

    private const string IdleAnimationLeaf = "idle_loop";
    private const string BlinkAnimationLeaf = "blink";

    // The idle holds track 0. A second track plays the blink over it, thus the eyes keep their own
    // clock and never lock to the loop of the idle. Vantom and LagavulinMatriarch layer this way
    private const int BlinkTrack = 1;

    // Spine plays a queue, and the eyes stop when it empties. The queue must thus outlast any
    // fight: 400 blinks at a mean gap of 6 seconds cover about 40 minutes. The game makes the
    // visuals again for each combat, thus the queue starts over every time
    private const int QueuedBlinks = 400;
    private const float MinBlinkGap = 3f;
    private const float MaxBlinkGap = 9f;

    // A private generator, thus the blink times never draw from the seeded run of the game
    private static readonly Random BlinkRng = new();

    /// <summary>
    /// The idle animation, read from the skeleton. Spine puts the name of the folder that holds an
    /// animation in front of its name, thus a new folder in the project renames it (idle_loop became
    /// main/idle_loop between two exports). Matching on the last part survives that.
    /// </summary>
    public static string IdleAnimation { get; private set; } = IdleAnimationLeaf;

    /// <summary>The blink animation, or null if the skeleton holds none.</summary>
    public static string? BlinkAnimation { get; private set; }

    // How high the model stands on screen, from the feet to the top of the art. This is near the
    // height of the ironclad (1185 units at 0.28 scale). The scale comes from the skeleton at run
    // time, thus a rig that changes size between exports still draws at this height. The Spine
    // atlas scale does not enter into it: it changes only how many texels cover the same art
    private const float ModelHeight = 296f;

    // The height of the ironclad rig, used only if the skeleton does not report its own size
    private const float FallbackSkeletonHeight = 833f;

    // The feet of the model sit at y = 0 and Godot y increases downward, thus the art occupies
    // y -296 to 0. The skeleton is 8 units wider on the left, where the staff is.
    // These are in screen pixels, thus ModelScale already applies and they do not follow the rig
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
        var scale = ScaleFor(data);
        sprite.Scale = new Vector2(scale, scale);
        return sprite;
    }

    /// <summary>
    /// Returns the scale that draws the skeleton ModelHeight high.
    /// </summary>
    /// <remarks>
    /// The skeleton reports the box around its setup pose. Its y is the bottom edge, which sits
    /// below the feet because the shadow reaches past them, thus y plus the height is the part
    /// above the ground and is what must match ModelHeight.
    /// </remarks>
    private static float ScaleFor(Resource data)
    {
        var above = FallbackSkeletonHeight;
        if (data.HasMethod("get_height") && data.HasMethod("get_y"))
        {
            var reported = data.Call("get_y").AsSingle() + data.Call("get_height").AsSingle();
            if (reported > 1f) above = reported;
            else MainFile.Logger.Info($"The Alchemist skeleton reported a height of {reported}. Using the fallback.");
        }

        return ModelHeight / above;
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

        IdleAnimation = ResolveAnimation(data, IdleAnimationLeaf) ?? IdleAnimationLeaf;
        BlinkAnimation = ResolveAnimation(data, BlinkAnimationLeaf);

        if (BlinkAnimation == null)
            MainFile.Logger.Info("The Alchemist skeleton holds no blink animation. The eyes stay open.");

        _skeletonData = data;
        return data;
    }

    /// <summary>
    /// Returns the full name of the animation whose last part is <paramref name="leaf"/>, or null.
    /// </summary>
    /// <remarks>
    /// The artist names these, thus the match ignores a folder in front and a leading underscore.
    /// Both have appeared already: main/idle_loop, then _blink because Spine refuses a name that
    /// holds a slash.
    /// </remarks>
    private static string? ResolveAnimation(Resource data, string leaf)
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
    /// Queues the blinks on their own track, each one after a random wait.
    /// </summary>
    /// <remarks>
    /// The skeleton of a SpineSprite loads over several frames, thus the animation state can be
    /// absent when the game builds the animator. RunWhenSpineReady waits for it.
    /// </remarks>
    public static void StartBlinking(MegaSprite sprite)
    {
        if (BlinkAnimation == null) return;
        if (sprite.BoundObject is not Node host) return;

        host.RunWhenSpineReady(sprite, QueueBlinks);
    }

    private static void QueueBlinks(MegaAnimationState state)
    {
        if (BlinkAnimation is not { } blink) return;

        var blinkLength = 0f;
        for (var i = 0; i < QueuedBlinks; i++)
        {
            var gap = MinBlinkGap + (float)BlinkRng.NextDouble() * (MaxBlinkGap - MinBlinkGap);

            // Spine counts the delay from the start of the entry before, thus each wait must also
            // carry the length of the blink. The first entry counts from now
            var delay = i == 0 ? gap : blinkLength + gap;

            using var entry = state.AddAnimationTracked(blink, delay, loop: false, BlinkTrack);

            // The blink swaps the eye attachment, and an attachment must snap rather than fade
            entry.SetMixDuration(0f);

            if (i == 0) blinkLength = entry.GetAnimationDuration();
        }
    }

    private static bool ReadFailed(Resource resource, string method, string path)
    {
        var error = (Error)resource.Call(method, path).AsInt64();
        if (error == Error.Ok) return false;

        MainFile.Logger.Error($"Could not read {path} ({error}). The Alchemist uses the fallback model.");
        return true;
    }
}
