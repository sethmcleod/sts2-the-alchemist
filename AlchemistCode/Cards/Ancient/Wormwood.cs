using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Ancient;

[CardTheme(CardTheme.Poison)]
public class Wormwood : AlchemistCard
{
    private const int Hits = 2;

    public Wormwood() : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
        WithCalculatedDamage(4, static (card, _) => Dose(card), ValueProp.Move, 2);
        WithKeyword(AlchemistKeywords.Laced);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits, vfx: HitVfx("vfx/vfx_sandy_impact"))
            .WithAttackerAnim("Cast", Owner.Character.CastAnimDelay)
            .Execute(choiceContext);
    }
}
