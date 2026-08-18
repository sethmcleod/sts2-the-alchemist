using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class MiasmaPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // Only doses that land on you, and only positive ones. The Poison it applies to enemies has a
    // different Owner, so it cannot feed itself
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not PoisonPower || power.Owner != Owner || amount <= 0) return;
        if (Owner.CombatState is not { } combat) return;
        Flash();
        foreach (var enemy in combat.GetOpponentsOf(Owner).Where(e => e.IsAlive).ToList())
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, (int)amount, Owner, null);
    }
}
