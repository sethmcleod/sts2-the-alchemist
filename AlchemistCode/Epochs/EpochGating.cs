using System;
using System.Collections.Generic;
using Alchemist.AlchemistCode.Config;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace Alchemist.AlchemistCode.Epochs;

// Gates each epoch's declared cards/relics/potions behind that epoch being revealed on the Timeline,
// mirroring how base-game characters unlock content. Content not tied to an epoch is always available,
// and when the epoch system is disabled in mod config nothing is gated at all.
public static class EpochGating
{
    // Gated content id -> "is this content's epoch revealed?". Ungated content is absent from the map.
    private static Dictionary<ModelId, Func<UnlockState, bool>> _cardGates;
    private static Dictionary<ModelId, Func<UnlockState, bool>> _relicGates;
    private static Dictionary<ModelId, Func<UnlockState, bool>> _potionGates;

    // One reveal predicate per content epoch. Compile-time generics work for our registered custom epochs.
    private static readonly (Type Epoch, Func<UnlockState, bool> Revealed)[] Revealers =
    {
        (typeof(Alchemist2Epoch), us => us.IsEpochRevealed<Alchemist2Epoch>()),
        (typeof(Alchemist3Epoch), us => us.IsEpochRevealed<Alchemist3Epoch>()),
        (typeof(Alchemist4Epoch), us => us.IsEpochRevealed<Alchemist4Epoch>()),
        (typeof(Alchemist5Epoch), us => us.IsEpochRevealed<Alchemist5Epoch>()),
        (typeof(Alchemist6Epoch), us => us.IsEpochRevealed<Alchemist6Epoch>()),
        (typeof(Alchemist7Epoch), us => us.IsEpochRevealed<Alchemist7Epoch>()),
    };

    public static bool CardUnlocked(ModelId id, UnlockState unlockState) => Unlocked(Cards, id, unlockState);
    public static bool RelicUnlocked(ModelId id, UnlockState unlockState) => Unlocked(Relics, id, unlockState);
    public static bool PotionUnlocked(ModelId id, UnlockState unlockState) => Unlocked(Potions, id, unlockState);

    private static bool Unlocked(Dictionary<ModelId, Func<UnlockState, bool>> gates, ModelId id, UnlockState unlockState)
    {
        // Disabling the epoch system unlocks everything the Timeline would normally gate
        if (!AlchemistModConfig.EnableEpochs) return true;
        return !gates.TryGetValue(id, out var revealed) || revealed(unlockState);
    }

    private static Dictionary<ModelId, Func<UnlockState, bool>> Cards { get { Build(); return _cardGates; } }
    private static Dictionary<ModelId, Func<UnlockState, bool>> Relics { get { Build(); return _relicGates; } }
    private static Dictionary<ModelId, Func<UnlockState, bool>> Potions { get { Build(); return _potionGates; } }

    private static void Build()
    {
        if (_cardGates != null) return;
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
        _relicGates = relics;
        _potionGates = potions;
        _cardGates = cards; // set last: doubles as the built flag
    }
}
