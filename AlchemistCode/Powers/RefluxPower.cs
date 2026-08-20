using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Powers;

public class RefluxPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // A whole team's debuffs in one turn is unbounded, so the payout is capped per turn
    private const int MaxTriggers = 3;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        // The card source requirement excludes our own poison, which has none, so two players running
        // Reflux cannot trigger each other without end
        if (amount <= 0 || cardSource == null || power.Type != PowerType.Debuff) return;
        if (applier == null || applier == Owner || !applier.IsPlayer) return;
        if (power.Owner is not { IsPlayer: false, IsAlive: true } enemy) return;
        Flash();
        await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, Amount, Owner, null);
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return Task.CompletedTask;
        return PowerCmd.Remove<RefluxPower>(Owner);
    }
}
