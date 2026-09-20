using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class Backfire : AlchemistCard
{
    public Backfire() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 2);
        WithPower<WeakPower>(1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
        if (Dose(this) <= 0 || play.Target is not { IsAlive: true } target) return;
        await PowerCmd.Apply<WeakPower>(choiceContext, target, DynamicVars.Weak.IntValue, Owner.Creature, this);
    }
}
