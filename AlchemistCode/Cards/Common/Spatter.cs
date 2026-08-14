using Alchemist.AlchemistCode.Compat;
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
        WithDamage(2, 1);
        WithVar("hits", 4, 0);
    }

    // No Poison of its own: Laced is what puts Poison on this, once per hit, and a per-hit apply here
    // would also trigger Harden four times. Four small hits rather than Sword Boomerang's three
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        for (var i = 0; i < DynamicVars["hits"].IntValue; i++)
        {
            var enemy = Owner.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies);
            if (enemy == null) break;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, play)
                .WithHitFx(HitVfx("vfx/vfx_attack_slash"), null, "dagger_throw.mp3")
                .Targeting(enemy)
                .Execute(choiceContext);
        }
    }
}
