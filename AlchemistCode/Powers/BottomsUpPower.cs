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

public class BottomsUpPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount <= 0 || power is not PoisonPower || power.Owner != Owner) return;
        if (Owner.CombatState is not { } combat) return;
        var allies = combat.Players
            .Where(p => p != Owner.Player && p.Creature is { IsAlive: true })
            .ToList();
        if (allies.Count == 0) return;
        Flash();
        foreach (var ally in allies)
            await CreatureCmd.GainBlock(ally.Creature, (int)amount * Amount, ValueProp.Unpowered, null);
    }
}
