using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

public class PanaceaPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Tracks whether the owner is currently at/below 50% HP, so we fire only on the crossing
    // (above → at-or-below), not on every hit taken while already low.
    private bool _below;

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner)) _below = Owner.CurrentHp * 2 <= Owner.MaxHp;
        return Task.CompletedTask;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner) return;
        var nowBelow = Owner.CurrentHp * 2 <= Owner.MaxHp;
        var crossed = nowBelow && !_below;
        _below = nowBelow;
        if (!crossed) return;

        Flash();
        var allies = CombatState.GetTeammatesOf(Owner).Append(Owner)
            .Where(c => c is { IsAlive: true, IsPlayer: true }).Distinct();
        foreach (var ally in allies)
        {
            await PowerCmd.Apply<RegenPower>(new ThrowingPlayerChoiceContext(), ally, Amount, Owner, null);
            await CreatureCmd.Heal(ally, Amount);
        }
    }
}
