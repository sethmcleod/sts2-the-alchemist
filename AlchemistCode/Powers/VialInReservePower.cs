using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Alchemist.AlchemistCode.Powers;

// Base MachineLearningPower with the potion condition: the vial you did not drink pays for
// itself in draw. Held state rather than a per-drink trigger, because potions are too few for
// a trigger to feel alive
public class VialInReservePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner.Player) return count;
        if (!player.PotionSlots.Any(p => p != null)) return count;
        return count + Amount;
    }
}
