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

        // Only the solid part of the painting may touch the viewport: the swirl arms
        // around it have transparent gaps, backed by the flat rect below in the swirl's own
        // edge purple. The gap-free envelope was measured in the running game with a magenta
        // underlay (skeleton-space x -1517..1484, y -519..1033, y-up), and the scale and
        // offset were then tuned live against it with the modder (2026-08-20): the height
        // fit binds, the framing favours the character's lower half, and a full-frame leak
        // check at these exact values found no exposed underlay at 16:9. bg.Size is the
        // 1920x1080 design rect here; layout inflates the control afterwards, carrying the
        // menu's own off-screen bleed for wider ratios
        var scale = bg.Size.Y / 1080f * 0.65f;
        if (SpineModel.CreateSprite(data, scale) is not { } sprite)
            return;

        sprite.Name = "SelectScreenSpine";
        sprite.Position = new Vector2(bg.Size.X / 1920f * 966f, bg.Size.Y / 1080f * 650f);
        sprite.AddChild(new NSpineAutoPlayer());

        var under = new ColorRect
        {
            Name = "SelectScreenBase",
            Color = new Color(0x2e / 255f, 0x20 / 255f, 0x50 / 255f),
            Size = bg.Size,
        };
        bg.AddChild(under);
        bg.AddChild(sprite);
        bg.MoveChild(under, 0);
        bg.MoveChild(sprite, 1);
    }

}
