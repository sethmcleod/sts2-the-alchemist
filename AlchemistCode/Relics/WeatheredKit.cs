using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Rooms;

namespace Alchemist.AlchemistCode.Relics;

public class WeatheredKit : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner) return;
        if (potion is FoulPotion && Owner.RunState.CurrentRoom is MerchantRoom) return;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        options.Add(new BrewRestSiteOption(player));
        return true;
    }
}
