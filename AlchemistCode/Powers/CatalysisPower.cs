using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Powers;

public class CatalysisPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { AlchemistTips.Reaction };

    private bool _triggeredThisTurn;
    // The specific play we owe a draw to, not a bare flag: a card play can nest inside another
    // (an autoplay, a Sly discard), and the inner BeforeCardPlayed would clear a flag the outer
    // play had set, swallowing the draw
    private CardPlay? _fireFor;

    // Read the Reaction BEFORE the card resolves. AlchemistCard.ReactionActive compares against the
    // last play that FINISHED this turn, so once this card finishes it becomes its own predecessor
    // and the answer flips. Only this hook sees the true value
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (!_triggeredThisTurn
            && cardPlay.Card is AlchemistCard { IsReactionCard: true } card
            && card.Owner == Owner.Player
            && card.ReactionActive)
            _fireFor = cardPlay;
        return Task.CompletedTask;
    }

    // Draw after the card resolves, so the cards arrive behind its own effect
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!ReferenceEquals(_fireFor, cardPlay)) return;
        _fireFor = null;
        if (Owner.Player is not { } player) return;
        _triggeredThisTurn = true;
        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner)) { _triggeredThisTurn = false; _fireFor = null; }
        return Task.CompletedTask;
    }
}
