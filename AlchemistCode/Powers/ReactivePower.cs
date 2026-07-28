using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Powers;

// Reagent's grant. AlchemistCard.ReactionActive reads the stack, so a card only spends it when the card
// actually carries a Reaction; a card with no rider passes through and the stack survives
public class ReactivePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player != Owner.Player) return;
        if (cardPlay.Card is not AlchemistCard { IsReactionCard: true }) return;
        Flash();
        await PowerCmd.Decrement(this);
    }

    // AfterSideTurnEnd fires for both sides; self-remove only at the owner's own turn end
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
