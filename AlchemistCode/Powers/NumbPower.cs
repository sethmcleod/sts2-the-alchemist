using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// The skip itself lives in Patches.NumbPatches, which stops PoisonPower.Trigger while this power is present
public class NumbPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // Late: CombatManager fires AfterPlayerTurnStart BEFORE AfterSideTurnStart, where Poison
    // triggers, so removal on the early hook would land one hook before the tick this skips
    public override async Task AfterSideTurnStartLate(CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        if (Amount > 1) await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -1, Owner, null);
        else await PowerCmd.Remove(this);
    }
}
