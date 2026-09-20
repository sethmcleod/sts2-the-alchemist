using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;

namespace Alchemist.AlchemistCode.Analytics;

// Analytics only: the card-play facts the run row cannot recover from the deck list. How deep a
// Ferment card had brewed when it was played says whether the lane is played as designed; a Mix
// played is a Mix that was not wasted to Ethereal or the end-of-turn discard
public sealed class AnalyticsHooks() : CustomSingletonModel(HookType.Combat)
{
    public const string FermentPlays = "ferment_plays";
    public const string FermentTurns = "ferment_turns";
    public const string FermentZero = "ferment_zero";
    // Mixes played per fight, bucketed, so a "per Mix played" payoff can be priced against how many
    // Mixes a fight really sees, which the run total hides
    public const string MixesPerFight = "mixfight:";

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

    // Before the game clears its play history, which is where the per-fight count lives
    public override Task AfterCombatEnd(CombatRoom room)
    {
        foreach (var player in room.CombatState.Players)
            RunCounters.Tally(player, MixesPerFight + FightBucket(Mixing.PlayedThisCombat(player)));
        return Task.CompletedTask;
    }

    private static string FightBucket(int mixes) => mixes switch
    {
        <= 4 => mixes.ToString(),
        <= 6 => "5-6",
        <= 9 => "7-9",
        _ => "10+",
    };
}
