using BaseLib.Patches.Saves;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Badges;

// A sale leaves no trace a badge can read. The sell path removes the potion without going through
// PotionCmd.Discard, so nothing lands in the run history, and gold gained inside a shop also comes from
// relics and Foul Potion. A badge only ever sees the serialized run, so the count rides on the player
// through BaseLib's extended save, which copies it in a Player.ToSerializable postfix
public static class PotionSaleCounter
{
    private const string SoldKey = MainFile.ModId + "-potions_sold";

    private static readonly SpireField<Player, int> Sold = new(() => 0);

    // Must run before the serializer builds its property list for SerializablePlayer, which MainFile's
    // mod initializer is early enough for
    public static void Register()
    {
        ExtendedSaveTypes.RegisterSavedValue<Player, int>(
            SoldKey,
            player => Sold[player],
            (player, count) => Sold[player] = count,
            (count, writer) => writer.WriteInt(count),
            reader => reader.ReadInt());
    }

    public static void RecordSale(Player player)
    {
        Sold[player] = Sold[player] + 1;
    }

    public static int CountFor(SerializablePlayer player)
    {
        return ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedData[player]
            .DictForType<int>()
            .GetValueOrDefault(SoldKey);
    }
}
