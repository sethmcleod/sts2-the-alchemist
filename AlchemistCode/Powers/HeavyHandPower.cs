
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Powers;

public class HeavyHandPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount,
        Creature? target, CardModel? cardSource)
    {
        if (power is PoisonPower && giver == Owner && target != null && amount > 0)
            return Amount;
        return 0m;
    }
}
