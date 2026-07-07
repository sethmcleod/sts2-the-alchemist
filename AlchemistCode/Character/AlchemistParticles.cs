using System.Collections.Generic;
using System.Reflection;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace Alchemist.AlchemistCode.Character;

// Self-wiring energy-counter particles: base-game resource refs bake to null in a mod scene, so we
// collect the GpuParticles2D children and load their textures/materials at runtime instead
[GlobalClass]
public partial class AlchemistParticles : NParticlesContainer
{
    private static readonly FieldInfo ParticlesField =
        typeof(NParticlesContainer).GetField("_particles", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly Color Gold = new("E9AB2F");

    // recolorLut rebuilds the flipbook shader's colour LUT to gold instead of using self_modulate
    private static readonly System.Collections.Generic.Dictionary<string, (string tex, string mat, bool recolorLut)> Setup = new()
    {
        ["Glow"] = ("res://images/vfx/common/common_glow.png", "res://materials/vfx/common/vfx_glow.tres", false),
        ["Ring"] = ("res://images/vfx/common/common_ring_polar_a.png", "res://materials/vfx/common/vfx_ring_polar.tres", false),
        ["Ribbon"] = ("res://images/vfx/ribbon_flipbook/ribbon_flipbook_2.png", "res://materials/vfx/ribbon_flipbook/vfx_ribbon_flipbook_1_2_green.tres", true),
        ["Specks"] = ("res://images/vfx/common/common_speck.png", "res://materials/vfx/common/vfx_speck_glow_white.tres", false),
    };

    public override void _Ready()
    {
        base._Ready();

        var collected = new Array<GpuParticles2D>();
        Collect(this, collected);
        if (collected.Count > 0)
            ParticlesField.SetValue(this, collected);

        foreach (var p in collected)
        {
            if (!Setup.TryGetValue(p.Name, out var s))
                continue;

            var tex = ResourceLoader.Load<Texture2D>(s.tex);
            if (tex != null)
                p.Texture = tex;

            var mat = ResourceLoader.Load<Material>(s.mat);
            if (mat != null)
                p.Material = s.recolorLut ? RecolorToGold(mat) : mat;
        }
    }

    private static Material RecolorToGold(Material source)
    {
        if (source is not ShaderMaterial shader)
            return source;

        var dup = (ShaderMaterial)shader.Duplicate(true);
        var gradient = new Gradient
        {
            Offsets = [0.41f, 0.87f],
            Colors = [new Color(0.55f, 0.38f, 0.1f), new Color(0.96f, 0.78f, 0.3f)],
        };
        dup.SetShaderParameter("lut", new GradientTexture1D { Gradient = gradient });
        return dup;
    }

    private static void Collect(Node node, Array<GpuParticles2D> particles)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is GpuParticles2D p)
                particles.Add(p);
            Collect(child, particles);
        }
    }
}
