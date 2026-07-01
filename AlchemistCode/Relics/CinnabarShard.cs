using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

public class CinnabarShard : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<AccelerantPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, null);
    }
}
