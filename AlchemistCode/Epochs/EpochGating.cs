using System;
using System.Collections.Generic;
using Alchemist.AlchemistCode.Config;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace Alchemist.AlchemistCode.Epochs;

// Holds the cards, relics, and potions of each epoch back until the player reveals that epoch on the
// Timeline, the way a base-game character unlocks content. Content with no epoch is always available
public static class EpochGating
{
    // Gated content id -> "is this content's epoch revealed?". Ungated content is absent from the maps,
    // and all three build in one pass, so a single reference write publishes the finished set
    private sealed record Gates(
        Dictionary<ModelId, Func<UnlockState, bool>> Cards,
        Dictionary<ModelId, Func<UnlockState, bool>> Relics,
        Dictionary<ModelId, Func<UnlockState, bool>> Potions);

    private static Gates? _gates;

    // IsEpochRevealed is generic, so each epoch needs its own compile-time predicate
    private static readonly (Type Epoch, Func<UnlockState, bool> Revealed)[] Revealers =
    {
        (typeof(Alchemist2Epoch), us => us.IsEpochRevealed<Alchemist2Epoch>()),
        (typeof(Alchemist3Epoch), us => us.IsEpochRevealed<Alchemist3Epoch>()),
        (typeof(Alchemist4Epoch), us => us.IsEpochRevealed<Alchemist4Epoch>()),
        (typeof(Alchemist5Epoch), us => us.IsEpochRevealed<Alchemist5Epoch>()),
        (typeof(Alchemist6Epoch), us => us.IsEpochRevealed<Alchemist6Epoch>()),
        (typeof(Alchemist7Epoch), us => us.IsEpochRevealed<Alchemist7Epoch>()),
    };

    public static bool CardUnlocked(ModelId id, UnlockState unlockState) => Unlocked(Built.Cards, id, unlockState);
    public static bool RelicUnlocked(ModelId id, UnlockState unlockState) => Unlocked(Built.Relics, id, unlockState);
    public static bool PotionUnlocked(ModelId id, UnlockState unlockState) => Unlocked(Built.Potions, id, unlockState);

    private static bool Unlocked(Dictionary<ModelId, Func<UnlockState, bool>> gates, ModelId id, UnlockState unlockState)
    {
        // If the epoch system is off, this unlocks everything that the Timeline gates
        if (!AlchemistModConfig.EnableEpochs) return true;
        return !gates.TryGetValue(id, out var revealed) || revealed(unlockState);
    }

    private static Gates Built => _gates ??= Build();

    private static Gates Build()
    {
        var cards = new Dictionary<ModelId, Func<UnlockState, bool>>();
        var relics = new Dictionary<ModelId, Func<UnlockState, bool>>();
        var potions = new Dictionary<ModelId, Func<UnlockState, bool>>();
        foreach (var (type, revealed) in Revealers)
        {
            var epoch = (AlchemistEpoch)Activator.CreateInstance(type)!;
            foreach (var c in epoch.GatedCards) cards[c.Id] = revealed;
            foreach (var r in epoch.GatedRelics) relics[r.Id] = revealed;
            foreach (var p in epoch.GatedPotions) potions[p.Id] = revealed;
        }
        return new Gates(cards, relics, potions);
    }
}
