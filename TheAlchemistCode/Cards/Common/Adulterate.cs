using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheAlchemist.TheAlchemistCode.Cards.Token;
using TheAlchemist.TheAlchemistCode.Commands;

namespace TheAlchemist.TheAlchemistCode.Cards.Common;

public class Adulterate : TheAlchemistCard
{
    public Adulterate() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithUpgradingCardTip<Dross>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await AlchemistCardCmd.TransformFromHand<Dross>(choiceContext, this);
    }
}
