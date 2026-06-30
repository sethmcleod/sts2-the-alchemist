using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;

namespace TheAlchemist.TheAlchemistCode.Relics;

public class TarnishedFlask : TheAlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner) return;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, 4m);
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        options.Add(new BrewRestSiteOption(player));
        return true;
    }
}
