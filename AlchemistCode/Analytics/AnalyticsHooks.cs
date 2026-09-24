using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Commands;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Analytics;

// Counts what the run history does not keep: plays per card, Ferment depth at play, Mixes played and
// left over, and the Poison damage enemies take in a solo run
public sealed class AnalyticsHooks() : CustomSingletonModel(HookType.Combat)
{
    // Before OnPlay, because Pour Over drains its own turns as it resolves; the raw stored count,
    // because Mother of Vinegar's read-time floor would make zero unreachable
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card is AlchemistCard { IsFermentInline: true, Owner: { } player } ferment)
        {
            var turns = ferment.StoredFermentTurns;
            var label = RunCounters.Label(ferment);
            RunCounters.Tally(player, RunCounters.FermentPlays);
            RunCounters.Tally(player, RunCounters.FermentTurns, turns);
            RunCounters.Tally(player, RunCounters.FermentCardPlays + label);
            RunCounters.Tally(player, RunCounters.FermentCardTurns + label, turns);
            if (turns == 0) RunCounters.Tally(player, RunCounters.FermentZero);
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card.Owner is not { } player) return Task.CompletedTask;
        if (Mixing.IsMix(card))
            RunCounters.Tally(player, RunCounters.MixPlayed + Mixing.KindLabel(card));
        else if (card is AlchemistCard)
            RunCounters.Tally(player, RunCounters.CardPlayed + RunCounters.Label(card));
        return Task.CompletedTask;
    }

    // Before the game clears its play history and the piles, which is where both counts live
    public override Task AfterCombatEnd(CombatRoom room)
    {
        CountFight(room.CombatState.Players);
        return Task.CompletedTask;
    }

    private static void CountFight(IReadOnlyList<Player> players)
    {
        foreach (var player in players)
        {
            RunCounters.Tally(player, RunCounters.MixesPerFight + FightBucket(Mixing.PlayedThisCombat(player)));
            if (player.PlayerCombatState is { } piles)
                RunCounters.Tally(player, RunCounters.MixLeftover,
                    new[] { piles.Hand, piles.DrawPile, piles.DiscardPile }.Sum(p => p.Cards.Count(Mixing.IsMix)));
        }
        EnemyTicks.Clear();
    }

    private static string FightBucket(int mixes) => mixes switch
    {
        <= 4 => mixes.ToString(),
        <= 6 => "5-6",
        <= 9 => "7-9",
        _ => "10+",
    };

    // An enemy's Poison tick, held from BeforeDamageReceived until the damage lands. The game skips
    // AfterDamageReceived for the hit that kills, so a lethal tick is counted in AfterDeath instead
    private static readonly Dictionary<Creature, int> EnemyTicks = new();

    public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        EnemyTicks.Remove(target);
        if (!target.IsPlayer && SoloPlayer(target) != null
            && AntitoxinRules.IsPoisonTick(target, amount, props, dealer, cardSource))
            EnemyTicks[target] = Math.Min((int)amount, target.CurrentHp);
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (EnemyTicks.Remove(target))
            RunCounters.Tally(SoloPlayer(target), RunCounters.PoisonDealt, result.UnblockedDamage);
        return Task.CompletedTask;
    }

    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (EnemyTicks.Remove(creature, out var damage))
            RunCounters.Tally(SoloPlayer(creature), RunCounters.PoisonDealt, damage);
        // A lost fight ends through LoseCombat, which never raises AfterCombatEnd
        if (!wasRemovalPrevented && creature.IsPlayer && creature.CombatState is { } combat
            && combat.Players.All(p => p.Creature.IsDead))
            CountFight(combat.Players);
        return Task.CompletedTask;
    }

    // In co-op another player's Poison ticks the same way, so enemy ticks count only in a solo fight
    private static Player? SoloPlayer(Creature creature) =>
        creature.CombatState?.Players is { Count: 1 } players ? players[0] : null;
}
