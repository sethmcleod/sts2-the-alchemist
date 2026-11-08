using Alchemist.AlchemistCode.Character;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Animation;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Alchemist.AlchemistCode.Patches;

// Base-game resource refs bake to null in a mod scene and root scripts don't bind, so assets load here
[HarmonyPatch(typeof(NCharacterSelectScreen))]
class CharSelectBgPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("SelectCharacter")]
    static void AfterSelect(NCharacterSelectScreen __instance) => Apply(__instance);

    [HarmonyPostfix]
    [HarmonyPatch("OnLocalCharacterChangedForRandom")]
    static void AfterRandom(NCharacterSelectScreen __instance) => Apply(__instance);

    // The multiplayer load lobby builds the same bg into its own AnimatedBg
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NMultiplayerLoadGameScreen), "AfterMultiplayerStarted")]
    static void AfterLoadLobby(NMultiplayerLoadGameScreen __instance) => Apply(__instance);

    static void Apply(Control screen)
    {
        var container = screen.GetNodeOrNull<Control>("AnimatedBg");
        if (container == null)
            return;

        foreach (var child in container.GetChildren())
        {
            // The root's name doesn't survive instantiation, so match on the marker node
            if (child is Control bg && bg.GetNodeOrNull("AlchemistSelectMarker") != null)
                AttachSpineScene(bg);
        }
    }

    private const string SelectDir = $"{MainFile.ResPath}/animations/character_select/alchemist";
    private const string SelectAtlas = $"{SelectDir}/select_screen.atlas";

    private static readonly string[] HeadBones = ["head"];

    // Radii run 1 px past the measured eyes (66 and 62.5)
    private const float HeadWidth = 350f;
    private const float HeadHeight = 296f;
    private static readonly Rainbow.EyeMap HeadEyes = new("head", SelectAtlas, "head",
        Left: new Vector4(127.9f / HeadWidth, 127.5f / HeadHeight, 67f / HeadWidth, 67f / HeadHeight),
        Right: new Vector4(284f / HeadWidth, 158.5f / HeadHeight, 63.5f / HeadWidth, 63.5f / HeadHeight));

    private static readonly Rainbow.Rig RainbowRig = new(
        Rainbow.Parts.None,
        Rainbow.Slots(
            (Rainbow.Parts.Robe | Rainbow.Parts.Bag | Rainbow.Parts.Light, ["body"]),
            (Rainbow.Parts.Robe | Rainbow.Parts.Light, ["arm_r_1"]),
            (Rainbow.Parts.Robe, ["hand_l_2"]),
            (Rainbow.Parts.Robe | Rainbow.Parts.Skin | Rainbow.Parts.Light, ["hand_l_1"]),
            (Rainbow.Parts.Robe | Rainbow.Parts.Skin | Rainbow.Parts.Orb, ["arm_r_2"]),
            (Rainbow.Parts.Skin | Rainbow.Parts.Eyes | Rainbow.Parts.Light, ["head"]),
            (Rainbow.Parts.Orb, ["staff_light"])),
        HeadEyes);

    static void AttachSpineScene(Control bg)
    {
        if (bg.GetNodeOrNull("SelectScreenSpine") != null)
            return;

        if (SpineModel.Load(SelectAtlas, $"{SelectDir}/select_screen.skel") is not { } data)
            return;

        BigHead.Apply(data, HeadBones);

        if (SpineModel.CreateSprite(data, 0.65f) is not { } sprite)
            return;

        sprite.Name = "SelectScreenSpine";
        sprite.AddChild(new NSpineAutoPlayer());

        var gradient = new Gradient();
        gradient.SetColor(0, new Color("4a3675"));
        gradient.SetColor(1, new Color("241a3f"));
        var gradientTex = new GradientTexture2D
        {
            Gradient = gradient,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.3f, 0.3f),
            FillTo = new Vector2(1.0f, 0.3f),
            Width = 512,
            Height = 256,
        };
        var under = new TextureRect
        {
            Name = "SelectScreenBase",
            Texture = gradientTex,
            StretchMode = TextureRect.StretchModeEnum.Scale,
        };
        bg.AddChild(under);
        bg.AddChild(sprite);
        bg.MoveChild(under, 0);
        bg.MoveChild(sprite, 1);
        Rainbow.Apply(sprite, data, RainbowRig);

        // The visible rect comes from the canvas transform, since bg.Size is still the 1920x1080 design size
        var lastVis = new Rect2();
        void Layout()
        {
            if (!GodotObject.IsInstanceValid(sprite) || !GodotObject.IsInstanceValid(bg)) return;
            // A tinted underlay holds the layout still so the sprite can be posed by hand
            if (under.Modulate != Colors.White) return;
            var toLocal = bg.GetGlobalTransformWithCanvas().AffineInverse();
            var vis = toLocal * bg.GetViewportRect();
            if (vis.IsEqualApprox(lastVis)) return;
            lastVis = vis;

            // The painting's solid square spans x -2138..2044, y -721..1032; 1552 is the tuned 16:9 height
            var scale = 1.03f * Mathf.Max(vis.Size.X / 4182f, vis.Size.Y / 1552f);
            var posY = vis.End.Y - 583f * scale;
            var posLo = vis.End.X - 2044f * scale;
            var posHi = vis.Position.X + 2138f * scale;
            var posX = Mathf.Clamp(vis.GetCenter().X + 11f, Mathf.Min(posLo, posHi), Mathf.Max(posLo, posHi));
            sprite.Scale = new Vector2(scale, scale);
            sprite.Position = new Vector2(posX, posY);

            under.Position = vis.Position - vis.Size * 0.25f;
            under.Size = vis.Size * 1.5f;
            var orb = new Vector2(posX + scale * -650f, posY - scale * 680f);
            var from = (orb - under.Position) / under.Size;
            gradientTex.FillFrom = from.Clamp(Vector2.Zero, Vector2.One);
            gradientTex.FillTo = gradientTex.FillFrom + new Vector2(0.85f, 0f);
        }

        // Per frame, since letterboxing moves the canvas with no Resized, and a mod Node subclass can't be AddChild'd
        var tree = bg.GetTree();
        tree.ProcessFrame += Layout;
        bg.TreeExiting += () => tree.ProcessFrame -= Layout;
    }

}
