using Alchemist.AlchemistCode.Config;
using Godot;

namespace Alchemist.AlchemistCode.Character;

internal static class BigHead
{
    private const string MeshClass = "SpineMesh2D";

    private static readonly Dictionary<(ulong Skeleton, string Bone), Vector2> SetupScales = [];

    public static bool Enabled => AlchemistModConfig.BigHeadMode && SecretUnlocks.IsUnlocked;

    // Must run before each sprite exists: a sprite reads the setup pose when it's made
    public static void Apply(Resource skeleton, IEnumerable<string> bones)
    {
        var enabled = Enabled;
        var scale = (float)AlchemistModConfig.BigHeadSize;

        foreach (var name in bones)
        {
            var key = (skeleton.GetInstanceId(), name);
            var scaledBefore = SetupScales.TryGetValue(key, out var setup);
            if (!enabled && !scaledBefore) continue;

            // Spine binding calls return a new wrapper each time, so dispose it here
            using var found = skeleton.Call("find_bone", name);
            using var bone = found.AsGodotObject();
            if (bone == null)
            {
                MainFile.Logger.Info($"The skeleton has no bone named {name}. Big Head Mode skips it.");
                continue;
            }

            if (!scaledBefore)
            {
                setup = new Vector2(bone.Call("get_scale_x").AsSingle(), bone.Call("get_scale_y").AsSingle());
                SetupScales[key] = setup;
            }

            var factor = enabled ? scale : 1f;
            bone.Call("set_scale_x", setup.X * factor);
            bone.Call("set_scale_y", setup.Y * factor);
        }
    }

    // The binding can't set draw order, but moving a SpineMesh2D child moves its place in it
    public static void DrawUnderHead(Node2D sprite, Resource skeleton, string headSlot, IEnumerable<string> overlays)
    {
        var slots = SlotNames(skeleton);
        var meshes = sprite.GetChildren().Where(child => child.GetClass() == MeshClass).ToList();
        var head = slots.IndexOf(headSlot);
        if (head < 0 || meshes.Count != slots.Count)
        {
            MainFile.Logger.Info($"The sprite does not match its skeleton, thus {headSlot} keeps its place in the draw order.");
            return;
        }

        foreach (var overlay in overlays)
        {
            var index = slots.IndexOf(overlay);
            if (index > head) sprite.MoveChild(meshes[index], meshes[head].GetIndex());
        }
    }

    private static List<string> SlotNames(Resource skeleton)
    {
        using var found = skeleton.Call("get_slots");
        using var slots = found.AsGodotArray();
        var names = new List<string>(slots.Count);
        for (var i = 0; i < slots.Count; i++)
        {
            using var entry = slots[i];
            using var slot = entry.AsGodotObject();
            names.Add(slot?.Call("get_name").AsString() ?? "");
        }

        return names;
    }
}
