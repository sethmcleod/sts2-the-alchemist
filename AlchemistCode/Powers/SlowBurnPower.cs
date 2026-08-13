using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class SlowBurnPower : AlchemistPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // The play turn's own end arms the charge; the end of the next player turn detonates it. The
    // flag is transient: a mid-combat reload restarts the delay, which is acceptable
    private bool _armed;

    // The forecast reads this so the health bar preview shows only on the turn the hit will land, not
    // the turn it is applied
    internal bool IsArmed => _armed;

    // What the owner will actually lose, after Hard to Kill caps the damage and Intangible caps the HP
    // loss. Poison previews itself the same way, through the shared damage hook (PoisonPower
    // .CalculateTotalDamageNextTurn)
    internal int EffectiveDamage
    {
        get
        {
            // Owner asserts mutable, so it throws on the canonical model behind a dumb concept tooltip.
            // The raw amount is the only meaningful answer there anyway, with no combat to modify it
            if (!IsMutable || Owner?.CombatState is not { } combat) return (int)Amount;
            var damage = GameCompat.ModifyDamage(combat.RunState, combat, Owner, Applier, Amount,
                ValueProp.Unpowered, null, null, ModifyDamageHookType.All, CardPreviewMode.None, out _);
            damage = Hook.ModifyHpLost(combat.RunState, combat, Owner, damage, ValueProp.Unpowered,
                Applier, null, HpLossHookPhase.All, out _);
            return (int)damage;
        }
    }

    // Names the turn the hit lands, so the tooltip agrees with the health bar forecast
    internal string WhenText =>
        new LocString("powers", IsMutable && _armed
            ? "ALCHEMIST-UNSTABLE_COMPOUND_POWER.when_this_turn"
            : "ALCHEMIST-UNSTABLE_COMPOUND_POWER.when_next_turn").GetFormattedText();

    // Fills the smart (instance) tooltip. The plain Description path below never sees these
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new DynamicVar[] { new SlowBurnDamageVar(), new SlowBurnWhenVar() };

    // Fills the dumb (concept) tooltip, which is built from the canonical model and so skips DynamicVars
    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add("Damage", EffectiveDamage);
            description.Add("When", WhenText);
            return description;
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // The power sits on an enemy, so track the APPLIER's turn ends, not the owner's
        if (Applier == null || !participants.Contains(Applier)) return;
        if (!_armed)
        {
            _armed = true;
            return;
        }
        Flash();
        // Unpowered: the number on the card is the number dealt (the Inversion precedent). Block applies
        await GameCompat.Damage(new ThrowingPlayerChoiceContext(), Owner, Amount, ValueProp.Unpowered, Applier, null, null);
        await PowerCmd.Remove(this);
    }
}
