using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Timeline;

namespace Alchemist.AlchemistCode.Epochs;

// Injects our epochs + story into the base game's private static registries at mod load
public static class EpochRegistration
{
    public static readonly Type[] AlchemistEpochTypes =
    {
        typeof(Alchemist1Epoch), typeof(Alchemist2Epoch), typeof(Alchemist3Epoch),
        typeof(Alchemist4Epoch), typeof(Alchemist5Epoch), typeof(Alchemist6Epoch),
        typeof(Alchemist7Epoch),
    };

    private const BindingFlags StaticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;

    private static readonly FieldInfo EpochById = Require(typeof(EpochModel), "_epochTypeDictionary");
    private static readonly FieldInfo IdByType = Require(typeof(EpochModel), "_typeToIdDictionary");
    private static readonly FieldInfo AllEpochs = Require(typeof(EpochModel), "_allEpochs");
    private static readonly FieldInfo AllEpochIdsCache = Require(typeof(EpochModel), "_allEpochIds");
    private static readonly FieldInfo StoryById = Require(typeof(StoryModel), "_storyTypeDictionary");

    // Cached FieldInfo; throws loudly if a game update renames a field, rather than silently no-op'ing
    private static FieldInfo Require(Type type, string name) =>
        type.GetField(name, StaticNonPublic)
        ?? throw new InvalidOperationException(
            $"[Alchemist] Epoch registration: {type.Name}.{name} not found; base game changed, epochs disabled.");

    private static bool _registered;

    public static void RegisterEpochs()
    {
        if (_registered) return;
        _registered = true;

        var epochById = (Dictionary<string, Type>)EpochById.GetValue(null)!;
        var idByType = (Dictionary<Type, string>)IdByType.GetValue(null)!;
        var allEpochs = (List<Type>)AllEpochs.GetValue(null)!;

        foreach (var type in AlchemistEpochTypes)
        {
            var epoch = (EpochModel)Activator.CreateInstance(type)!;
            if (epochById.ContainsKey(epoch.Id)) continue;
            epochById[epoch.Id] = type;
            idByType[type] = epoch.Id;
            allEpochs.Add(type);
        }
        AllEpochIdsCache.SetValue(null, null); // Bust the lazy cache so AllEpochIds rebuilds from _allEpochs

        var storyById = (Dictionary<string, Type>)StoryById.GetValue(null)!;
        storyById[AlchemistStory.StoryKey] = typeof(AlchemistStory);

        MainFile.Logger.Info($"[Epochs] Registered {AlchemistEpochTypes.Length} epochs + story '{AlchemistStory.StoryKey}'.");
    }

    public static IEnumerable<string> GatingEpochIds(EpochUnlockKind kind) =>
        AlchemistEpochTypes
            .Select(t => (AlchemistEpoch)Activator.CreateInstance(t)!)
            .Where(e => e.UnlockKind == kind)
            .Select(e => e.Id);

    // Collision-free placement: scan the cells occupied by every other registered epoch and take free ones.
    // Done lazily so all mods have registered into _allEpochs first; cached for the session
    private static readonly EpochEra[] PreferredEras =
    {
        EpochEra.Invitation2, EpochEra.Invitation3, EpochEra.Invitation4,
        EpochEra.Invitation5, EpochEra.Invitation6, EpochEra.Invitation7,
    };
    private const int TopRow = 4; // Rows 0 (bottom) .. 4 (top)
    private static Dictionary<Type, (EpochEra era, int pos)> _slots;

    public static (EpochEra era, int pos) SlotFor(Type epochType)
    {
        _slots ??= AssignSlots();
        return _slots.TryGetValue(epochType, out var s) ? s : (EpochEra.Invitation7, 0);
    }

    private static Dictionary<Type, (EpochEra, int)> AssignSlots()
    {
        var occupied = new HashSet<(EpochEra, int)>();
        foreach (var type in (List<Type>)AllEpochs.GetValue(null)!)
        {
            if (typeof(AlchemistEpoch).IsAssignableFrom(type)) continue; // Skip ours (would recurse into SlotFor)
            try
            {
                var e = (EpochModel)Activator.CreateInstance(type)!;
                occupied.Add((e.Era, e.EraPosition));
            }
            catch { }
        }

        var slots = new Dictionary<Type, (EpochEra, int)>();
        foreach (var type in AlchemistEpochTypes)
        {
            var cell = FindFreeCell(occupied);
            slots[type] = cell;
            occupied.Add(cell);
        }
        return slots;
    }

    private static (EpochEra, int) FindFreeCell(HashSet<(EpochEra, int)> occupied)
    {
        for (var pos = TopRow; pos >= 0; pos--)
            foreach (var era in PreferredEras)
                if (!occupied.Contains((era, pos)))
                    return (era, pos);
        return (EpochEra.Invitation7, 0);
    }
}
