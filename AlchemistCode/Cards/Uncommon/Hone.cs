using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Hone : AlchemistCard
{
    public Hone() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(5, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        var hand = PileType.Hand.GetPile(Owner).Cards
            .Where(c => !c.IsUpgraded).ToList();
        if (hand.Count > 0)
            CardCmd.Upgrade(hand, CardPreviewStyle.HorizontalLayout);
    }
}
