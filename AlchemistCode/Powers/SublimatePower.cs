using System.Linq;
using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class SublimatePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<AntitoxinPower>() };

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;

        var fuel = Owner.GetPowerAmount<AntitoxinPower>();
        if (fuel <= 0) return;

        var target = combatState.GetOpponentsOf(Owner).Where(e => e.IsAlive)
            .OrderByDescending(e => e.CurrentHp).FirstOrDefault();
        if (target == null) return;

        Flash();
        // Unpowered, so Strength does not inflate a number defined as the size of the bar
        await GameCompat.Damage(new ThrowingPlayerChoiceContext(), target, fuel,
            ValueProp.Unpowered, Owner, null, null);
    }
}
