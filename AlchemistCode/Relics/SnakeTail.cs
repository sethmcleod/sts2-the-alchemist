using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Relics;

/// <summary>
/// Snake Tail — a Poison-flavored Lizard Tail. Once per run, when a lethal Poison tick would reduce the
/// player to 0 HP, the death is prevented and they heal to 33% of Max HP. Because the heal runs inside the
/// damage command (via <see cref="AfterPreventingDeath"/>), the owner is alive again when Poison's own
/// "if alive, decrement" check fires right after — so the Poison stack still ticks down by 1 (e.g. 5 Poison
/// at 3 HP → survive, Poison becomes 4, HP restored to 33%).
/// </summary>
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

    // Prevent the killing blow only when it's us, the relic is unspent, and we're currently Poisoned — i.e.
    // the lethal turn-start Poison tick (Poison hasn't decremented yet at this point). Any other death, or a
    // second poison death after we've been used, resolves normally.
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
