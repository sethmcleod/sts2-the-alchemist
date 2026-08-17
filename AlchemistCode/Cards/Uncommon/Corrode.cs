using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Corrode : AlchemistCard
{
    public Corrode() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithPower<WeakPower>(1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var dose = Owner.Creature.GetPowerAmount<PoisonPower>();
        foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
        {
            if (dose > 0)
            {
                PoisonSplash(enemy);
                await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, dose, Owner.Creature, this);
            }
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        }
        if (dose > 0)
            await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, -dose, Owner.Creature, this);
    }
}
