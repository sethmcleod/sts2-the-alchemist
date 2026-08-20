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

// Neither of these can live on AntitoxinPower: the ceiling has to apply to the first grant of a
// combat, when no AntitoxinPower exists yet, and the absorb record has to outlive the power being
// spent down to nothing. A combat-hook singleton is always listening.
public sealed class AntitoxinRules() : CustomSingletonModel(HookType.Combat)
{
    private static readonly HashSet<Creature> Absorbed = [];

    // The limit is a wall: a grant past it is clipped. It used to spill into Block, which was an
    // alternate Block source the rubric names, and it is gone as of 2026-08-18
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target,
        decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not AntitoxinPower || amount <= 0) return false;

        var room = Math.Max(0, AntitoxinPower.MaxFor(target) - target.GetPowerAmount<AntitoxinPower>());
        if (amount <= room) return false;
        modifiedAmount = room;
        return true;
    }

    // Royal Poison and in-combat max HP loss deal damage with the same null dealer and
    // Unblockable|Unpowered shape as a Poison tick. PoisonPower.Trigger deals exactly the stack it is
    // about to decrement, so requiring that much Poison on the target is what separates them
    internal static bool IsPoisonTick(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        dealer == null
        && cardSource == null
        && props.HasFlag(ValueProp.Unblockable)
        && props.HasFlag(ValueProp.Unpowered)
        && amount > 0
        && target.GetPowerAmount<PoisonPower>() >= amount;

    // The absorbed slice of the tick that is resolving right now. Callus runs in
    // AfterDamageReceived, which is handed the post-absorb amount, so without this a fully soaked tick
    // pays it nothing and the character's own starter relic switches them off
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
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target.IsPlayer && IsPoisonTick(target, result.UnblockedDamage, props, dealer, cardSource))
            Analytics.RunCounters.Add(target.Player, Analytics.RunCounters.PoisonBled, result.UnblockedDamage);
        return Task.CompletedTask;
    }

    internal static bool AbsorbedThisTurn(Creature creature) => Absorbed.Contains(creature);

    // Cleared wholesale rather than per participant, so no Creature from a finished combat is held
    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        Absorbed.Clear();
        AbsorbedOnTick.Clear();
        return Task.CompletedTask;
    }
}
