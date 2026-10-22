using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.ReshuffledSpire;

[CardTheme(CardTheme.Poison)]
public class LetItSettle : AlchemistReshuffledCard
{
    protected internal override bool PlaysCastAnimation => false;

    public LetItSettle() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(9, 3);
        WithTips(_ => new[] { HoverTipFactory.FromCard<Dregs>() });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (CombatState is not { } combat) return;
        var dregs = combat.CreateCard<Dregs>(Owner);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(dregs, PileType.Draw, Owner, CardPilePosition.Random));
    }
}
