using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Mix)]
public class Thicken : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Thicken() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(4, 1);
        WithTips(card => AlchemistTips.MixSingle(
            card.IsUpgraded ? "ALCHEMIST-SYRUPY_MIX_PLUS" : "ALCHEMIST-SYRUPY_MIX", "mix_syrupy"));
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(CardKeyword.Retain) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        var mix = await Mixing.CreateOne<Token.SyrupyMix>(choiceContext, Owner, IsUpgraded);
        if (mix == null) return;
        CardCmd.ApplyKeyword(mix, CardKeyword.Retain);
    }
}
