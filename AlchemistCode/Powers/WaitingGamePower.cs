using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class WaitingGamePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.FromPower<StrengthPower>() };

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!target.IsAlive || Owner.CombatState is not { } combat) return;
        if (!combat.GetOpponentsOf(Owner).Contains(target)) return;
        if (!AntitoxinRules.IsPoisonTick(target, result.UnblockedDamage, props, dealer, cardSource)) return;
        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, target, -Amount, Owner, null);
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
