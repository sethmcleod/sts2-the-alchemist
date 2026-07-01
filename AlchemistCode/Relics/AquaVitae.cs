using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Relics;

public class AquaVitae : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner) return;
        Flash();
        await CreatureCmd.GainMaxHp(Owner.Creature, 1m);
    }
}
