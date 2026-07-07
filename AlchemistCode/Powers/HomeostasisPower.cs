using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class HomeostasisPower : AlchemistPower
{
    private const double LowerBound = 0.33;
    private const double UpperBound = 0.66;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        var pct = (double)Owner.CurrentHp / Owner.MaxHp;
        if (pct < LowerBound || pct > UpperBound) return;

        Flash();
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), Amount, Owner.Player!);
        await PlayerCmd.GainEnergy(Amount, Owner.Player!);
    }
}
