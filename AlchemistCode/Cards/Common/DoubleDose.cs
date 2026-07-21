using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class DoubleDose : AlchemistCard
{
    public DoubleDose() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 1);
        WithPower<PoisonPower>(1, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        for (var i = 0; i < 2; i++)
        {
            await CommonActions.CardAttack(this, play).Execute(choiceContext);
            if (play.Target is { IsAlive: true })
                await CommonActions.Apply<PoisonPower>(choiceContext, this, play);
        }
    }
}
