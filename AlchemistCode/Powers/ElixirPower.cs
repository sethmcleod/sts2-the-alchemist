using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;

namespace Alchemist.AlchemistCode.Powers;

public class ElixirPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;

        Flash();
        // AlchemistPotionPool filters IBrewOnly out of every generation path, so this cannot roll a
        // Brew-only potion
        for (var i = 0; i < Amount; i++)
            await PotionCmd.TryToProcure(
                PotionFactory.CreateRandomPotionInCombat(Owner.Player!,
                    Owner.Player!.RunState.Rng.CombatPotionGeneration).ToMutable(), Owner.Player!);
    }
}
