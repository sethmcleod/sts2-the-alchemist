using System;
using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// The ModifyDamageAdditive override is spelled differently on the two game branches, so it lives in
// Compat/AntitoxinPowerCompat.cs and calls the branch-agnostic Absorb below. The ceiling and the
// per-turn absorb record live in AntitoxinRules, which exists even when this power does not.
public partial class AntitoxinPower : AlchemistPower
{
    // Raised for the combat by granting AntitoxinCapacityPower; AntitoxinRules enforces the result
    public const int BaseMax = 20;

    public static int MaxFor(Creature creature) =>
        BaseMax + creature.GetPowerAmount<AntitoxinCapacityPower>();

    // Written by Absorb, spent by BeforeDamageReceived. Absorb clears it on every pass, so it only
    // ever holds a value for the one hit that just passed the Poison tick test
    private int _pendingSpend;

    public override PowerType Type => PowerType.Buff;

    // The bar under the health bar shows the amount, so the icon would be a second copy of the same
    // number. AntitoxinBarPatches keeps the hover tips on the creature, because PowerModel.HoverTips
    // returns nothing once a power is invisible
    protected override bool IsVisibleInternal => false;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // The limit is a number two places have to agree on, so neither the tooltip nor the bar restates it
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new AntitoxinLimitVar() };

    // Read live rather than cached, so no Creature from a finished combat is held. Null outside a
    // combat, which is when the base ceiling is the honest number to print
    private static Creature? LocalCreature() =>
        NCombatRoom.Instance is { } room
            ? LocalContext.GetMe(room.CreatureNodes.Select(node => node.Entity))
            : null;

    internal static int LimitFor(Creature? creature) =>
        (creature ?? LocalCreature()) is { } subject ? MaxFor(subject) : BaseMax;

    private LocString DescriptionFor(Creature? creature)
    {
        var description = base.Description;
        description.Add("Limit", LimitFor(creature));
        return description;
    }

    public override LocString Description => DescriptionFor(IsMutable ? Owner : null);

    // For the bar, which is the one place that has to tell creatures apart: in multiplayer you can
    // hover another player's bar. The Id still comes off the canonical model, so MegaTryAddingTip
    // de-duplicates this against any other Antitoxin tip as before
    public static IHoverTip TipFor(Creature creature)
    {
        var model = ModelDb.Power<AntitoxinPower>();
        return new HoverTip(model, model.DescriptionFor(creature).GetFormattedText(), isSmart: false);
    }

    // PoisonPower.CalculateTotalDamageNextTurn runs its forecast through this same hook, so reducing
    // here also keeps the incoming damage preview correct
    private bool IsPoisonTick(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
        target == Owner
        && AntitoxinRules.IsPoisonTick(Owner, amount, props, dealer, cardSource);

    internal decimal Absorb(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        // Cleared on EVERY pass, including the rejects. This hook runs for all damage the owner
        // takes, and the Poison forecast runs it without ever reaching BeforeDamageReceived, so a
        // value left set by a reject would be spent on whatever landed next, such as an enemy attack
        _pendingSpend = 0;
        AntitoxinRules.ClearTickAbsorb(Owner);
        if (!IsPoisonTick(target, amount, props, dealer, cardSource))
            return 0m;

        var absorbed = Math.Min(Amount, (int)amount);
        _pendingSpend = absorbed;
        return -absorbed;
    }

    private void AbsorbSplash()
    {
        var vfx = NGaseousImpactVfx.Create(Owner, AlchemistModConfig.AntitoxinBarColor);
        if (vfx != null) NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(vfx);
    }

    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // Deliberately not re-running IsPoisonTick: this hook is handed the amount AFTER
        // ModifyDamage, so a fully absorbed tick arrives as 0 and would fail the predicate's own
        // amount test. Absorb already vetted the hit, so a non-zero _pendingSpend is the proof
        if (target != Owner) return;

        var spend = _pendingSpend;
        _pendingSpend = 0;
        if (spend <= 0) return;

        Flash();
        AbsorbSplash();
        AntitoxinRules.MarkAbsorbed(Owner, spend);
        if (Owner.GetPower<PassItOnPower>() is { } crucible)
            await crucible.OnAbsorbed(spend);
        if (Owner.GetPower<WardedPower>() is { } slag)
            await slag.OnAbsorbed(spend);
        if (spend >= Amount)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(choiceContext, this, -spend, Owner, null, silent: true);
        // The Poison stack is left in place on purpose. The dose is what powers the character's
        // attacks, so Antitoxin pays the tick and PoisonPower's own decrement is the only thing
        // that lowers it. The 0.9.0 cure made the stack unreadable one turn after any dose
    }
}
