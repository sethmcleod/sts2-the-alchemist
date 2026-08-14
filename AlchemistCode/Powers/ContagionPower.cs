using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Powers;

// Detect the poison tick: unblockable + unpowered damage with no dealer or card source
public class ContagionPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        var tick = result.UnblockedDamage + AntitoxinRules.TickAbsorb(Owner);
        if (tick <= 0) return;
        if (!AntitoxinRules.IsPoisonTick(Owner, tick, props, dealer, cardSource)) return;

        Flash();
        // Capped: reading the pre-absorb tick uncaps this from your own defence, and a quadratic
        // feeding a quadratic is the strongest thing in the mod without a ceiling
        var poison = Math.Min(tick, Amount);
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), enemy, poison, Owner, null);
    }
}
