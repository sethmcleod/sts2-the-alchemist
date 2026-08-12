using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace Alchemist.AlchemistCode.Potions;

// Written against stock Godot nodes and Tweens on purpose. Godot virtual methods do not dispatch on
// node types declared in a mod assembly, so a _Process-driven node like the one The Witch uses would
// silently never run here. Tweens and CpuParticles2D are engine types, so they always do.
public static class UnstablePotionVfx
{
    // The Witch's own green, matched exactly so the effect reads as one shared mechanic across mods
    private static readonly Color Toxic = new("83eb85");

    private const string AmbientName = "UnstableAmbientVfx";
    private const string BurstName = "UnstableBurst";
    private const float PulseSeconds = 1.3f;
    private const float TintStrength = 0.45f;
    private const float ShakeSeconds = 0.2f;

    private static readonly Dictionary<ulong, Tween> Ambient = new();

    public static void Attach(NPotion potion)
    {
        if (!GodotObject.IsInstanceValid(potion) || potion.Image is not { } image) return;
        if (Ambient.ContainsKey(potion.GetInstanceId())) return;

        image.PivotOffset = image.Size * 0.5f;

        var tint = Colors.White.Lerp(Toxic, TintStrength);
        var tween = potion.CreateTween().SetLoops();
        tween.TweenProperty(image, "modulate", tint, PulseSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(image, "modulate", Colors.White, PulseSeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenInterval(4.0);
        tween.TweenProperty(image, "rotation_degrees", 4f, 0.09);
        tween.TweenProperty(image, "rotation_degrees", -4f, 0.09);
        tween.TweenProperty(image, "rotation_degrees", 0f, 0.09);

        Ambient[potion.GetInstanceId()] = tween;

        if (potion.GetNodeOrNull<Node2D>(AmbientName) != null) return;
        var fumes = new Node2D { Name = AmbientName, Position = Center(image) };
        fumes.AddChild(AmbientFumes());
        potion.AddChild(fumes);
    }

    public static void Detach(NPotion potion)
    {
        if (!GodotObject.IsInstanceValid(potion)) return;
        if (Ambient.Remove(potion.GetInstanceId(), out var tween) && GodotObject.IsInstanceValid(tween))
            tween.Kill();

        potion.GetNodeOrNull<Node2D>(AmbientName)?.QueueFree();

        if (potion.Image is not { } image) return;
        image.Modulate = Colors.White;
        image.RotationDegrees = 0f;
    }

    // fade rather than hanging in the air after the burst, and killing the looping tween hands the
    // rotation to the shake alone instead of leaving two tweens fighting over the same property
    public static void Shake(NPotion potion)
    {
        if (!GodotObject.IsInstanceValid(potion) || potion.Image is not { } image) return;

        if (Ambient.Remove(potion.GetInstanceId(), out var ambient) && GodotObject.IsInstanceValid(ambient))
            ambient.Kill();
        FadeAmbient(potion);

        image.PivotOffset = image.Size * 0.5f;
        var tween = potion.CreateTween();
        var step = ShakeSeconds / 6f;
        foreach (var angle in new[] { 14f, -13f, 11f, -8f, 5f, 0f })
            tween.TweenProperty(image, "rotation_degrees", angle, step);
    }

    // Stops new fumes at once and fades the ones already in the air, over the shake's own length, so
    // the potion is clean by the time the burst goes off
    private static void FadeAmbient(NPotion potion)
    {
        if (potion.GetNodeOrNull<Node2D>(AmbientName) is not { } fumes) return;

        foreach (var child in fumes.GetChildren())
            if (child is CpuParticles2D particles)
                particles.Emitting = false;

        var fade = fumes.CreateTween();
        fade.TweenProperty(fumes, "modulate:a", 0f, ShakeSeconds);
        fade.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(fumes)) fumes.QueueFree();
        }));
    }

    // Glass and fumes. Built in code rather than from a scene file, because a mod scene that references
    // base-game textures bakes those references to null on export
    public static void PlayBurst(Node host, Vector2 at)
    {
        if (!GodotObject.IsInstanceValid(host)) return;

        NDebugAudioManager.Instance?.Play("glass_orb_evoke.mp3", 1f, PitchVariance.Small);

        var burst = new Node2D { Name = BurstName, Position = at };
        burst.AddChild(Shards());
        burst.AddChild(BurstFumes());
        host.AddChild(burst);

        foreach (var child in burst.GetChildren())
            if (child is CpuParticles2D particles)
            {
                particles.Emitting = true;
                particles.Restart();
            }

        host.GetTree().CreateTimer(1.5).Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(burst)) burst.QueueFree();
        };
    }

    // A Control child sits at its parent's top-left, and a potion icon is only about 40px wide, so an
    // effect placed at the origin reads as coming from off past the corner of the belt
    public static Vector2 Center(Control control) => control.Position + control.Size * 0.5f;

    // Sizes are deliberately small. light.png is a broad soft glow, so at anything near its native
    // scale one particle covers the whole top bar
    private static CpuParticles2D AmbientFumes() => new()
    {
        Name = "Fumes",
        Texture = ResourceLoader.Load<Texture2D>("res://images/vfx/dot.png"),
        Amount = 5,
        Lifetime = 1.8f,
        Explosiveness = 0f,
        Spread = 25f,
        Direction = Vector2.Up,
        Gravity = new Vector2(0f, -18f),
        InitialVelocityMin = 6f,
        InitialVelocityMax = 16f,
        ScaleAmountMin = 0.06f,
        ScaleAmountMax = 0.14f,
        EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere,
        EmissionSphereRadius = 9f,
        Color = new Color(Toxic, 0.7f),
    };

    private static CpuParticles2D Shards() => new()
    {
        Name = "Shards",
        Texture = ResourceLoader.Load<Texture2D>("res://images/vfx/dot.png"),
        Amount = 10,
        Lifetime = 0.55f,
        OneShot = true,
        Explosiveness = 1f,
        Spread = 180f,
        Gravity = new Vector2(0f, 320f),
        InitialVelocityMin = 45f,
        InitialVelocityMax = 110f,
        ScaleAmountMin = 0.1f,
        ScaleAmountMax = 0.22f,
        Color = Toxic,
    };

    private static CpuParticles2D BurstFumes() => new()
    {
        Name = "Fumes",
        Texture = ResourceLoader.Load<Texture2D>("res://images/vfx/light.png"),
        Material = ResourceLoader.Load<Material>("res://themes/canvas_item_material_additive_shared.tres"),
        Amount = 4,
        Lifetime = 0.8f,
        OneShot = true,
        Explosiveness = 0.85f,
        Spread = 45f,
        Direction = Vector2.Up,
        Gravity = new Vector2(0f, -40f),
        InitialVelocityMin = 12f,
        InitialVelocityMax = 34f,
        ScaleAmountMin = 0.09f,
        ScaleAmountMax = 0.18f,
        Color = new Color(Toxic, 0.55f),
    };
}
