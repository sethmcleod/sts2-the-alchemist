using System.Linq;
using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class SublimatePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;

        var fuel = Owner.GetPowerAmount<PoisonPower>() * Amount;
        if (fuel <= 0) return;

        var target = combatState.GetOpponentsOf(Owner).Where(e => e.IsAlive)
            .OrderByDescending(e => e.CurrentHp).FirstOrDefault();
        if (target == null) return;

        Flash();
        // Unpowered, so Strength does not inflate a number defined as the size of the dose
        await GameCompat.Damage(new ThrowingPlayerChoiceContext(), target, fuel,
            ValueProp.Unpowered, Owner, null, null);
    }
}
