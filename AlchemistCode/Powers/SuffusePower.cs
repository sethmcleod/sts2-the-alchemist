using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Powers;

public class SuffusePower : AlchemistPower
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
        var rng = Owner.Player!.RunState.Rng.CombatPotionGeneration;
        var allies = combatState.GetTeammatesOf(Owner).Append(Owner)
            .Where(c => c is { IsAlive: true, IsPlayer: true }).Distinct();
        foreach (var ally in allies)
        {
            var potion = PotionFactory.CreateRandomPotionInCombat(ally.Player!, rng).ToMutable();
            await PotionCmd.TryToProcure(potion, ally.Player!);
        }
    }
}
