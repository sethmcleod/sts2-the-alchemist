using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Relics;

// The heal runs inside the damage command, so the owner is alive again when poison's own
// "if alive, decrement" check fires right after, and the poison stack still ticks down by 1
public class SnakeTail : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

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

    // ShouldDieLate is not told what dealt the damage, so the incoming hit is classified here first.
    // Holding Poison is not enough on its own: the relic only answers for a Poison death
    private bool _poisonTickInFlight;

    public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner.Creature)
            _poisonTickInFlight =
                AntitoxinRules.IsPoisonTick(target, amount, props, dealer, cardSource);
        return Task.CompletedTask;
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner.Creature || _used) return true;
        return !_poisonTickInFlight;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        Used = true;
        await CreatureCmd.Heal(creature, Math.Max(1m, (decimal)creature.MaxHp * 33m / 100m));
    }
}
