using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Alchemist.AlchemistCode.Patches;

// The placeholder character-select background needs the base game's particle textures.
// A base-game reference bakes to null in a mod scene, and a C# script on the scene
// root does not bind when the screen instantiates it. So these patches find the
// instantiated background and load the resources at runtime. The scene is
// Alchemist/scenes/screens/char_select/char_select_bg_alchemist.tscn
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
                ApplyParticleAssets(bg);
        }
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
                // Pin the pulse phase to global engine time. The swirl shader computes the
                // same phase from TIME, so its light masks breathe with the visible lights
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
