using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Basic;

// The starter's payoff half. Dose puts the Poison on you; Jab is the first card that reads it, so
// turn 1 of a fresh run already shows the loop: dose, then hit harder for as long as it lasts
[CardTheme(CardTheme.Poison)]
public class Jab : AlchemistCard
{
    public Jab() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(5, static (card, _) => Dose(card), ValueProp.Move, 3);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
    }
}
