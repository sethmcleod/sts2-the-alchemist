using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class StrikeAlchemist : AlchemistCard
{
    public StrikeAlchemist() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithTags(CardTag.Strike);
        WithDamage(6, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
    }
}
