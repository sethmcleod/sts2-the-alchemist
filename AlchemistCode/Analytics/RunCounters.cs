using System.Collections.Generic;
using BaseLib.Patches.Saves;
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
    public const string PoisonGained = "poison_gained";
    public const string PoisonAbsorbed = "poison_absorbed";
    public const string PoisonBled = "poison_bled";

    public static readonly string[] Keys =
        { MixBursting, MixFuming, MixSyrupy, MixZesty, PoisonGained, PoisonAbsorbed, PoisonBled };

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
    }

    public static void Add(Player? player, string key, int amount)
    {
        // TryGetValue: if registration failed, analytics degrade to zeros instead of a
        // KeyNotFoundException inside a combat hook
        if (player == null || amount <= 0 || !Fields.TryGetValue(key, out var field)) return;
        field[player] = field[player] + amount;
    }

    public static int CountFor(SerializablePlayer player, string key) =>
        ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedData[player]
            .DictForType<int>()
            .GetValueOrDefault(MainFile.ModId + "-" + key);
}
