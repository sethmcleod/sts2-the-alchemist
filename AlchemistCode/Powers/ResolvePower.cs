using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// Amount is the total stack count (1 per Resolve played). Each copy is tracked by its own
// threshold: base copies grant 1 Strength per 20 missing HP, upgraded copies 1 per 15.
// Mixed stacks therefore behave as the sum of the individual copies, and the tooltip
// switches wording via SmartDescriptionLocKey (custom vars only render on the smart path).
public class ResolvePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new[] { new DynamicVar("Stacks20", 0m), new DynamicVar("Stacks15", 0m) };

    /// <summary>Called by the card after each apply — counts this copy under its threshold.</summary>
    public void RegisterCopy(bool upgraded) =>
        DynamicVars[upgraded ? "Stacks15" : "Stacks20"].BaseValue += 1;

    protected override string SmartDescriptionLocKey
    {
        get
        {
            var s20 = DynamicVars["Stacks20"].IntValue;
            var s15 = DynamicVars["Stacks15"].IntValue;
            if (s20 > 0 && s15 > 0) return $"{Id.Entry}.smartDescriptionMixed";
            if (s15 > 0) return $"{Id.Entry}.smartDescription15";
            return base.SmartDescriptionLocKey;
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        var missingHp = Owner.MaxHp - Owner.CurrentHp;
        var strengthGain = DynamicVars["Stacks20"].IntValue * (missingHp / 20)
                           + DynamicVars["Stacks15"].IntValue * (missingHp / 15);
        if (strengthGain > 0)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, strengthGain, Owner, null);
        }
    }
}
