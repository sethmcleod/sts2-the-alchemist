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

    private static readonly FieldInfo? EpochById = Find(typeof(EpochModel), "_epochTypeDictionary");
    private static readonly FieldInfo? IdByType = Find(typeof(EpochModel), "_typeToIdDictionary");
    private static readonly FieldInfo? AllEpochs = Find(typeof(EpochModel), "_allEpochs");
    private static readonly FieldInfo? AllEpochIdsCache = Find(typeof(EpochModel), "_allEpochIds");
    private static readonly FieldInfo? StoryById = Find(typeof(StoryModel), "_storyTypeDictionary");

    // Nullable on purpose. This used to throw when a field was missing, but it ran inside a static
    // field initializer, so the throw became a TypeInitializationException that POISONED the whole
    // class: every later touch of AlchemistEpochTypes or SlotFor rethrew it, from the config screen
    // to every epoch's Era getter. One absent field took the mod down with it. Now the lookup simply
    // reports that the registry is missing, and the Timeline feature turns itself off.
    private static FieldInfo? Find(Type type, string name) => type.GetField(name, StaticNonPublic);

    /// <summary>
    /// Whether this build of the game lets a mod add epochs at all. The game's default branch
    /// builds its epoch list from a hardcoded array and has no <c>_allEpochs</c> to append to, so
    /// custom epochs cannot work there; the public-beta branch has the mutable registry.
    /// </summary>
    public static bool Supported =>
        EpochById != null && IdByType != null && AllEpochs != null && AllEpochIdsCache != null
        && StoryById != null;

    private static bool _registered;

    public static void RegisterEpochs()
    {
        if (_registered) return;
        _registered = true;

        if (!Supported)
        {
            MainFile.Logger.Warn(
                "[Epochs] This build of the game has no mod-writable epoch registry, so the Timeline "
                + "feature is off. Every card, relic and potion is available instead of Timeline-gated. "
                + "The rest of the mod is unaffected.");
            return;
        }

        var epochById = (Dictionary<string, Type>)EpochById!.GetValue(null)!;
        var idByType = (Dictionary<Type, string>)IdByType!.GetValue(null)!;
        var allEpochs = (List<Type>)AllEpochs!.GetValue(null)!;

        foreach (var type in AlchemistEpochTypes)
        {
            var epoch = (EpochModel)Activator.CreateInstance(type)!;
            if (epochById.ContainsKey(epoch.Id)) continue;
            epochById[epoch.Id] = type;
            idByType[type] = epoch.Id;
            allEpochs.Add(type);
        }
        AllEpochIdsCache!.SetValue(null, null); // Bust the lazy cache so AllEpochIds rebuilds from _allEpochs

        var storyById = (Dictionary<string, Type>)StoryById!.GetValue(null)!;
        storyById[AlchemistStory.StoryKey] = typeof(AlchemistStory);

        MainFile.Logger.Info($"[Epochs] Registered {AlchemistEpochTypes.Length} epochs + story '{AlchemistStory.StoryKey}'.");
    }

    public static IEnumerable<string> GatingEpochIds(EpochUnlockKind kind) =>
        AlchemistEpochTypes
            .Select(t => (AlchemistEpoch)Activator.CreateInstance(t)!)
            .Where(e => e.UnlockKind == kind)
            .Select(e => e.Id);

    // Placement scans the cells every other registered epoch occupies and takes free ones. Done lazily so
    // that all mods have registered into _allEpochs first, then cached for the session
    private static readonly EpochEra[] PreferredEras =
    {
        EpochEra.Invitation2, EpochEra.Invitation3, EpochEra.Invitation4,
        EpochEra.Invitation5, EpochEra.Invitation6, EpochEra.Invitation7,
    };
    private const int TopRow = 4; // Rows 0 (bottom) .. 4 (top)
    private static Dictionary<Type, (EpochEra era, int pos)>? _slots;

    public static (EpochEra era, int pos) SlotFor(Type epochType)
    {
        // Without the registry there is no timeline to place anything on, and this getter is reached
        // from every epoch's Era property, so it must never throw
        if (!Supported) return (EpochEra.Invitation7, 0);
        var slots = _slots ??= AssignSlots();
        return slots.TryGetValue(epochType, out var s) ? s : (EpochEra.Invitation7, 0);
    }

    private static Dictionary<Type, (EpochEra, int)> AssignSlots()
    {
        var occupied = new HashSet<(EpochEra, int)>();
        foreach (var type in (List<Type>)AllEpochs!.GetValue(null)!)
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
