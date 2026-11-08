using BaseLib.Patches.Saves;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Badges;

public static class PotionSaleCounter
{
    private const string SoldKey = MainFile.ModId + "-potions_sold";

    private static readonly SpireField<Player, int> Sold = new(() => 0);

    // Must run before the serializer builds SerializablePlayer's property list
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
