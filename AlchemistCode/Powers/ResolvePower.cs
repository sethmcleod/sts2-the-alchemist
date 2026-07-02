using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// Amount is the summed Strength per step (1 per base copy, 2 per upgraded copy):
// each turn, gain Amount Strength for every 20 HP you are missing.
public class ResolvePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        var missingHp = Owner.MaxHp - Owner.CurrentHp;
        var steps = missingHp / 20; // full 20-HP increments missing
        var strengthGain = Amount * steps;
        if (strengthGain > 0)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, strengthGain, Owner, null);
        }
    }
}
