using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// A combat-hook singleton, because Callus, Second Skin and the analytics counters all read the tick
// record on creatures that may hold no Antitoxin at all.
public sealed class AntitoxinRules() : CustomSingletonModel(HookType.Combat)
{
    private static readonly HashSet<Creature> Absorbed = [];

    // Poison that got past the capacity this turn. Smelling Salts reads it in AfterSideTurnStartLate:
    // the tick has resolved by then, and comparing the two stacks instead would read a Poison amount
    // PoisonPower has already decremented
    private static readonly HashSet<Creature> Bled = [];

    internal static bool BledThisTurn(Creature creature) => Bled.Contains(creature);

    // Royal Poison and in-combat max HP loss deal damage with the same null dealer and
    // Unblockable|Unpowered shape as a Poison tick. PoisonPower.Trigger deals exactly the stack it is
    // about to decrement, so requiring that much Poison on the target is what separates them
    internal static bool IsPoisonTick(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        HasPoisonTickShape(props, dealer, cardSource)
        && amount > 0
        && target.GetPowerAmount<PoisonPower>() >= amount;

    // The half of the test that does NOT read the amount. BeforeDamageReceived is handed the
    // post-ModifyDamage amount, so it cannot re-run the amount test, but it can re-run this, and
    // this is the half that separates a tick from an enemy attack: an attack carries a dealer and
    // is not Unblockable
    internal static bool HasPoisonTickShape(ValueProp props, Creature? dealer, CardModel? cardSource) =>
        dealer == null
        && cardSource == null
        && props.HasFlag(ValueProp.Unblockable)
        && props.HasFlag(ValueProp.Unpowered);

    // The held slice of the tick resolving right now, written only by AntitoxinPower for a real tick.
    // Callus and Second Skin run in AfterDamageReceived, where the amount is already reduced, so a
    // fully held tick would pay them nothing without it
    private static readonly Dictionary<Creature, int> AbsorbedOnTick = new();

    internal static void ClearTickAbsorb(Creature creature) => AbsorbedOnTick.Remove(creature);

    internal static int TickAbsorb(Creature creature) =>
        AbsorbedOnTick.TryGetValue(creature, out var amount) ? amount : 0;

    internal static void MarkAbsorbed(Creature creature, int amount)
    {
        Absorbed.Add(creature);
        AbsorbedOnTick[creature] = amount;
        Analytics.RunCounters.Add(creature.Player, Analytics.RunCounters.PoisonAbsorbed, amount);
    }

    // Analytics only. The gained counter reads any positive Poison landing on a player; the bled
    // counter reads the tick that resolves AFTER absorption, so it is what Poison actually cost in
    // HP. IsPoisonTick is the one definition of the tick shape; the stack still holds the full
    // amount here because PoisonPower decrements after the damage lands
    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is PoisonPower && power.Owner.IsPlayer && amount > 0)
            Analytics.RunCounters.Add(power.Owner.Player, Analytics.RunCounters.PoisonGained, (int)amount);
        if (power is AntitoxinPower && power.Owner.IsPlayer && amount > 0)
            Analytics.RunCounters.RaiseTo(power.Owner.Player, Analytics.RunCounters.AntitoxinPeak,
                (int)power.Amount);
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target.IsPlayer && IsPoisonTick(target, result.UnblockedDamage, props, dealer, cardSource))
        {
            Bled.Add(target);
            Analytics.RunCounters.Add(target.Player, Analytics.RunCounters.PoisonBled, result.UnblockedDamage);
        }
        return Task.CompletedTask;
    }

    internal static bool AbsorbedThisTurn(Creature creature) => Absorbed.Contains(creature);

    // Cleared wholesale rather than per participant, so no Creature from a finished combat is held
    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        Absorbed.Clear();
        Bled.Clear();
        AbsorbedOnTick.Clear();
        return Task.CompletedTask;
    }
}
