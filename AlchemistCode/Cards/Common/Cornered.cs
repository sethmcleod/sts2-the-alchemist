using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Cornered : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Cornered() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(9, 5, static (card, _) => ((AlchemistCard)card).IsReduced ? 1 : 0, ValueProp.Move, 3, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (!IsReduced)
            await LoseHp(choiceContext, 5);
    }
}
