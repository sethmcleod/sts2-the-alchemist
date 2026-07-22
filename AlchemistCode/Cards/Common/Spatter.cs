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
        WithPower<PoisonPower>(2, 0);
    }

    // The many small hits make this a strong Laced target: each unblocked hit then applies the Poison. The
    // card does not apply its own per-hit Poison, which would also make each hit trigger a Poison-on-apply
    // effect, such as Sediment, a second time
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
        }
        await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
    }
}
