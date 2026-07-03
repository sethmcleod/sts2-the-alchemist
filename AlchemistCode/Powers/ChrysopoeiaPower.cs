using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class ChrysopoeiaPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        // Whenever you lose HP (any source — poison, attacks, self-damage), gain Gold.
        if (creature != Owner || delta >= 0) return;
        Flash();
        await PlayerCmd.GainGold(Amount, Owner.Player!);
    }
}
