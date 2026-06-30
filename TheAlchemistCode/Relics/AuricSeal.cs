using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace TheAlchemist.TheAlchemistCode.Relics;

public class AuricSeal : TheAlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator != Owner) return Task.CompletedTask;
        if (card.IsUpgraded) return Task.CompletedTask;
        Flash();
        CardCmd.Upgrade(card);
        return Task.CompletedTask;
    }
}
