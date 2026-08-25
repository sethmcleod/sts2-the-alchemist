using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// A threshold that holds back Poison damage up to its own amount every turn and is never spent, so
// the tick the owner takes is max(0, Poison - Antitoxin).
//
// The ModifyDamageAdditive override is spelled differently on the two game branches, so it lives in
// Compat/AntitoxinPowerCompat.cs and calls the branch-agnostic Absorb below.
public partial class AntitoxinPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;

    // The bar shows the amount, so an icon would repeat it. AntitoxinBarPatches puts the hover tips
    // back on the creature, because PowerModel.HoverTips goes empty once a power is invisible
    protected override bool IsVisibleInternal => false;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    // For the bar. The Id comes off the canonical model, so MegaTryAddingTip de-duplicates it
    public static IHoverTip TipFor(Creature creature)
    {
        var model = ModelDb.Power<AntitoxinPower>();
        return new HoverTip(model, model.Description.GetFormattedText(), isSmart: false);
    }

    private bool IsPoisonTick(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
        target == Owner
        && AntitoxinRules.IsPoisonTick(Owner, amount, props, dealer, cardSource);

    // Pure: PoisonPower.CalculateTotalDamageNextTurn previews through this same hook, and since
    // nothing is spent the preview is correct without extra state, multi-trigger Accelerant included
    internal decimal Absorb(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource) =>
        IsPoisonTick(target, amount, props, dealer, cardSource)
            ? -Math.Min(Amount, (int)amount)
            : 0m;

    // Bookkeeping only, and only for real damage, which is why it is not in Absorb.
    //
    // The amount here is post-Absorb, so the held slice is derived rather than carried: PoisonPower
    // decrements AFTER the damage lands, so the stack still holds the full pre-tick value and the
    // reduction is stack - amount. Anything outside 0 < held <= Amount was not this power's doing
    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;

        // Callus and Second Skin read this in AfterDamageReceived, where the amount is already
        // reduced, so a fully held tick would pay them nothing without it. Cleared on every hit so
        // they only ever see the tick resolving right now
        AntitoxinRules.ClearTickAbsorb(Owner);
        if (!AntitoxinRules.HasPoisonTickShape(props, dealer, cardSource)) return;

        var stack = Owner.GetPowerAmount<PoisonPower>();
        var absorbed = stack - (int)amount;
        if (absorbed <= 0 || absorbed > Amount) return;

        AntitoxinRules.MarkAbsorbed(Owner, absorbed);
        if (Owner.GetPower<PassItOnPower>() is { } crucible)
            await crucible.OnAbsorbed(absorbed);
    }
}
