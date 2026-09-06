using MegaCrit.Sts2.Core.Entities.Powers;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class BrinePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // After the play, not on each hit: the base Envenom keys off unblocked damage, and this card
    // promises the Poison whether or not the hit landed through Block
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card.Type != CardType.Attack || card.Owner?.Creature != Owner) return;
        if (Owner.CombatState is not { } combat) return;
        var targets = card.TargetType == TargetType.AllEnemies
            ? combat.Enemies.Where(e => e.IsAlive).ToList()
            : cardPlay.Target is { IsAlive: true, IsPlayer: false } single ? new List<Creature> { single } : new();
        if (targets.Count == 0) return;
        Flash();
        foreach (var target in targets)
            await PowerCmd.Apply<PoisonPower>(choiceContext, target, Amount, Owner, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        await PowerCmd.Remove(this);
    }
}
