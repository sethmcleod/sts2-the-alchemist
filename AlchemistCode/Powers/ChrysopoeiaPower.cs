using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace Alchemist.AlchemistCode.Powers;

// Royalties is the base shape: the gold arrives as an extra combat reward, and the powers are still
// live when AfterCombatEnd runs, so the Poison count is the one the fight ended on
public class ChrysopoeiaPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override Task AfterCombatEnd(CombatRoom room)
    {
        var poison = Owner.GetPowerAmount<PoisonPower>();
        if (poison > 0 && Owner.Player != null)
            room.AddExtraReward(Owner.Player, new GoldReward(poison * Amount, Owner.Player));
        return Task.CompletedTask;
    }
}
