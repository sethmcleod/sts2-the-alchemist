using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// Amount is the stack count (1 per Resolve played) and acts as the Strength multiplier:
// each turn, gain (Amount) Strength for every HpThreshold HP you are missing.
public class ResolvePower : AlchemistPower
{
    private const int HpThreshold = 15;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        var missingHp = Owner.MaxHp - Owner.CurrentHp;
        var strengthGain = Amount * (missingHp / HpThreshold);
        if (strengthGain > 0)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, strengthGain, Owner, null);
        }
    }
}
