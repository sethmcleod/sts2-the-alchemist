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
            // The Alchemist background is the one with this marker node; the root's name
            // does not survive instantiation under the screen
            if (child is Control bg && bg.GetNodeOrNull("AlchemistSelectMarker") != null)
                AttachSpineScene(bg);
        }
    }

    // The animated select screen is a Spine scene with the background, character and
    // motion baked in; a mod scene cannot hold a SpineSprite, so it is built here
    private const string SelectDir = $"{MainFile.ResPath}/animations/character_select/alchemist";

    static void AttachSpineScene(Control bg)
    {
        if (bg.GetNodeOrNull("SelectScreenSpine") != null)
            return;

        if (SpineModel.Load($"{SelectDir}/select_screen.atlas", $"{SelectDir}/select_screen.skel")
            is not { } data)
            return;

        // The scene is painted on a 2562x1479 canvas centred on the skeleton origin (the bg
        // attachment's untrimmed size). The character reads right at canvas-to-visible-rect
        // size, but at that size the background stops just past the staff. So the skeleton
        // renders twice: an environment layer scaled up so the swirl covers the oversized
        // AnimatedBg container at any aspect ratio, and the character layer at mockup size.
        // Each hides the other's slots, so nothing draws twice and the swirl has no seam
        // The scene is painted on a 2562x1479 canvas centred on the skeleton origin (the bg
        // attachment's untrimmed size), with the swirl radiating from behind the staff orb.
        // The canvas maps to the visible rect; the composition (orb on swirl, bleed for wide
        // aspect ratios) is the rig's to solve, so this stays one sprite at one scale
        var design = new Vector2(2562, 1479);
        var scale = bg.Size.X / design.X;
        if (SpineModel.CreateSprite(data, scale) is not { } sprite)
            return;

        sprite.Name = "SelectScreenSpine";
        sprite.Position = bg.Size * 0.5f;
        sprite.AddChild(new NSpineAutoPlayer());
        bg.AddChild(sprite);
        // Behind the particle nodes
        bg.MoveChild(sprite, 0);
    }

}
