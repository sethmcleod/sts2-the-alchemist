using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

public class AquaVitae : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner) return;
        Flash();
        await CreatureCmd.GainMaxHp(Owner.Creature, 1m);
        if (Owner.Creature.CombatState != null)
            await PowerCmd.Apply<RegenPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
    }
}
