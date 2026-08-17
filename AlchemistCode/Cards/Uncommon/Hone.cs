using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Infuse)]
public class Hone : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Hone() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithVar("Infuse", 1, 1);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = PileType.Hand.GetPile(Owner).Cards
            .Where(c => !c.IsUpgraded).ToList();
        if (hand.Count > 0)
            CardCmd.Upgrade(hand, CardPreviewStyle.HorizontalLayout);
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, DynamicVars["Infuse"].IntValue);
    }
}
