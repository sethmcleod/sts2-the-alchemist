using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
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

    // The stack is the Energy; the threshold is a second Salts' upgrade at most, so the lowest
    // applied one stands. No smartDescription key for this power: the smart path cannot take an
    // extra argument, and the plain Description can
    private const int DefaultThreshold = 3;
    private int _threshold = DefaultThreshold;

    internal void LowerThreshold(int threshold) => _threshold = Math.Min(_threshold, threshold);

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add("Threshold", _threshold);
            return description;
        }
    }

    // Late, not AfterSideTurnStart: Poison ticks in AfterSideTurnStart, and the threshold has to
    // read the stack after the tick, not race it
    public override async Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        if (Owner.GetPowerAmount<PoisonPower>() < _threshold) return;
        Flash();
        await PlayerCmd.GainEnergy(Amount, Owner.Player!);
    }
}
