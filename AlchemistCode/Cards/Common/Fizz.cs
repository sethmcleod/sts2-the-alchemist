using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Cards.Variables;
using BaseLib.Extensions;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Mix)]
public class Fizz : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Fizz() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithVar(new ScryVar(1).WithUpgrade(1));
        WithUpgradingCardTip<Token.BurstingMix>();
        WithUpgradingCardTip<Token.ZestyMix>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await Scrying.Execute(choiceContext, this);
        await Mixing.CreateOne<Token.BurstingMix>(choiceContext, Owner, IsUpgraded, this);
        await Mixing.CreateOne<Token.ZestyMix>(choiceContext, Owner, IsUpgraded, this);
    }
}
