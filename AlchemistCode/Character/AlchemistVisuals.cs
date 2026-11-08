using Alchemist.AlchemistCode.Compat;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Alchemist.AlchemistCode.Character;

internal static class AlchemistVisuals
{
    private const string ModelDir = $"{MainFile.ResPath}/animations/characters/alchemist";
    private const string AtlasPath = $"{ModelDir}/alchemist_model.atlas";
    private const string SkeletonPath = $"{ModelDir}/alchemist_model.skel";

    public const string TexturePath = $"{ModelDir}/alchemist_model.png";

    private const string IdleAnimationLeaf = "idle_loop";
    private const string BlinkAnimationLeaf = "blink";
    private const string HurtAnimationLeaf = "hurt";
    private const string AttackAnimationLeaf = "attack";
    private const string HeavyAttackAnimationLeaf = "attack_heavy";
    private const string CastAnimationLeaf = "cast";
    private const string DeathAnimationLeaf = "die";
    private const string RelaxedAnimationLeaf = "relaxed_loop";
    private const string NearDeathAnimationLeaf = "near_death_loop";
    private const string LowHealthAnimationLeaf = "low_health_loop";
    private const string ShineAnimationLeaf = "shine";

    private const int BlinkTrack = 1;
    private const int ShineTrack = 2;

    // A track stops when its queue empties, so each queue must outlast any fight
    private const int QueuedLoops = 400;
    private const float MinBlinkGap = 3f;
    private const float MaxBlinkGap = 9f;
    private const float MinShineGap = 7f;
    private const float MaxShineGap = 9f;

    private const float FlourishFadeOut = 0.15f;

    // Not the run's seeded RNG, so cosmetic timing never moves the seed
    private static readonly Random FlourishRng = new();

    public static string IdleAnimation { get; private set; } = IdleAnimationLeaf;
    public static string? BlinkAnimation { get; private set; }
    public static string? HurtAnimation { get; private set; }
    public static string? AttackAnimation { get; private set; }
    public static string? HeavyAttackAnimation { get; private set; }
    public static string? CastAnimation { get; private set; }
    public static string? DeathAnimation { get; private set; }
    public static string? RelaxedAnimation { get; private set; }
    public static string? NearDeathAnimation { get; private set; }
    public static string? ShineAnimation { get; private set; }

    private const float ModelHeight = 272f;

    // The anchors were measured on a 296 px model
    private const float AnchorScale = ModelHeight / 296f;

    private const float FallbackSkeletonHeight = 833f;

    private static readonly Vector2 BoundsPosition = new(-118 * AnchorScale, -296 * AnchorScale);
    private static readonly Vector2 BoundsSize = new(228 * AnchorScale, 296 * AnchorScale);
    private static readonly Vector2 CenterPosition = new(0, -170 * AnchorScale);
    private static readonly Vector2 IntentPosition = new(0, -300 * AnchorScale);

    private static Resource? _skeletonData;

    public static readonly string[] HeadBones = ["head", "head_extra_1"];

    public static readonly Rainbow.Rig RainbowRig = new(
        Rainbow.Parts.Robe | Rainbow.Parts.Skin | Rainbow.Parts.Bag,
        Rainbow.Slots(
            (Rainbow.Parts.Eyes, ["eye_l", "eye_r", "eye_l_extra_2", "eye_r_extra1"]),
            (Rainbow.Parts.Orb, ["staff_gem1", "staff_gem2", "staff_gem_white", "staff_gem_white4",
                "staff_shine_a1", "staff_shine_a2", "star_a1", "star_a2", "Light", "Light2"]),
            (Rainbow.Parts.Skin | Rainbow.Parts.Orb, ["staff_extra1", "staff_extra2"]),
            (Rainbow.Parts.None, ["staff1", "staff2",
                "herbs1", "herbs2", "herbs3", "herbs4", "herbs5", "herbs6", "herbs7", "herbs8",
                "potion1", "potion2", "potion3", "potion4", "potion5", "potion6",
                "arrow_a1", "Trail_a1", "Trail_a2", "Trail_a3", "Trail_b1"])));

    public static NCreatureVisuals? Create()
    {
        var sprite = CreateSprite();
        if (sprite == null) return null;

        var visuals = new NCreatureVisuals();

        // AddFormVfx throws without this holder
        visuals.AddUnique(new Control(), "FormVfx");

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

        return visuals;
    }

    private static Node2D? CreateSprite()
    {
        var data = SkeletonData();
        if (data == null) return null;

        BigHead.Apply(data, HeadBones);

        var scale = ModelHeight / SpineModel.AboveOrigin(data, FallbackSkeletonHeight);
        var sprite = SpineModel.CreateSprite(data, scale);
        if (sprite != null) Rainbow.Apply(sprite, data, RainbowRig);
        return sprite;
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

    private static void StopFlourishesOnDeath(MegaSprite sprite, MegaAnimationState state)
    {
        if (DeathAnimation == null) return;

        // Guarded and deferred: FadeOutTrack raises this signal again and would re-enter inside the Spine update
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

            // Spine counts each delay from the start of the entry before, so it carries that entry's length
            var delay = i == 0 ? gap : length + gap;

            var played = GameCompat.QueueBlink(state, animation, delay, track);

            if (i == 0) length = played;
        }
    }
}
