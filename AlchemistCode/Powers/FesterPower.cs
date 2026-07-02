using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// "Poison is triggered against the enemy {Amount} additional time(s) next turn."
// Detects the natural poison tick (unblockable+unpowered damage with no dealer/card source),
// then runs Amount real triggers: each deals damage equal to the CURRENT Poison and ticks
// Poison down by 1, matching the game's own trigger semantics. Expires after processing,
// or at the end of the owner's turn if no poison ticked.
public class FesterPower : AlchemistPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0) return;
        if (dealer != null || cardSource != null) return;
        if (!props.HasFlag(ValueProp.Unblockable) || !props.HasFlag(ValueProp.Unpowered)) return;

        Flash();
        // Our extra tick damage passes a non-null dealer, so it can't re-enter this handler.
        var triggerDealer = Applier ?? Owner;
        for (var i = 0; i < Amount; i++)
        {
            var poison = Owner.GetPowerAmount<PoisonPower>();
            if (poison <= 0) break;
            await CreatureCmd.Damage(choiceContext, Owner, poison,
                ValueProp.Unblockable | ValueProp.Unpowered, triggerDealer, null);
            await PowerCmd.Apply<PoisonPower>(choiceContext, Owner, -1, Applier, null);
        }
        await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // "Next turn" only — if the owner's turn ends without a poison tick, expire anyway.
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
