
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheAlchemist.TheAlchemistCode.Powers;

// Amount=1: damage random enemy on heal. Amount>=2: damage ALL enemies on heal.
public class InversionPower : TheAlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta <= 0) return;
        Flash();
        if (Amount >= 2)
        {
            var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();
            foreach (var enemy in enemies)
                await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), enemy, delta,
                    ValueProp.Move, Owner, null);
        }
        else
        {
            var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();
            if (enemies.Count > 0)
            {
                var target = enemies[CombatState.RunState.Rng.CombatCardGeneration.NextInt(enemies.Count)];
                await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), target, delta,
                    ValueProp.Move, Owner, null);
            }
        }
    }
}
