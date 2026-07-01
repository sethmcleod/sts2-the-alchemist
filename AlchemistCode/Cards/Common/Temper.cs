using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Temper : AlchemistCard
{
    public Temper() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6, 0);
        WithUpgradingCardTip<Distillate>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await AlchemistCardCmd.TransformFromHand<Distillate>(choiceContext, this);
    }
}
