using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace TheAlchemist.TheAlchemistCode.Relics;

public class MidasRoot : TheAlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    private decimal _goldRemainder;

    public override async Task AfterModifyingGoldGained(Player player, decimal amount)
    {
        if (player != Owner) return;
        _goldRemainder += amount;
        var heals = (int)(_goldRemainder / 15m);
        if (heals <= 0) return;
        _goldRemainder -= heals * 15m;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, heals);
    }
}
