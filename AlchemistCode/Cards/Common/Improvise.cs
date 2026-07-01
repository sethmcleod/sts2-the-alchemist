using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Improvise : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Improvise() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(5, 2);
        WithBlock(4, 2);
        WithPower<RegenPower>(2, 1);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await CommonActions.CardBlock(this, play);
        if (IsReduced)
            await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
    }
}
