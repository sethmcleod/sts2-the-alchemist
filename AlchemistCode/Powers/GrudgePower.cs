using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// PoisonPower decrements itself after each tick, so re-applying on the tick cancels that decrement
public class GrudgePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // A Poison tick is the only damage that arrives unblockable and unpowered with no dealer and no
    // card behind it
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner || !target.IsAlive) return;
        if (Owner.CombatState is not { } combat || !combat.GetOpponentsOf(Owner).Contains(target)) return;
        if (!AntitoxinRules.IsPoisonTick(target, result.UnblockedDamage, props, dealer, cardSource)) return;
        Flash();
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, Amount, Owner, null);
    }
}
