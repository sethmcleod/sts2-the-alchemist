using System.Linq;
using Alchemist.AlchemistCode.Compat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class PassItOnPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<AntitoxinPower>() };

    // Driven by AntitoxinPower rather than a damage hook, because a fully soaked tick deals no damage
    // for a hook to see
    internal async Task OnAbsorbed(int amount)
    {
        if (amount <= 0 || Owner.CombatState is not { } combat) return;
        Flash();
        foreach (var enemy in combat.GetOpponentsOf(Owner).Where(e => e.IsAlive).ToList())
            await GameCompat.Damage(new ThrowingPlayerChoiceContext(), enemy, amount * Amount,
                ValueProp.Unpowered, Owner, null, null);
    }
}
