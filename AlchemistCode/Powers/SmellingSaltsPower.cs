using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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
    // applied one stands. A DynamicVar rather than a field, so the smart hover path (which only adds
    // Amount and the DynamicVars) can print it, and the analyzer's required smartDescription key works
    private const int DefaultThreshold = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Threshold", DefaultThreshold) };

    private int Threshold => DynamicVars["Threshold"].IntValue;

    internal void LowerThreshold(int threshold) =>
        DynamicVars["Threshold"].BaseValue = Math.Min(Threshold, threshold);

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            DynamicVars.AddTo(description);
            return description;
        }
    }

    // Late, not AfterSideTurnStart: Poison ticks in AfterSideTurnStart, and the threshold has to
    // read the stack after the tick, not race it
    public override async Task AfterSideTurnStartLate(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        if (Owner.GetPowerAmount<PoisonPower>() < Threshold) return;
        Flash();
        await PlayerCmd.GainEnergy(Amount, Owner.Player!);
    }
}
