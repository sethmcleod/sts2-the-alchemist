using System.Collections.Generic;
using System.Reflection;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace Alchemist.AlchemistCode.Character;

/// <summary>
/// A self-wiring <see cref="NParticlesContainer"/> for the custom energy counter.
///
/// Two problems are solved here at runtime:
/// 1. BaseLib's scene converter keeps this node (it already is-a NParticlesContainer) but does not
///    populate the private <c>_particles</c> export, so we collect the GPUParticles2D children ourselves.
/// 2. References to base-game resources (textures/materials/shaders) bake as null inside a mod scene,
///    because they don't exist in the mod project at export time. So instead of referencing them in the
///    .tscn, we load them here via ResourceLoader (which resolves against the mounted base-game PCK).
/// </summary>
[GlobalClass]
public partial class AlchemistParticles : NParticlesContainer
{
    private static readonly FieldInfo ParticlesField =
        typeof(NParticlesContainer).GetField("_particles", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly Color Gold = new("E9AB2F");

    // Maps each particle node (by name) to its base-game texture/material. recolorLut = rebuild the
    // flipbook shader's colour LUT to gold (the ribbon bakes its colour into the material, not self_modulate).
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

    /// <summary>
    /// Duplicates a flipbook ShaderMaterial and swaps its colour LUT for a gold gradient.
    /// </summary>
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
