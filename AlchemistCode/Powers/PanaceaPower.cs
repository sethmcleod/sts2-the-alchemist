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

// The dose that gets through becomes permanent capacity, so the tick teaches the body its own
// tolerance and converges on zero. Unlike Callus this reads the UNABSORBED damage only, without
// AntitoxinRules.TickAbsorb: a tick the capacity already held did no damage, so it cures nothing
public class PanaceaPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.FromPower<AntitoxinPower>() };

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        var bled = result.UnblockedDamage;
        if (bled <= 0) return;
        if (!AntitoxinRules.IsPoisonTick(Owner, bled, props, dealer, cardSource)) return;
        Flash();
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, Owner, bled, Owner, null);
    }
}
