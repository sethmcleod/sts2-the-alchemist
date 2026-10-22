using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class ToxicAuraPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || CombatState == null) return;
        var tick = result.UnblockedDamage + AntitoxinRules.TickAbsorb(Owner);
        if (tick <= 0) return;
        if (!AntitoxinRules.IsPoisonTick(Owner, tick, props, dealer, cardSource)) return;
        Flash();
        foreach (var enemy in CombatState.HittableEnemies.ToList())
            await GameCompat.Damage(choiceContext, enemy, tick * (int)Amount, ValueProp.Unpowered, Owner, null);
    }
}
