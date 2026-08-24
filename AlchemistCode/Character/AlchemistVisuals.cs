using Alchemist.AlchemistCode.Compat;
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
/// SpineModel carries the reason the model is built here and not in a scene.
/// </remarks>
internal static class AlchemistVisuals
{
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
    private const string HurtAnimationLeaf = "hurt";
    private const string AttackAnimationLeaf = "attack";
    private const string HeavyAttackAnimationLeaf = "attack_heavy";
    private const string CastAnimationLeaf = "cast";
    private const string DeathAnimationLeaf = "die";
    private const string RelaxedAnimationLeaf = "relaxed_loop";
    // Our rig calls it near_death_loop and the base game calls it low_health_loop, thus both
    private const string NearDeathAnimationLeaf = "near_death_loop";
    private const string LowHealthAnimationLeaf = "low_health_loop";
    private const string ShineAnimationLeaf = "shine";

    // The idle holds track 0. A second track plays the blink over it, thus the eyes keep their own
    // clock and never lock to the loop of the idle. Vantom and LagavulinMatriarch layer this way
    private const int BlinkTrack = 1;

    // The gem on the staff shines on a third track, thus the eyes, the staff and the body each
    // keep a clock of their own
    private const int ShineTrack = 2;

    // Spine plays a queue, and a track stops when its queue empties. Each queue must thus outlast
    // any fight: 400 entries at a mean gap of 6 seconds cover about 40 minutes. The game makes the
    // visuals again for each combat, thus both queues start over every time
    private const int QueuedLoops = 400;
    private const float MinBlinkGap = 3f;
    private const float MaxBlinkGap = 9f;
    private const float MinShineGap = 7f;
    private const float MaxShineGap = 9f;

    // How long the two tracks take to fade out at death. A blink caught half closed thus opens
    // rather than snapping
    private const float FlourishFadeOut = 0.15f;

    // A private generator, thus these times never draw from the seeded run of the game
    private static readonly Random FlourishRng = new();

    /// <summary>
    /// The idle animation, read from the skeleton. Spine puts the name of the folder that holds an
    /// animation in front of its name, thus a new folder in the project renames it (idle_loop became
    /// main/idle_loop between two exports). Matching on the last part survives that.
    /// </summary>
    public static string IdleAnimation { get; private set; } = IdleAnimationLeaf;

    /// <summary>The blink animation, or null if the skeleton holds none.</summary>
    public static string? BlinkAnimation { get; private set; }

    /// <summary>The on hit animation, or null. Null makes the game play the idle instead.</summary>
    public static string? HurtAnimation { get; private set; }

    /// <summary>The light attack, or null.</summary>
    public static string? AttackAnimation { get; private set; }

    /// <summary>The heavy attack, or null. Null makes a heavy hit fall back to the light one.</summary>
    public static string? HeavyAttackAnimation { get; private set; }

    /// <summary>Played for Skills and, following the base characters, for Powers too.</summary>
    public static string? CastAnimation { get; private set; }

    /// <summary>The death animation, or null while the skeleton still lacks one.</summary>
    public static string? DeathAnimation { get; private set; }

    /// <summary>
    /// The out of combat idle. Every base character has one and registers it, but nothing in the game
    /// fires the "Relaxed" trigger, so it stays dormant until MegaCrit starts using it.
    /// </summary>
    public static string? RelaxedAnimation { get; private set; }

    /// <summary>The low HP idle, or null if the skeleton holds none.</summary>
    public static string? NearDeathAnimation { get; private set; }

    /// <summary>The shine on the staff gem, or null if the skeleton holds none.</summary>
    public static string? ShineAnimation { get; private set; }


    // How high the model stands on screen, from the feet to the top of the art. This is near the
    // height of the ironclad (1185 units at 0.28 scale). The scale comes from the skeleton at run
    // time, thus a rig that changes size between exports still draws at this height. The Spine
    // atlas scale does not enter into it: it changes only how many texels cover the same art
    private const float ModelHeight = 272f;

    // The anchors below were measured against a 296-unit model. They are screen pixels, so they do
    // not follow the rig: scale them with the height or the hover box and intent bubble drift
    private const float AnchorScale = ModelHeight / 296f;

    // The height of the ironclad rig, used only if the skeleton does not report its own size
    private const float FallbackSkeletonHeight = 833f;

    // The feet of the model sit at y = 0 and Godot y increases downward, thus the art occupies
    // y -296 to 0. The skeleton is 8 units wider on the left, where the staff is.
    // These are in screen pixels, thus ModelScale already applies and they do not follow the rig
    private static readonly Vector2 BoundsPosition = new(-118 * AnchorScale, -296 * AnchorScale);
    private static readonly Vector2 BoundsSize = new(228 * AnchorScale, 296 * AnchorScale);
    private static readonly Vector2 CenterPosition = new(0, -170 * AnchorScale);
    private static readonly Vector2 IntentPosition = new(0, -300 * AnchorScale);

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

