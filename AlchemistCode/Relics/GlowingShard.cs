using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

// Poison decay is the -1 that PoisonPower.Trigger applies to itself after each tick: always
// exactly -1 with no applier. Zeroing that offset is the whole relic. Only enemy Poison is
// guarded; the player's own Poison keeps decaying, which Tolerance depends on
public class GlowingShard : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target,
        decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not PoisonPower || amount != -1m || applier != null) return false;
        if (Owner.Creature?.CombatState is not { } combat) return false;
        if (!combat.GetOpponentsOf(Owner.Creature).Contains(target)) return false;
        modifiedAmount = 0m;
        return true;
    }

    public override Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        Flash();
        return Task.CompletedTask;
    }
}
