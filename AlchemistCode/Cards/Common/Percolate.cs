using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Percolate : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Percolate() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar("cards", 2, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Distillate>();
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play) =>
        AlchemistCardCmd.GiveCard<Distillate>(this, DynamicVars["cards"].IntValue);
}
