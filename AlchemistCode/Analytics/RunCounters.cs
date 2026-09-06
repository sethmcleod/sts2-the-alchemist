using System.Collections.Generic;
using BaseLib.Patches.Saves;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Analytics;

// Per-run counters for the analytics row, carried on the Player through BaseLib's extended save the
// same way PotionSaleCounter is, so they survive save and reload and land in the serialized run
public static class RunCounters
{
    public const string MixBursting = "mix_bursting";
    public const string MixFuming = "mix_fuming";
    public const string MixSyrupy = "mix_syrupy";
    public const string MixZesty = "mix_zesty";
    public const string MixAcrid = "mix_acrid";
    public const string MixSparkling = "mix_sparkling";
    public const string MixCompound = "mix_compound";
    public const string PoisonGained = "poison_gained";
    public const string PoisonAbsorbed = "poison_absorbed";
    public const string PoisonBled = "poison_bled";
    public const string AntitoxinPeak = "antitoxin_peak";

    // Tally keys. The prefixed ones carry a label after the colon
    public const string TickCovered = "tick_covered";
    public const string TickBled = "tick_bled";
    public const string PoisonDeath = "poison_death";
    public const string BrewOffered = "brew_offer:";
    public const string BrewPicked = "brew_pick:";
    public const string MixSource = "mixsrc:";
    public const string MixMade = "mixmade:";
    public const string MixPlayed = "mixplay:";
    public const string CompoundPair = "pair:";

    public static readonly string[] Keys =
    {
        MixBursting, MixFuming, MixSyrupy, MixZesty, MixAcrid, MixSparkling, MixCompound, PoisonGained, PoisonAbsorbed, PoisonBled,
        AntitoxinPeak,
    };

    private static readonly Dictionary<string, SpireField<Player, int>> Fields = new();

    public static void Register()
    {
        foreach (var key in Keys)
        {
            var field = new SpireField<Player, int>(() => 0);
            Fields[key] = field;
            ExtendedSaveTypes.RegisterSavedValue<Player, int>(
                MainFile.ModId + "-" + key,
                player => field[player],
                (player, count) => field[player] = count,
                (count, writer) => writer.WriteInt(count),
                reader => reader.ReadInt());
        }
        RegisterTally();
    }

    public static void Add(Player? player, string key, int amount)
    {
        // TryGetValue: if registration failed, analytics degrade to zeros instead of a
        // KeyNotFoundException inside a combat hook
        if (player == null || amount <= 0 || !Fields.TryGetValue(key, out var field)) return;
        field[player] = field[player] + amount;
    }

    // Max semantics for high-water marks such as the Antitoxin peak
    public static void RaiseTo(Player? player, string key, int value)
    {
        if (player == null || value <= 0 || !Fields.TryGetValue(key, out var field)) return;
        if (value > field[player]) field[player] = value;
    }

    public static int CountFor(SerializablePlayer player, string key) =>
        ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedData[player]
            .DictForType<int>()
            .GetValueOrDefault(MainFile.ModId + "-" + key);

    // Open-keyed counts for the things the fixed counters cannot name up front: which maker created
    // a Mix, which two kinds a compound paired, which Brew potion was offered. One saved value holds
    // the whole bag, so a new key needs no registration and an old save reads back what it has
    private const string TallyKey = MainFile.ModId + "-tally";

    private static readonly SpireField<Player, Dictionary<string, int>> TallyField = new(() => new());

    private static void RegisterTally()
    {
        // The saved value's OUTER dictionary gets its JSON type info from RegisterSavedValue; the inner
        // one is only registered by BaseLib's own CardModifier, a side effect the save must not lean on
        ExtendedSaveTypes.RegisterDictionarySaveType<string, int>();
        ExtendedSaveTypes.RegisterSavedValue<Player, Dictionary<string, int>>(
            TallyKey,
            player => TallyField[player],
            (player, bag) => TallyField[player] = bag,
            (bag, writer) =>
            {
                writer.WriteInt(bag.Count);
                foreach (var (key, count) in bag)
                {
                    writer.WriteString(key);
                    writer.WriteInt(count);
                }
            },
            reader =>
            {
                var bag = new Dictionary<string, int>();
                var count = reader.ReadInt();
                for (var i = 0; i < count; i++)
                {
                    var key = reader.ReadString();
                    bag[key] = reader.ReadInt();
                }
                return bag;
            });
    }

    public static void Tally(Player? player, string key, int amount = 1)
    {
        if (player == null || amount <= 0) return;
        var bag = TallyField[player]!;
        bag[key] = bag.GetValueOrDefault(key) + amount;
    }

    public static IReadOnlyDictionary<string, int> TallyFor(SerializablePlayer player) =>
        ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedData[player]
            .DictForType<Dictionary<string, int>>()
            .GetValueOrDefault(TallyKey) ?? new Dictionary<string, int>();

    // A model's id without the mod prefix, lower-cased: the label a tally key carries
    public static string Label(MegaCrit.Sts2.Core.Models.AbstractModel? model) =>
        model?.Id?.Entry is { } entry ? entry.RemovePrefix().ToLowerInvariant() : "unknown";
}
