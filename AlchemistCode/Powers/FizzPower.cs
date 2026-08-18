using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;

namespace Alchemist.AlchemistCode.Powers;

// Lasts until the end of the turn it was played, like the base game's turn-scoped buffs
public class FizzPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not PoisonPower || power.Owner != Owner || amount <= 0) return;
        if (Owner.CombatState is not { } combat) return;
        Flash();
        foreach (var enemy in combat.GetOpponentsOf(Owner).Where(e => e.IsAlive).ToList())
            await GameCompat.Damage(choiceContext, enemy, Amount, ValueProp.Unpowered, Owner, null, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
