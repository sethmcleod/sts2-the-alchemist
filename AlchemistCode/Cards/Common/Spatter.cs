using System.Linq;
using Alchemist.AlchemistCode.Compat;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Spatter : AlchemistCard
{
    public Spatter() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8, 3);
        WithVar("Splash", 3, 2);
        WithVar("SplashPoison", 1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash"),
            sfx: "event:/sfx/characters/attack_fire").Execute(choiceContext);
        if (CombatState == null || play.Target == null) return;

        // Unpowered, the way Pass It On and White Heat deal their secondary damage, so the splash is
        // exactly the number on the card rather than a second helping of Strength
        var splash = DynamicVars["Splash"].IntValue;
        var poison = DynamicVars["SplashPoison"].IntValue;
        foreach (var enemy in CombatState.HittableEnemies.Where(e => e != play.Target && e.IsAlive).ToList())
        {
            await GameCompat.Damage(choiceContext, enemy, splash, ValueProp.Unpowered, Owner.Creature, this, null);
            if (!enemy.IsAlive) continue;
            PoisonSplash(enemy);
            await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, poison, Owner.Creature, this);
        }
    }
}
