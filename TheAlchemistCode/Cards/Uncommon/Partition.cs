using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace TheAlchemist.TheAlchemistCode.Cards.Uncommon;

public class Partition : TheAlchemistCard
{
    public Partition() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithVar("totalDamage", 32, 16);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0) return;
        var total = DynamicVars["totalDamage"].IntValue;
        var perEnemy = total / enemies.Count;
        var remainder = total % enemies.Count;
        foreach (var enemy in enemies)
        {
            var damage = perEnemy + (remainder > 0 ? 1 : 0);
            remainder--;
            await CreatureCmd.Damage(choiceContext, enemy, damage, ValueProp.Move, Owner.Creature, this);
        }
    }
}
