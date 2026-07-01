using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class ConjunctionPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        if (!Owner.HasPower<PoisonPower>() || !Owner.HasPower<RegenPower>()) return;
        Flash();
        await PlayerCmd.GainEnergy(1, Owner.Player!);
        if (Amount >= 2)
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 1, Owner.Player!);
    }
}
