using Alchemist.AlchemistCode.Config;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace Alchemist.AlchemistCode.Character;

/// <summary>
/// The rainbow secrets. One shader cycles each part of the Alchemist whose setting is on.
/// </summary>
/// <remarks>
/// A part is found by its hue inside the slots that can hold it. The sprite material covers the
/// slots that hold robe, skin and bag. A SpineSlotNode child gives one slot its own material, which
/// tells the eyes from the orb (both gold) and keeps the staff and the contents of the bag at their
/// base color.
/// </remarks>
internal static class Rainbow
{
    [Flags]
    public enum Parts
    {
        None = 0,
        Robe = 1,
        Skin = 2,
        Bag = 4,
        Eyes = 8,
        Orb = 16,
        // The light of the orb, painted along the edges of the other parts
        Light = 32
    }

    /// <summary>
    /// The parts under the sprite material, the slots with a material of their own, and the eyes of
    /// a slot that paints them together with the light of the orb.
    /// </summary>
    public sealed record Rig(Parts SpriteParts, IReadOnlyDictionary<string, Parts> Slots, EyeMap? Eyes = null);

    /// <summary>
    /// Where the eyes sit on the image of <paramref name="Slot"/>. The light of the orb is the same gold
    /// as the eyes, thus only their place tells the two apart. Each eye is an ellipse, its centre in xy
    /// and its radii in zw, as fractions of the image.
    /// </summary>
    public sealed record EyeMap(string Slot, string AtlasPath, string Region, Vector4 Left, Vector4 Right);

    public static IReadOnlyDictionary<string, Parts> Slots(params (Parts Parts, string[] Names)[] groups) =>
        groups.SelectMany(group => group.Names.Select(name => (name, group.Parts)))
            .ToDictionary(slot => slot.name, slot => slot.Parts);

    private const string ShaderPath = $"{MainFile.ResPath}/shaders/rainbow.gdshader";
    private const string SlotNodeClass = "SpineSlotNode";

    // One material for each mix of parts and eyes, shared by every sprite. The cycle runs on TIME,
    // thus all parts stay in step
    private static readonly Dictionary<(Parts Parts, Vector4 LeftEye, Vector4 RightEye), Material> Materials = [];
    private static Shader? _shader;

    private static Parts Enabled
    {
        get
        {
            if (!SecretUnlocks.IsUnlocked) return Parts.None;

            var parts = Parts.None;
            if (AlchemistModConfig.RainbowRobes) parts |= Parts.Robe;
            if (AlchemistModConfig.RainbowEyes) parts |= Parts.Eyes;
            if (AlchemistModConfig.RainbowOrb) parts |= Parts.Orb | Parts.Light;
            if (AlchemistModConfig.RainbowSkin) parts |= Parts.Skin;
            if (AlchemistModConfig.RainbowBag) parts |= Parts.Bag;
            return parts;
        }
    }

    public static void Apply(Node2D sprite, Resource skeleton, Rig rig)
    {
        var enabled = Enabled;
        if (enabled == Parts.None) return;

        var spriteParts = rig.SpriteParts & enabled;
        if (spriteParts != Parts.None && MaterialFor(spriteParts, null) is { } spriteMaterial)
            sprite.Set("normal_material", spriteMaterial);

        // An eye slot the atlas cannot place keeps the light of the orb off, rather than cycle the
        // eyes with it
        var eyes = rig.Eyes is { } map ? EyesOnPage(map) : null;
        Parts SlotParts(string name, Parts parts) =>
            name == rig.Eyes?.Slot && eyes == null ? parts & ~Parts.Light : parts;

        // Under a sprite material a set-apart slot needs a material even when its parts are off, or
        // the sprite material cycles it
        var slots = rig.Slots
            .Select(pair => (Name: pair.Key, Parts: SlotParts(pair.Key, pair.Value & enabled)))
            .Where(slot => slot.Parts != Parts.None || spriteParts != Parts.None)
            .Where(slot => HasSlot(skeleton, slot.Name))
            .ToList();
        if (slots.Count == 0) return;

        // A slot node finds its slot through the skeleton of its sprite, thus the nodes go in once
        // that skeleton exists. The wait for it needs the tree, which a new sprite has not joined yet
        void AddSlotNodes() => sprite.RunWhenSpineReady(new MegaSprite(sprite), _ =>
        {
            foreach (var (name, parts) in slots)
            {
                if (MaterialFor(parts, name == rig.Eyes?.Slot ? eyes : null) is not { } slotMaterial) continue;
                if (ClassDB.Instantiate(SlotNodeClass).As<Node2D>() is not { } node) return;

                node.Set("slot_name", name);
                node.Set("normal_material", slotMaterial);
                sprite.AddChild(node);
            }
        });

        if (sprite.IsInsideTree()) AddSlotNodes();
        else sprite.Ready += AddSlotNodes;
    }

    // A zero radius leaves an eye out, thus no eyes at all is two zero vectors
    private static Material? MaterialFor(Parts parts, (Vector4 Left, Vector4 Right)? eyes)
    {
        var (left, right) = eyes ?? default;
        var key = (parts, left, right);
        if (Materials.TryGetValue(key, out var cached)) return cached;
        if (parts == Parts.None) return Materials[key] = new CanvasItemMaterial();

        _shader ??= ResourceLoader.Load<Shader>(ShaderPath);
        if (_shader == null)
        {
            MainFile.Logger.Error($"Could not load {ShaderPath}. The rainbow secrets stay off.");
            return null;
        }

        var material = new ShaderMaterial { Shader = _shader };
        material.SetShaderParameter("robe", parts.HasFlag(Parts.Robe));
        material.SetShaderParameter("skin", parts.HasFlag(Parts.Skin));
        material.SetShaderParameter("bag", parts.HasFlag(Parts.Bag));
        material.SetShaderParameter("eyes", parts.HasFlag(Parts.Eyes));
        material.SetShaderParameter("orb", parts.HasFlag(Parts.Orb));
        material.SetShaderParameter("orb_light", parts.HasFlag(Parts.Light));
        material.SetShaderParameter("left_eye", left);
        material.SetShaderParameter("right_eye", right);
        return Materials[key] = material;
    }

    // The eyes of the map in UV of the atlas page, or null if the atlas does not say where the image sits
    private static (Vector4 Left, Vector4 Right)? EyesOnPage(EyeMap map)
    {
        if (SpineModel.RegionUv(map.AtlasPath, map.Region) is not { } image)
        {
            MainFile.Logger.Info($"{map.AtlasPath} does not place {map.Region}. The light of the orb skips {map.Slot}.");
            return null;
        }

        Vector4 OnPage(Vector4 eye) => new(
            image.Position.X + eye.X * image.Size.X, image.Position.Y + eye.Y * image.Size.Y,
            eye.Z * image.Size.X, eye.W * image.Size.Y);
        return (OnPage(map.Left), OnPage(map.Right));
    }

    // find_slot makes a new wrapper each call, and both usings release it on this thread
    private static bool HasSlot(Resource skeleton, string name)
    {
        using var found = skeleton.Call("find_slot", name);
        using var slot = found.AsGodotObject();
        if (slot != null) return true;

        MainFile.Logger.Info($"The skeleton has no slot named {name}. The rainbow secrets skip it.");
        return false;
    }
}
