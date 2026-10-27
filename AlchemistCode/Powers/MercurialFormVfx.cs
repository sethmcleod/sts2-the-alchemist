using Alchemist.AlchemistCode.Character;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Forms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Alchemist.AlchemistCode.Powers;

internal static class MercurialFormVfx
{
    private const string Dir = $"{MainFile.ResPath}/animations/vfx/mercurial_form";
    private const string AtlasPath = $"{Dir}/mercurial_form.atlas";
    private const string SkeletonPath = $"{Dir}/mercurial_form.skel";

    public const string TexturePath = $"{Dir}/mercurial_form.png";

    private const string AppearAnimationLeaf = "appear";
    private const string IdleAnimationLeaf = "idle_loop";
    private const string TriggeredAnimationLeaf = "effect_triggered";

    private const float SwirlDiameter = 488f;
    private const float SwirlHeight = 298f;

    private const float SizeToBody = 0.92f;
    private const float ChestHeight = 0.66f;
    private const float FadeOutDuration = 0.3f;

    private static string? _idle;
    private static string? _triggered;

    public static NFormVfx? Create(Creature owner)
    {
        if (TestMode.IsOn) return null;
        if (NCombatRoom.Instance?.GetCreatureNode(owner)?.Visuals is not { FormVfxHolder: not null } visuals)
            return null;
        if (SpineModel.Load(AtlasPath, SkeletonPath) is not { } data) return null;

        var body = visuals.Bounds;
        var scale = body.Size.Y * SizeToBody / SwirlDiameter;
        if (SpineModel.CreateSprite(data, scale) is not { } sprite) return null;

        var chest = new Vector2(
            body.Position.X + body.Size.X / 2f,
            body.Position.Y + body.Size.Y * (1f - ChestHeight));
        sprite.Position = chest + new Vector2(0f, SwirlHeight * scale);

        var vfx = new NFormVfx { Name = "MercurialFormVfx" };
        vfx.AddChild(sprite);
        visuals.AddFormVfx(vfx);

        var appear = SpineModel.ResolveAnimation(data, AppearAnimationLeaf);
        _idle = SpineModel.ResolveAnimation(data, IdleAnimationLeaf);
        _triggered = SpineModel.ResolveAnimation(data, TriggeredAnimationLeaf);
        vfx.RunWhenSpineReady(new MegaSprite(sprite), state =>
        {
            if (appear != null) state.SetAnimation(appear, loop: false);
            if (_idle == null) return;
            if (appear != null) state.AddAnimation(_idle);
            else state.SetAnimation(_idle);
        });

        return vfx;
    }

    public static void OnEffectTriggered(NFormVfx? vfx)
    {
        if (_triggered == null || vfx == null || !GodotObject.IsInstanceValid(vfx)) return;
        if (vfx.GetChildCount() == 0 || vfx.GetChild(0) is not Node2D node) return;

        var sprite = new MegaSprite(node);
        if (!sprite.IsAnimationStateReady()) return;

        var state = sprite.GetAnimationState();
        state.SetAnimation(_triggered, loop: false);
        if (_idle != null) state.AddAnimation(_idle);
    }

    public static void FadeOut(NFormVfx? vfx)
    {
        if (vfx == null || !GodotObject.IsInstanceValid(vfx) || !vfx.IsInsideTree()) return;

        var tween = vfx.CreateTween();
        tween.TweenProperty(vfx, "modulate:a", 0f, FadeOutDuration);
        tween.TweenCallback(Callable.From(vfx.QueueFree));
    }
}
