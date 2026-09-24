using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class Spit : AlchemistCard
{
    public Spit() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 2);
        WithPower<PoisonPower>(3, 1);
        WithVar("SelfPoison", 1, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
        if (play.Target is { IsAlive: true } target)
        {
            PoisonSplash(target);
            await CommonActions.Apply<PoisonPower>(choiceContext, this, play);
        }
        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature,
            DynamicVars["SelfPoison"].IntValue, Owner.Creature, this);
    }
}
