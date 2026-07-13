using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class ImbuePower : AlchemistPower
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
        // First Poison you apply each turn: stack Amount more onto that same target. The guard is set before
        // applying, so the recursive AfterPowerAmountChanged from the extra Poison is ignored
        if (!_triggeredThisTurn && power is PoisonPower && applier == Owner && amount > 0)
        {
            _triggeredThisTurn = true;
            Flash();
            await PowerCmd.Apply<PoisonPower>(choiceContext, power.Owner, Amount, Owner, null);
        }
    }
}
