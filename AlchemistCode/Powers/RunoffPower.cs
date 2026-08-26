using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// The Mix payoff: every card the owner creates vents Poison over the field. Same hook as base
// ArsenalPower, which is the vanilla "whenever you create a card" power
public class RunoffPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator?.Creature != Owner || CombatState == null) return;
        Flash();
        await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(),
            CombatState.HittableEnemies, Amount, Owner, null);
    }
}