        // A rig that changes size between exports still draws ModelHeight high
        var scale = ModelHeight / SpineModel.AboveOrigin(data, FallbackSkeletonHeight);
        return SpineModel.CreateSprite(data, scale);
    }

    public static Resource? SkeletonData()
    {
        if (_skeletonData != null) return _skeletonData;

        var data = SpineModel.Load(AtlasPath, SkeletonPath);
        if (data == null)
        {
            MainFile.Logger.Error("The Alchemist combat model did not load. It uses the fallback model.");
            return null;
        }

        IdleAnimation = SpineModel.ResolveAnimation(data, IdleAnimationLeaf) ?? IdleAnimationLeaf;
        BlinkAnimation = SpineModel.ResolveAnimation(data, BlinkAnimationLeaf);
        HurtAnimation = SpineModel.ResolveAnimation(data, HurtAnimationLeaf);
        AttackAnimation = SpineModel.ResolveAnimation(data, AttackAnimationLeaf);
        HeavyAttackAnimation = SpineModel.ResolveAnimation(data, HeavyAttackAnimationLeaf);
        CastAnimation = SpineModel.ResolveAnimation(data, CastAnimationLeaf);
        DeathAnimation = SpineModel.ResolveAnimation(data, DeathAnimationLeaf);
        RelaxedAnimation = SpineModel.ResolveAnimation(data, RelaxedAnimationLeaf);
        NearDeathAnimation = SpineModel.ResolveAnimation(data, NearDeathAnimationLeaf)
            ?? SpineModel.ResolveAnimation(data, LowHealthAnimationLeaf);
        ShineAnimation = SpineModel.ResolveAnimation(data, ShineAnimationLeaf);

        if (BlinkAnimation == null)
            MainFile.Logger.Info("The Alchemist skeleton holds no blink animation. The eyes stay open.");

        _skeletonData = data;
        return data;
    }

    /// <summary>
    /// Queues the blink and the shine on their own tracks, each entry after a random wait.
    /// </summary>
    /// <remarks>
    /// The skeleton of a SpineSprite loads over several frames, thus the animation state can be
    /// absent when the game builds the animator. RunWhenSpineReady waits for it.
    /// </remarks>
    public static void StartIdleFlourishes(MegaSprite sprite)
    {
        if (BlinkAnimation == null && ShineAnimation == null) return;
        if (sprite.BoundObject is not Node host) return;

        host.RunWhenSpineReady(sprite, state =>
        {
            QueueFlourish(state, BlinkAnimation, BlinkTrack, MinBlinkGap, MaxBlinkGap);
            QueueFlourish(state, ShineAnimation, ShineTrack, MinShineGap, MaxShineGap);
            StopFlourishesOnDeath(sprite, state);
        });
    }

    /// <summary>
    /// Empties the blink and the shine tracks once the death animation starts.
    /// </summary>
    /// <remarks>
    /// Each track holds minutes of entries and keeps a clock of its own, thus both would go on over
    /// the corpse. The death animation lays the model down and these two tracks hold the eyes and
    /// the staff gem where the standing model had them, thus they float clear of the body.
    ///
    /// Spine reports the start of every entry on every track. The animation on track 0 at that
    /// moment is the one to read, thus a blink that starts after the death animation also finds it.
    /// The handler stays connected for the life of the sprite, the way CreatureAnimator leaves its
    /// three. An empty track that empties again costs nothing.
    /// </remarks>
    private static void StopFlourishesOnDeath(MegaSprite sprite, MegaAnimationState state)
    {
        if (DeathAnimation == null) return;

        // Once, and deferred. FadeOutTrack starts an empty animation, which raises this same signal
        // while the death animation is still current on track 0, so an unguarded handler re-enters
        // itself inside the Spine update and hangs or crashes the main thread the moment the player
        // dies (repro: console `die`; the Give Up option hits the same path)
        var faded = false;
        sprite.ConnectAnimationStarted(Callable.From<GodotObject, GodotObject, GodotObject>((_, _, _) =>
        {
            if (faded || state.GetCurrentAnimationName() != DeathAnimation) return;
            faded = true;
            Callable.From(() =>
            {
                SpineModel.FadeOutTrack(state, BlinkTrack, FlourishFadeOut);
                SpineModel.FadeOutTrack(state, ShineTrack, FlourishFadeOut);
            }).CallDeferred();
        }));
    }

    private static void QueueFlourish(MegaAnimationState state, string? animation, int track,
        float minGap, float maxGap)
    {
        if (animation == null) return;

        var length = 0f;
        for (var i = 0; i < QueuedLoops; i++)
        {
            var gap = minGap + (float)FlourishRng.NextDouble() * (maxGap - minGap);

            // Spine counts the delay from the start of the entry before, thus each wait must also
            // carry the length of the animation. The first entry counts from now
            var delay = i == 0 ? gap : length + gap;

            var played = GameCompat.QueueBlink(state, animation, delay, track);

            if (i == 0) length = played;
        }
    }
}
