using System.Collections.Generic;
using BaseLib.Patches.Saves;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Analytics;

public static class RunCounters
{
    public const string MixPrefix = "mix_";
    public const string MixBursting = MixPrefix + "bursting";
    public const string MixFuming = MixPrefix + "fuming";
    public const string MixSyrupy = MixPrefix + "syrupy";
    public const string MixZesty = MixPrefix + "zesty";
    public const string MixAcrid = MixPrefix + "acrid";
    public const string MixSparkling = MixPrefix + "sparkling";
    public const string MixCompound = MixPrefix + "compound";
    public const string PoisonGained = "poison_gained";
    public const string PoisonAbsorbed = "poison_absorbed";
    public const string PoisonBled = "poison_bled";
    public const string PoisonPeak = "poison_peak";
    public const string AntitoxinPeak = "antitoxin_peak";

    public const string TickCovered = "tick_covered";
    public const string TickBled = "tick_bled";
    public const string PoisonDeath = "poison_death";
    public const string PoisonDealt = "poison_dealt";
    public const string AntitoxinSource = "atxsrc:";
    public const string AntitoxinDecayed = "atx_decayed";
    public const string FermentPlays = "ferment_plays";
    public const string FermentTurns = "ferment_turns";
    public const string FermentZero = "ferment_zero";
    public const string FermentCardPlays = "fermentplay:";
    public const string FermentCardTurns = "fermentturns:";
    public const string BrewOffered = "brew_offer:";
    public const string BrewPicked = "brew_pick:";
    public const string MixSource = "mixsrc:";
    public const string MixMade = "mixmade:";
    public const string MixPlayed = "mixplay:";
    public const string MixCombined = "mixlost:combined";
    public const string MixLeftover = "mixlost:leftover";
    public const string MixesPerFight = "mixfight:";
    public const string CompoundPair = "pair:";
    public const string CardPlayed = "play:";

    public static readonly string[] Keys =
    {
        MixBursting, MixFuming, MixSyrupy, MixZesty, MixAcrid, MixSparkling, MixCompound, PoisonGained, PoisonAbsorbed, PoisonBled,
        PoisonPeak, AntitoxinPeak,
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
        // TryGetValue: a failed registration must not throw inside a combat hook
        if (player == null || amount <= 0 || !Fields.TryGetValue(key, out var field)) return;
        field[player] = field[player] + amount;
    }

    public static void RaiseTo(Player? player, string key, int value)
    {
        if (player == null || value <= 0 || !Fields.TryGetValue(key, out var field)) return;
        if (value > field[player]) field[player] = value;
    }

    public static int CountFor(SerializablePlayer player, string key) =>
        ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedData[player]
            .DictForType<int>()
            .GetValueOrDefault(MainFile.ModId + "-" + key);

    private const string TallyKey = MainFile.ModId + "-tally";

    private static readonly SpireField<Player, Dictionary<string, int>> TallyField = new(() => new());

    private static void RegisterTally()
    {
        // The inner dictionary is otherwise registered only as a side effect of BaseLib's CardModifier
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

    public static string Label(MegaCrit.Sts2.Core.Models.AbstractModel? model) =>
        model?.Id?.Entry is { } entry ? entry.RemovePrefix().ToLowerInvariant() : "unknown";
}
