using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace TheAlchemist.TheAlchemistCode.Powers;

public class HarvestPower : TheAlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyRewards(MegaCrit.Sts2.Core.Entities.Players.Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner.Player || Amount <= 0) return false;
        for (var i = 0; i < Amount; i++)
            rewards.Add(new PotionReward(player));
        return true;
    }
}
