using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Tinge : AlchemistCard
{
    public Tinge() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(3, 1);
        WithPower<PoisonPower>(2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.Apply<PoisonPower>(choiceContext, this, play);
    }
}
