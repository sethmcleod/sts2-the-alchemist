using Alchemist.AlchemistCode.Config;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace Alchemist.AlchemistCode.Character;

/// <summary>
/// Rainbow Robes. A shader cycles the hue of the robe purple and leaves every other color alone.
/// </summary>
internal static class RainbowRobes
{
    private const string ShaderPath = $"{MainFile.ResPath}/shaders/rainbow_robe.gdshader";
    private const string SlotNodeClass = "SpineSlotNode";

    // One material serves every sprite. The cycle runs on TIME, thus all robes stay in step
    private static ShaderMaterial? _material;

    public static bool Enabled => AlchemistModConfig.RainbowRobes && SecretUnlocks.IsUnlocked;

    /// <summary>Puts the material on every slot of the sprite.</summary>
    public static void Apply(Node2D sprite)
    {
        if (Enabled && Material() is { } material) sprite.Set("normal_material", material);
    }

    /// <summary>
    /// Puts the material on the named slots only, for a sprite whose other slots hold purple scenery.
    /// </summary>
    /// <remarks>
    /// A SpineSlotNode child overrides the material of one slot. It finds that slot through the
    /// skeleton of its SpineSprite, thus the nodes go in once the skeleton exists.
    /// </remarks>
    public static void ApplyToSlots(Node2D sprite, Resource skeleton, IEnumerable<string> slots)
    {
        if (!Enabled || Material() is not { } material) return;

        var present = slots.Where(slot => HasSlot(skeleton, slot)).ToList();
        sprite.RunWhenSpineReady(new MegaSprite(sprite), _ =>
        {
            foreach (var slot in present)
            {
                if (ClassDB.Instantiate(SlotNodeClass).As<Node2D>() is not { } node) return;

                node.Set("slot_name", slot);
                node.Set("normal_material", material);
                sprite.AddChild(node);
            }
        });
    }

    private static ShaderMaterial? Material()
    {
        if (_material != null) return _material;

        if (ResourceLoader.Load<Shader>(ShaderPath) is not { } shader)
        {
            MainFile.Logger.Error($"Could not load {ShaderPath}. Rainbow Robes stays off.");
            return null;
        }

        return _material = new ShaderMaterial { Shader = shader };
    }

    // find_slot makes a new wrapper each call, and both usings release it on this thread
    private static bool HasSlot(Resource skeleton, string name)
    {
        using var found = skeleton.Call("find_slot", name);
        using var slot = found.AsGodotObject();
        if (slot != null) return true;

        MainFile.Logger.Info($"The skeleton has no slot named {name}. Rainbow Robes skips it.");
        return false;
    }
}
