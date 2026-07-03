using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class MaterialisePower : AlchemistPower
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

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        // The first Poison you gain or apply each turn Infuses random cards in your Hand.
        if (!_triggeredThisTurn && power is PoisonPower && applier == Owner && amount > 0
            && Owner.Player is { } player)
        {
            _triggeredThisTurn = true;
            Flash();
            Infusion.InfuseRandomFromHand(player, Amount);
        }
        return Task.CompletedTask;
    }
}
