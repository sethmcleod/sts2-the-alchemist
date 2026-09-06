using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Mix)]
public class AgedBatch : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public AgedBatch() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(1, 0);
        WithUpgradingCardTip<Token.BurstingMix>();
        WithUpgradingCardTip<Token.SyrupyMix>();
        WithUpgradingCardTip<Token.ZestyMix>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        var mix = await Mixing.Choose(choiceContext, Owner, upgraded: IsUpgraded, source: this);
        if (mix == null) return;
        mix.RemoveKeyword(CardKeyword.Exhaust);
        await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, Owner);
    }
}
