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

// Soaks Poison damage one for one, and only Poison. It is not healing and it is not Block: it does not
// expire at the end of your turn and it does not stop an attack. It is the Alchemist's own dosing,
// paid for in advance.
//
// The ModifyDamageAdditive override is spelled differently on the two game branches, so it lives in
// Compat/AntitoxinPowerCompat.cs and calls the branch-agnostic Absorb below.
public partial class AntitoxinPower : AlchemistPower
{
    // The ceiling the second bar reads against. Anti-toxin never decays on its own, so without a cap a
    // quiet stretch of combat would bank an arbitrary buffer. Cards and relics raise it by granting
    // AntitoxinCapacityPower, and AntitoxinCap is what enforces the result
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

    // A Poison tick is the only damage that arrives unblockable and unpowered with no dealer and no card
    // behind it. The base game's own Poison forecast runs through this same hook, so reducing here keeps
    // the incoming damage number honest without a separate patch
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
        if (spend >= Amount)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(choiceContext, this, -spend, Owner, null, silent: true);
    }
}
