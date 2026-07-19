using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class BloomPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _triggeredThisTurn;

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner)) _triggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        // On the first Poison that you apply each turn, add Amount more to the same target. The code sets
        // the guard before it applies the Poison, so it ignores the recursive AfterPowerAmountChanged
        if (!_triggeredThisTurn && power is PoisonPower && applier == Owner && amount > 0)
        {
            _triggeredThisTurn = true;
            Flash();
            await PowerCmd.Apply<PoisonPower>(choiceContext, power.Owner, Amount, Owner, null);
        }
    }
}
