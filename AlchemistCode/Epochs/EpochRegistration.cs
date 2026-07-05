using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Timeline;

namespace Alchemist.AlchemistCode.Epochs;

/// <summary>
/// Injects the Alchemist's epochs + story into the base game's private static registries at mod load
/// (they were populated once by a static ctor before we loaded). Improves on TheSorceress by caching the
/// FieldInfo and throwing loudly if the base game renames a field, instead of silently disabling epochs.
/// </summary>
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

    private static FieldInfo Require(Type type, string name) =>
        type.GetField(name, StaticNonPublic)
        ?? throw new InvalidOperationException(
            $"[Alchemist] Epoch registration: {type.Name}.{name} not found — base game changed; epochs disabled.");

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
            if (epochById.ContainsKey(epoch.Id)) continue; // idempotent across reloads
            epochById[epoch.Id] = type;
            idByType[type] = epoch.Id;
            allEpochs.Add(type);
        }
        AllEpochIdsCache.SetValue(null, null); // bust the lazy cache; AllEpochIds rebuilds from _allEpochs

        var storyById = (Dictionary<string, Type>)StoryById.GetValue(null)!;
        storyById[AlchemistStory.StoryKey] = typeof(AlchemistStory);

        MainFile.Logger.Info($"[Epochs] Registered {AlchemistEpochTypes.Length} epochs + story '{AlchemistStory.StoryKey}'.");
    }

    /// <summary>Ids of our epochs that gate the given content kind — the GetXUnlockEpochIds patches call
    /// this, so there is no separate hardcoded id list to keep in sync.</summary>
    public static IEnumerable<string> GatingEpochIds(EpochUnlockKind kind) =>
        AlchemistEpochTypes
            .Select(t => (AlchemistEpoch)Activator.CreateInstance(t)!)
            .Where(e => e.UnlockKind == kind)
            .Select(e => e.Id);

    // ── Dynamic timeline placement (collision-free) ───────────────────────────────────────────────
    // Hardcoding Era/EraPosition collided with other mods (TheSorceress pins Invitation0/5 at pos 4 — the
    // cells we used to take). Instead we scan the cells already occupied by every OTHER registered epoch
    // (base game + mods) and hand each of our epochs a free one. Computed lazily on first Era access — by
    // then all mods have registered their epochs into _allEpochs — and cached for the session.

    private static readonly EpochEra[] PreferredEras =
    {
        EpochEra.Invitation2, EpochEra.Invitation3, EpochEra.Invitation4,
        EpochEra.Invitation5, EpochEra.Invitation6, EpochEra.Invitation7,
    };
    private const int TopRow = 4; // rows 0 (bottom) .. 4 (top)
    private static Dictionary<Type, (EpochEra era, int pos)> _slots;

    /// <summary>The (era, position) assigned to one of our epoch types. Assigns all of them on first call.</summary>
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
            if (typeof(AlchemistEpoch).IsAssignableFrom(type)) continue; // skip ours (would recurse into SlotFor)
            try
            {
                var e = (EpochModel)Activator.CreateInstance(type)!;
                occupied.Add((e.Era, e.EraPosition));
            }
            catch { /* an epoch we can't instantiate — just don't reserve its cell */ }
        }

        var slots = new Dictionary<Type, (EpochEra, int)>();
        foreach (var type in AlchemistEpochTypes) // chapter order; fills top row first, left-to-right
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
        return (EpochEra.Invitation7, 0); // ultra-fallback (30 cells for 7 epochs — never expected)
    }
}
