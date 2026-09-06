using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Analytics;

// Analytics only: the card-play facts the run row cannot recover from the deck list. How deep a
// Ferment card had brewed when it was played says whether the lane is played as designed; a Mix
// played is a Mix that was not wasted to Ethereal or the end-of-turn discard
public sealed class AnalyticsHooks() : CustomSingletonModel(HookType.Combat)
{
    public const string FermentPlays = "ferment_plays";
    public const string FermentTurns = "ferment_turns";
    public const string FermentZero = "ferment_zero";

    // Before OnPlay, because Pour Over drains its own turns as it resolves; the raw stored count,
    // because Mother of Vinegar's read-time floor would make zero unreachable
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card is AlchemistCard { IsFermentInline: true, Owner: { } player } ferment)
        {
            RunCounters.Tally(player, FermentPlays);
            RunCounters.Tally(player, FermentTurns, ferment.StoredFermentTurns);
            if (ferment.StoredFermentTurns == 0) RunCounters.Tally(player, FermentZero);
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card.Owner is { } player && Mixing.IsMix(card))
            RunCounters.Tally(player, RunCounters.MixPlayed + Mixing.KindLabel(card));
        return Task.CompletedTask;
    }
}
