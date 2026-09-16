using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

public class Toadstone : AlchemistRelic
{
    private const int Extra = 1;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<AntitoxinPower>() };

    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount,
        Creature? target, CardModel? cardSource) =>
        power is AntitoxinPower && giver == Owner.Creature ? Extra : 0m;

    public override Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        if (power is AntitoxinPower) Flash();
        return Task.CompletedTask;
    }
}
