using Alchemist.AlchemistCode.Extensions;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Badges;

public sealed class PotionSale() : CustomBadge(requiresWin: false, multiplayerOnly: false)
{
    private const int BronzeSales = 3;
    private const int SilverSales = 6;
    private const int GoldSales = 9;

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
