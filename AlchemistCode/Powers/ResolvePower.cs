using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// Amount represents the HP threshold (15 base, 10 upgraded)
public class ResolvePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        var missingHp = Owner.MaxHp - Owner.CurrentHp;
        if (missingHp <= 0 || Amount <= 0) return;
        var strengthGain = (missingHp / Amount);
        if (strengthGain > 0)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, strengthGain, Owner, null);
        }
    }
}
