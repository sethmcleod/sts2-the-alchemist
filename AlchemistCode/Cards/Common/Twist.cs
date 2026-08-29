using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Mix)]
public class Twist : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Twist() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(1, 0);
        WithTips(card => AlchemistTips.MixSingle(
            card.IsUpgraded ? "ALCHEMIST-ZESTY_MIX_PLUS" : "ALCHEMIST-ZESTY_MIX", "mix_zesty"));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        await Mixing.CreateOne<Token.ZestyMix>(choiceContext, Owner, IsUpgraded);
    }
}
