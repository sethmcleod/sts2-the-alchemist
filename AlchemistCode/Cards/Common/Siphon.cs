using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Siphon : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Siphon() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7, 2);
        WithCards(2, 0);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        var draw = DynamicVars["Cards"].IntValue + (IsReduced ? 1 : 0);
        await CardPileCmd.Draw(choiceContext, draw, Owner);
    }
}
