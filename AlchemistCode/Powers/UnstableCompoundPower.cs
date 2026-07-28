using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class UnstableCompoundPower : AlchemistPower
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
            if (Owner?.CombatState is not { } combat) return (int)Amount;
            var damage = Hook.ModifyDamage(combat.RunState, combat, Owner, Applier, Amount,
                ValueProp.Unpowered, null, null, ModifyDamageHookType.All, CardPreviewMode.None, out _);
            damage = Hook.ModifyHpLost(combat.RunState, combat, Owner, damage, ValueProp.Unpowered,
                Applier, null, HpLossHookPhase.All, out _);
            return (int)damage;
        }
    }

    // The tooltip reads the capped number and names the turn the hit lands, so it agrees with the
    // health bar. Description is the only hook: PowerModel adds its own args privately, and the smart
    // description falls back to this one when no smartDescription key exists
    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add("Damage", EffectiveDamage);
            description.Add("When", _armed ? "this turn" : "your next turn");
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
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, Amount, ValueProp.Unpowered, Applier, null, null);
        await PowerCmd.Remove(this);
    }
}
