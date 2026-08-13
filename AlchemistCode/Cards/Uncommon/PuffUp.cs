using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class PuffUp : AlchemistCard
{
    protected override int FermentPeak => 3;

    public PuffUp() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCalculatedBlock(10, static (card, _) =>
                (card.IsUpgraded ? 6m : 4m) * ((AlchemistCard)card).FermentTurns,
            ValueProp.Move, 0, 0);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}
