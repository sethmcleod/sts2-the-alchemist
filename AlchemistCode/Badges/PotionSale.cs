using Alchemist.AlchemistCode.Extensions;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Badges;

public sealed class PotionSale() : CustomBadge(requiresWin: false, multiplayerOnly: false)
{
    // One sellable potion per Brew at most, so the bar sits lower than the old 3/6/9
    private const int BronzeSales = 2;
    private const int SilverSales = 4;
    private const int GoldSales = 6;

    public override string CustomBadgeIconPath => "badge_potion_sale.png".BadgeImagePath();

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player)
    {
        var sold = PotionSaleCounter.CountFor(player);
        if (sold >= GoldSales) return BadgeRarity.Gold;
        if (sold >= SilverSales) return BadgeRarity.Silver;
        if (sold >= BronzeSales) return BadgeRarity.Bronze;
        return BadgeRarity.None;
    }

    public override bool IsObtained(SerializableRun run, SerializablePlayer player)
    {
        return Rarity(run, player) != BadgeRarity.None;
    }
}
