using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class PanaceaPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.FromPower<AntitoxinPower>() };

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        var prevented = AntitoxinRules.TickAbsorb(Owner);
        if (prevented <= 0) return;
        if (!AntitoxinRules.IsPoisonTick(Owner, result.UnblockedDamage + prevented, props, dealer, cardSource)) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, prevented * (int)Amount, ValueProp.Unpowered, null);
    }
}
