using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

using System;

namespace Alchemist.AlchemistCode.Powers;

public class ToxinSkinPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // An enemy attack that got through. "Not me" is not "an enemy", so the dealer is checked
        if (target != Owner || result.UnblockedDamage <= 0) return;
        if (dealer is not { IsAlive: true, IsPlayer: false } attacker) return;
        if (!props.HasFlag(ValueProp.Move)) return;

        var transfer = Math.Min(Amount, Owner.GetPowerAmount<PoisonPower>());
        if (transfer <= 0) return;
        Flash();
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner, -transfer, Owner, null);
        await PowerCmd.Apply<PoisonPower>(choiceContext, attacker, transfer, Owner, null);
    }
}
