using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class DefendAlchemist : AlchemistCard
{
    public DefendAlchemist() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithTags(CardTag.Defend);
        WithBlock(5, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}
