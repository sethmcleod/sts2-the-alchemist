using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// The ModifyDamageAdditive override is spelled differently on the two game branches, so it lives in
// Compat/AntitoxinPowerCompat.cs and calls the branch-agnostic Absorb below. The ceiling and the
// per-turn absorb record live in AntitoxinRules, which exists even when this power does not.
public partial class AntitoxinPower : AlchemistPower
{
    // Raised by granting AntitoxinCapacityPower; AntitoxinRules is what enforces the result
    public const int BaseMax = 20;

    public static int MaxFor(Creature creature) =>
        BaseMax + creature.GetPowerAmount<AntitoxinCapacityPower>();

    // Written by Absorb, spent by BeforeDamageReceived. This is safe even though Absorb also runs for
    // damage previews: CreatureCmd.Damage calls Hook.ModifyDamage and Hook.BeforeDamageReceived back to
    // back with nothing in between, so a value left behind by a preview is always overwritten by the
    // real call before it can be spent.
    private int _pendingSpend;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // A Poison tick is the only damage that arrives unblockable and unpowered with no dealer and no
    // card behind it. PoisonPower.CalculateTotalDamageNextTurn runs its forecast through this same
    // hook, so reducing here also keeps the incoming damage preview correct
    private bool IsPoisonTick(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        target == Owner
        && dealer == null
        && cardSource == null
        && props.HasFlag(ValueProp.Unblockable)
        && props.HasFlag(ValueProp.Unpowered);

    internal decimal Absorb(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (!IsPoisonTick(target, props, dealer, cardSource) || amount <= 0)
            return 0m;

        var absorbed = Math.Min(Amount, (int)amount);
        _pendingSpend = absorbed;
        return -absorbed;
    }

    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!IsPoisonTick(target, props, dealer, cardSource)) return;

        var spend = _pendingSpend;
        _pendingSpend = 0;
        if (spend <= 0) return;

        Flash();
        AntitoxinRules.MarkAbsorbed(Owner);
        if (Owner.GetPower<CruciblePower>() is { } crucible)
            await crucible.OnAbsorbed(spend);
        if (spend >= Amount)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(choiceContext, this, -spend, Owner, null, silent: true);
    }
}
