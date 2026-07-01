using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Falter : AlchemistCard
{
    protected override bool IsMettleCard => true;

    public Falter() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(7, 2);
        WithPower<RegenPower>(3, 1); // Mettle bonus
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Mettle) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (IsReduced)
            await CommonActions.ApplySelf<RegenPower>(choiceContext, this);
    }
}
