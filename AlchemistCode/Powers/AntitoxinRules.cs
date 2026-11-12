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

public sealed partial class AntitoxinRules() : CustomSingletonModel(HookType.Combat)
{
    private static readonly HashSet<Creature> Absorbed = [];

    // Royal Poison and max HP loss share the tick's shape. Only a real tick has that much Poison on the target
    internal static bool IsPoisonTick(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        HasPoisonTickShape(props, dealer, cardSource)
        && amount > 0
        && target.GetPowerAmount<PoisonPower>() >= amount;

    // No amount test: BeforeDamageReceived only sees the post-ModifyDamage amount
    internal static bool HasPoisonTickShape(ValueProp props, Creature? dealer, CardModel? cardSource) =>
        dealer == null
        && cardSource == null
        && props.HasFlag(ValueProp.Unblockable)
        && props.HasFlag(ValueProp.Unpowered);

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

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (!power.Owner.IsPlayer || amount <= 0) return Task.CompletedTask;
        var player = power.Owner.Player;
        if (power is PoisonPower)
        {
            Analytics.RunCounters.Add(player, Analytics.RunCounters.PoisonGained, (int)amount);
            Analytics.RunCounters.RaiseTo(player, Analytics.RunCounters.PoisonPeak, (int)power.Amount);
        }
        if (power is AntitoxinPower)
        {
            Analytics.RunCounters.RaiseTo(player, Analytics.RunCounters.AntitoxinPeak, (int)power.Amount);
            var source = cardSource ?? AntitoxinPower.GrantingSource;
            Analytics.RunCounters.Tally(player, Analytics.RunCounters.AntitoxinSource + Analytics.RunCounters.Label(source),
                (int)amount);
        }
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        LethalTick.Remove(target);
        if (target.IsPlayer && IsPoisonTick(target, result.UnblockedDamage, props, dealer, cardSource))
        {
            Analytics.RunCounters.Add(target.Player, Analytics.RunCounters.PoisonBled, result.UnblockedDamage);
        }
        return Task.CompletedTask;
    }

    internal static bool AbsorbedThisTurn(Creature creature) => Absorbed.Contains(creature);

    // The game skips AfterDamageReceived for a killing hit, so the lethal tick is latched on the way in
    private static readonly Dictionary<Creature, int> LethalTick = new();

    public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        LethalTick.Remove(target);
        if (target.IsPlayer && amount > 0 && amount >= target.CurrentHp
            && HasPoisonTickShape(props, dealer, cardSource)
            && target.GetPowerAmount<PoisonPower>() > 0)
            LethalTick[target] = (int)amount;
        return Task.CompletedTask;
    }

    private static Task OnDeath(Creature creature, bool wasRemovalPrevented)
    {
        if (!LethalTick.Remove(creature, out var bled)) return Task.CompletedTask;
        Analytics.RunCounters.Add(creature.Player, Analytics.RunCounters.PoisonBled, bled);
        if (!wasRemovalPrevented)
            Analytics.RunCounters.Tally(creature.Player, Analytics.RunCounters.PoisonDeath);
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        Absorbed.Clear();
        AbsorbedOnTick.Clear();
        LethalTick.Clear();
        return Task.CompletedTask;
    }
}
