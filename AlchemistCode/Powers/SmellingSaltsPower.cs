using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class SmellingSaltsPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[]
        {
            HoverTipFactory.FromPower<PoisonPower>(),
            HoverTipFactory.FromPower<AntitoxinPower>(),
        };

    // Late, not AfterSideTurnStart: Poison ticks in AfterSideTurnStart, and this has to read the result
    // of that tick rather than race it
    public override async Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        if (!AntitoxinRules.AbsorbedThisTurn(Owner)) return;
        Flash();
        await PlayerCmd.GainEnergy(Amount, Owner.Player!);
    }
}
