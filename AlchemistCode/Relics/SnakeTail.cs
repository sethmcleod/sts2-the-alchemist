using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Relics;

// The heal runs inside the damage command, so the owner is alive again when poison's own
// "if alive, decrement" check fires right after, and the poison stack still ticks down by 1
public class SnakeTail : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    private bool _used;
    public override bool IsUsedUp => _used;

    [SavedProperty]
    public bool Used
    {
        get => _used;
        set
        {
            AssertMutable();
            _used = value;
            if (_used) Status = RelicStatus.Disabled;
        }
    }

    // Only the lethal turn-start poison tick (poison hasn't decremented yet); other deaths resolve normally
    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner.Creature || _used) return true;
        return creature.GetPowerAmount<PoisonPower>() <= 0;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        Used = true;
        await CreatureCmd.Heal(creature, Math.Max(1m, (decimal)creature.MaxHp * 33m / 100m));
    }
}
