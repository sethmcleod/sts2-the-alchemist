using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Mix)]
public class Twist : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Twist() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(1, 0);
        WithUpgradingCardTip<Token.ZestyMix>(static (tip, _) => tip.AddKeyword(CardKeyword.Retain));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(CardKeyword.Retain) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        var mix = await Mixing.CreateOne<Token.ZestyMix>(choiceContext, Owner, IsUpgraded);
        if (mix == null) return;
        CardCmd.ApplyKeyword(mix, CardKeyword.Retain);
    }
}
