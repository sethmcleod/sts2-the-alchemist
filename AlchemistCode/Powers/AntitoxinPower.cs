using System;
using System.Collections.Generic;
using Alchemist.AlchemistCode.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// The ModifyDamageAdditive override is spelled differently on the two game branches, so it lives in
// Compat/AntitoxinPowerCompat.cs and calls the branch-agnostic Absorb below. The per-turn absorb
// record lives in AntitoxinRules, which exists even when this power does not.
public partial class AntitoxinPower : AlchemistPower
{
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

    // For the bar. The Id comes off the canonical model, so MegaTryAddingTip de-duplicates this
    // against any other Antitoxin tip
    public static IHoverTip TipFor(Creature creature)
    {
        var model = ModelDb.Power<AntitoxinPower>();
        return new HoverTip(model, model.Description.GetFormattedText(), isSmart: false);
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
