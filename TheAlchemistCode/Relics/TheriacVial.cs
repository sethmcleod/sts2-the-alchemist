using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace TheAlchemist.TheAlchemistCode.Relics;

public class TheriacVial : TheAlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (Owner.Creature.IsDead) return;
        var poison = Owner.Creature.GetPowerAmount<PoisonPower>();
        if (poison <= 0) return;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, poison);
    }
}
