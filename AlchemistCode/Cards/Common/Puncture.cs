using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class Puncture : AlchemistCard
{
    public Puncture() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 1);
        WithVar(new HitCountVar(2));
        WithPower<VulnerablePower>(2, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow => AllEnemiesPoisoned;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, DynamicVars.HitCount.IntValue, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
        if (play.Target is { IsAlive: true } target && Poisoned(target))
            await CommonActions.Apply<VulnerablePower>(choiceContext, this, play);
    }
}
