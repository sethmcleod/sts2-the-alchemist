using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Spatter : AlchemistCard
{
    public Spatter() : base(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
        WithDamage(3, 1);
        WithVar("hits", 4, 0);
        WithVar("hitPoison", 1, 0);
        WithPower<PoisonPower>(2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        for (var i = 0; i < DynamicVars["hits"].IntValue; i++)
        {
            var enemy = Owner.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies);
            if (enemy == null) break;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, play)
                .Targeting(enemy)
                .Execute(choiceContext);
            if (enemy.IsAlive)
                await PowerCmd.Apply<PoisonPower>(choiceContext, enemy,
                    DynamicVars["hitPoison"].IntValue, Owner.Creature, this);
        }
        await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
    }
}
