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
        // The forecast still needs the reduction below so the previewed number is right, but it must not
        // touch the pending record in either direction. Every real pass clears first, including the
        // rejects, because this hook runs for all damage the owner takes
        var forecast = AntitoxinRules.InPoisonForecast;
        if (!forecast)
        {
            _pendingSpend = 0;
            AntitoxinRules.ClearTickAbsorb(Owner);
        }
        if (!IsPoisonTick(target, amount, props, dealer, cardSource))
            return 0m;

        var absorbed = Math.Min(Amount, (int)amount);
        if (!forecast) _pendingSpend = absorbed;
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
        if (target != Owner) return;

        var spend = _pendingSpend;
        _pendingSpend = 0;
        if (spend <= 0) return;

        // Absorb clears _pendingSpend on every pass, but PoisonPower.CalculateTotalDamageNextTurn
        // runs Absorb through ModifyDamage and never reaches this hook, so a forecast that lands
        // between ModifyDamage and here leaves a value set that belongs to no real hit. The next hit
        // then spends it, which drained Antitoxin on attacks Block had already eaten. Re-checking the
        // shape rejects those: an attack carries a dealer and is not Unblockable. The amount test
        // cannot be re-run, because a fully absorbed tick arrives here as 0
        if (!AntitoxinRules.HasPoisonTickShape(props, dealer, cardSource)) return;

        Flash();
        AbsorbSplash();
        AntitoxinRules.MarkAbsorbed(Owner, spend);
        if (Owner.GetPower<PassItOnPower>() is { } crucible)
            await crucible.OnAbsorbed(spend);
        if (spend >= Amount)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(choiceContext, this, -spend, Owner, null, silent: true);
        // The Poison stack is left in place on purpose. The dose is what powers the character's
        // attacks, so Antitoxin pays the tick and PoisonPower's own decrement is the only thing
        // that lowers it. The 0.9.0 cure made the stack unreadable one turn after any dose
    }
}
