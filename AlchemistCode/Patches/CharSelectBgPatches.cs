using Alchemist.AlchemistCode.Character;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Animation;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Alchemist.AlchemistCode.Patches;

// A base-game resource reference bakes to null in a mod scene, and a C# script on the scene root does not
// bind when the screen instantiates it. So these patches find the instantiated background and load its
// particle assets at runtime. The scene is Alchemist/scenes/screens/char_select/char_select_bg_alchemist.tscn
[HarmonyPatch(typeof(NCharacterSelectScreen))]
class CharSelectBgPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("SelectCharacter")]
    static void AfterSelect(NCharacterSelectScreen __instance) => Apply(__instance);

    // The Random pick instantiates the revealed character's background through this path
    [HarmonyPostfix]
    [HarmonyPatch("OnLocalCharacterChangedForRandom")]
    static void AfterRandom(NCharacterSelectScreen __instance) => Apply(__instance);

    static void Apply(NCharacterSelectScreen screen)
    {
        var container = screen.GetNodeOrNull<Control>("AnimatedBg");
        if (container == null)
            return;

        foreach (var child in container.GetChildren())
        {
            // The Alchemist background is the one with these marker nodes
            if (child is Control bg && bg.GetNodeOrNull<TextureRect>("Gradient") != null
                && bg.GetNodeOrNull<CpuParticles2D>("SpecksGold") != null)
            {
                ApplyParticleAssets(bg);
                if (!AttachSpineScene(bg))
                    AnimateCharacter(bg);
            }
        }
    }

    // The animated select screen is a Spine scene with the background, character and
    // particles baked in. It replaces the flat art when its skeleton loads; the flat
    // art with the breathing tween stays as the fallback
    private const string SelectDir = $"{MainFile.ResPath}/animations/character_select/alchemist";

    static bool AttachSpineScene(Control bg)
    {
        if (bg.GetNodeOrNull("SelectScreenSpine") != null)
            return true;

        // The animated screen ships when the animator's export lands; absence is not an error
        if (!Godot.FileAccess.FileExists($"{SelectDir}/select_screen.skel"))
            return false;

        if (SpineModel.Load($"{SelectDir}/select_screen.atlas", $"{SelectDir}/select_screen.skel")
            is not { } data)
            return false;

        // The skeleton's box maps onto the bg rect the way the base scenes place theirs:
        // fill the width, centre vertically. Spine y grows upward, so the box centre in
        // Godot space is the negated y
        var box = SpineModel.Bounds(data, new Rect2(-1280, -600, 2560, 1200));
        var scale = bg.Size.X / box.Size.X;
        if (SpineModel.CreateSprite(data, scale) is not { } sprite)
            return false;

        sprite.Name = "SelectScreenSpine";
        sprite.Position = new Vector2(
            bg.Size.X * 0.5f - (box.Position.X + box.Size.X * 0.5f) * scale,
            bg.Size.Y * 0.5f + (box.Position.Y + box.Size.Y * 0.5f) * scale);
        sprite.AddChild(new NSpineAutoPlayer());
        bg.AddChild(sprite);
        // Behind the particle nodes, in place of the flat art
        bg.MoveChild(sprite, 0);

        if (bg.GetNodeOrNull<TextureRect>("Character") is { } character) character.Visible = false;
        if (bg.GetNodeOrNull<TextureRect>("Gradient") is { } gradient) gradient.Visible = false;
        return true;
    }

    // A slow breathing idle for the flat character art: a gentle sine scale pivoted at
    // the feet (the pivot is set in the scene), so the chest rises and the feet stay
    static void AnimateCharacter(Control bg)
    {
        if (bg.GetNodeOrNull<TextureRect>("Character") is not { } character)
            return;

        var tween = character.CreateTween().SetLoops();
        tween.TweenProperty(character, "scale", new Vector2(1.004f, 1.012f), 2.6)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(character, "scale", Vector2.One, 2.6)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    static void ApplyParticleAssets(Control bg)
    {
        var glow = ResourceLoader.Load<Texture2D>("res://images/vfx/light.png");
        var additive = ResourceLoader.Load<Material>("res://themes/canvas_item_material_additive_shared.tres");
        var dot = ResourceLoader.Load<Texture2D>("res://images/vfx/dot.png");

        foreach (var child in bg.GetChildren())
        {
            if (child is not CpuParticles2D particles)
                continue;

            if (particles.Name.ToString().StartsWith("Light"))
            {
                particles.Texture = glow;
                particles.Material = additive;
                // Pin the pulse phase to engine time. The swirl shader computes the same phase from
                // TIME, so its light masks breathe with the visible lights
                double now = Time.GetTicksMsec() / 1000.0;
                particles.Preprocess = now % particles.Lifetime;
            }
            else
            {
                particles.Texture = dot;
            }
        }
    }
}
