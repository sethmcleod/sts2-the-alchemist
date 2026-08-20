using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Basic;

[CardTheme(CardTheme.Poison)]
public class Jab : AlchemistCard
{
    public Jab() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, static (card, _) => Dose(card), ValueProp.Move, 3);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
    }
}
