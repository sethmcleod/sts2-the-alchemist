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

        if (SpineModel.CreateSprite(data, 0.65f) is not { } sprite)
            return;

        sprite.Name = "SelectScreenSpine";
        sprite.AddChild(new NSpineAutoPlayer());

        // Under the painting: a radial fade in the swirl's own purples, centred behind the
        // staff orb, so anything the painting cannot reach reads as the swirl thinning out
        // rather than a flat wall
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

        // Layout() re-derives that framing from whatever rect is actually visible, so wider
        // windows scale the painting up just enough to stay covered edge to edge, and a
        // window resize re-runs it. The visible rect comes from the live canvas transform,
        // which also sidesteps bg.Size being the pre-layout 1920x1080 design size here
        var lastVis = new Rect2();
        void Layout()
        {
            if (!GodotObject.IsInstanceValid(sprite) || !GodotObject.IsInstanceValid(bg)) return;
            var toLocal = bg.GetGlobalTransformWithCanvas().AffineInverse();
            var vis = toLocal * bg.GetViewportRect();
            // Per-frame cost stops here unless the view actually moved: the writes below are
            // cheap except the gradient fill points, which regenerate the texture
            if (vis.IsEqualApprox(lastVis)) return;
            lastVis = vis;

            // 2844 wide, not the full measured 3001: the painting's top-left corner is arm
            // gaps, and on ~2.2:1 windows the wider fit walked the view into it. Stopping the
            // usable left edge at -1360 keeps every corner in paint verified solid; the 16:9
            // framing binds on height and is unchanged by this
            var scale = 1.03f * Mathf.Max(vis.Size.X / 2844f, vis.Size.Y / 1552f);
            var posY = vis.End.Y - 583f * scale;
            // Cover clamps: keep the envelope over both side edges, preferring the tuned bias
            var posLo = vis.End.X - 1484f * scale;
            var posHi = vis.Position.X + 1360f * scale;
            var posX = Mathf.Clamp(vis.GetCenter().X + 11f, Mathf.Min(posLo, posHi), Mathf.Max(posLo, posHi));
            sprite.Scale = new Vector2(scale, scale);
            sprite.Position = new Vector2(posX, posY);

            // The gradient covers a margin past the visible rect and its centre tracks the
            // orb (skeleton -650, +680) through the same transform as the painting
            under.Position = vis.Position - vis.Size * 0.25f;
            under.Size = vis.Size * 1.5f;
            var orb = new Vector2(posX + scale * -650f, posY - scale * 680f);
            var from = (orb - under.Position) / under.Size;
            gradientTex.FillFrom = from.Clamp(Vector2.Zero, Vector2.One);
            gradientTex.FillTo = gradientTex.FillFrom + new Vector2(0.85f, 0f);
        }

        // Every frame, not on Resized: letterboxing and the menu's open animation change the
        // canvas transform without touching bg.Size, so a signal-driven layout goes stale. A
        // mod-defined Node subclass cannot be AddChild'd (ScriptManagerBridge has no script
        // resource for it), so the tree's own per-frame signal drives it instead, detached
        // when the background leaves the tree. The math is a handful of multiplies
        var tree = bg.GetTree();
        tree.ProcessFrame += Layout;
        bg.TreeExiting += () => tree.ProcessFrame -= Layout;
    }

}
