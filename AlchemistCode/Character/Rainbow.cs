using Alchemist.AlchemistCode.Config;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace Alchemist.AlchemistCode.Character;

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
        Light = 32
    }

    public sealed record Rig(Parts SpriteParts, IReadOnlyDictionary<string, Parts> Slots, EyeMap? Eyes = null);

    // Each eye is an ellipse: centre in xy, radii in zw, as fractions of the image
    public sealed record EyeMap(string Slot, string AtlasPath, string Region, Vector4 Left, Vector4 Right);

    public static IReadOnlyDictionary<string, Parts> Slots(params (Parts Parts, string[] Names)[] groups) =>
        groups.SelectMany(group => group.Names.Select(name => (name, group.Parts)))
            .ToDictionary(slot => slot.name, slot => slot.Parts);

    private const string ShaderPath = $"{MainFile.ResPath}/shaders/rainbow.gdshader";
    private const string SlotNodeClass = "SpineSlotNode";

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

        var eyes = rig.Eyes is { } map ? EyesOnPage(map) : null;
        Parts SlotParts(string name, Parts parts) =>
            name == rig.Eyes?.Slot && eyes == null ? parts & ~Parts.Light : parts;

        // A slot needs its own material even with its parts off, or the sprite material cycles it
        var slots = rig.Slots
            .Select(pair => (Name: pair.Key, Parts: SlotParts(pair.Key, pair.Value & enabled)))
            .Where(slot => slot.Parts != Parts.None || spriteParts != Parts.None)
            .Where(slot => HasSlot(skeleton, slot.Name))
            .ToList();
        if (slots.Count == 0) return;

        // A slot node finds its slot through the sprite's skeleton, so it can only go in once that exists
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

    private static bool HasSlot(Resource skeleton, string name)
    {
        using var found = skeleton.Call("find_slot", name);
        using var slot = found.AsGodotObject();
        if (slot != null) return true;

        MainFile.Logger.Info($"The skeleton has no slot named {name}. The rainbow secrets skip it.");
        return false;
    }
}
