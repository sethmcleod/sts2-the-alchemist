using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Adulterate : AlchemistCard
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
