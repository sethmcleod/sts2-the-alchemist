using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

// The multi-hit reader: two hits, each of them reads the dose, so a dose of 3 is +6 on one card
[CardTheme(CardTheme.Poison)]
public class Sting : AlchemistCard
{
    public Sting() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(3, static (card, _) => Dose(card), ValueProp.Move, 1);
        WithTip(typeof(PoisonPower));
    }

    private const int Hits = 2;

    protected override bool ConditionalGlow => Dose(this) > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits, vfx: HitVfx("vfx/vfx_dramatic_stab"))
            .Execute(choiceContext);
    }
}
