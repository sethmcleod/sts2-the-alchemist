using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// Neither of these can live on AntitoxinPower: the ceiling has to apply to the first grant of a
// combat, when no AntitoxinPower exists yet, and the absorb record has to outlive the power being
// spent down to nothing. A combat-hook singleton is always listening.
public sealed class AntitoxinRules() : CustomSingletonModel(HookType.Combat)
{
    private static readonly HashSet<Creature> Absorbed = [];

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target,
        decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not AntitoxinPower || amount <= 0) return false;

        var room = AntitoxinPower.MaxFor(target) - target.GetPowerAmount<AntitoxinPower>();
        if (amount <= room) return false;

        modifiedAmount = Math.Max(0, room);
        return true;
    }

    // Royal Poison and in-combat max HP loss deal damage with the same null dealer and
    // Unblockable|Unpowered shape as a Poison tick. PoisonPower.Trigger deals exactly the stack it is
    // about to decrement, so requiring that much Poison on the target is what separates them
    internal static bool IsPoisonTick(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        dealer == null
        && cardSource == null
        && props.HasFlag(ValueProp.Unblockable)
        && props.HasFlag(ValueProp.Unpowered)
        && amount > 0
        && target.GetPowerAmount<PoisonPower>() >= amount;

    internal static void MarkAbsorbed(Creature creature) => Absorbed.Add(creature);

    internal static bool AbsorbedThisTurn(Creature creature) => Absorbed.Contains(creature);

    // Cleared wholesale rather than per participant, so no Creature from a finished combat is held
    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        Absorbed.Clear();
        return Task.CompletedTask;
    }
}
