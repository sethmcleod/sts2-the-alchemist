using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class AccretionPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        Flash();
        var regen = Amount;
        // Gambit: gain a flat 1 extra Regen (does NOT scale with upgrade) while at or below 50% HP.
        if (Owner.CurrentHp * 2 <= Owner.MaxHp)
            regen += 1;
        await PowerCmd.Apply<RegenPower>(new ThrowingPlayerChoiceContext(), Owner, regen, Owner, null);
    }
}
