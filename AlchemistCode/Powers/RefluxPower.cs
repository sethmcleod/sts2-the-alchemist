using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class RefluxPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        // React only to a debuff a *teammate* applies to an enemy via a card play. Requiring a card
        // source excludes our own reactive Poison (applied with a null source), so two players'
        // Reflux powers can't ping-pong debuffs off each other forever.
        if (amount <= 0 || cardSource == null || power.Type != PowerType.Debuff) return;
        if (applier == null || applier == Owner || !applier.IsPlayer) return;
        if (power.Owner is not { IsPlayer: false, IsAlive: true } enemy) return;
        Flash();
        await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, Amount, Owner, null);
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // "this turn" — the buff lasts only until the end of the owner's turn.
        if (participants.Contains(Owner)) return PowerCmd.Remove<RefluxPower>(Owner);
        return Task.CompletedTask;
    }
}
