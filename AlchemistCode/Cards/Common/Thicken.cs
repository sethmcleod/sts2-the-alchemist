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
        WithBlock(4, 3);
        WithTips(_ => Mixing.MixTips());
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(CardKeyword.Retain) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        var mix = await Mixing.Choose(choiceContext, Owner);
        if (mix == null) return;
        await CardPileCmd.AddGeneratedCardToCombat(mix, PileType.Hand, Owner);
        CardCmd.ApplyKeyword(mix, CardKeyword.Retain);
    }
}
