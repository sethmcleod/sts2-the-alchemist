using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TheAlchemist.TheAlchemistCode.Cards.Basic;

public class DefendAlchemist : TheAlchemistCard
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
