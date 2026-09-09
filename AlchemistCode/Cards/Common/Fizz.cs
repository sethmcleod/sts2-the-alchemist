using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Mix)]
public class Fizz : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Fizz() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithUpgradingCardTip<Token.BurstingMix>();
        WithUpgradingCardTip<Token.ZestyMix>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await Mixing.CreateOne<Token.BurstingMix>(choiceContext, Owner, IsUpgraded, this);
        await Mixing.CreateOne<Token.ZestyMix>(choiceContext, Owner, IsUpgraded, this);
    }
}
