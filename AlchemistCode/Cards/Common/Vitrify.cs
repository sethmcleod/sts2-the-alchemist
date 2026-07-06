using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Vitrify : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Vitrify() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(3, 1); // 3 (4) Block, gained twice — an infused Nimble bonus lands on each gain
        _ = WithTips(_ => [
            HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit),
            HoverTipFactory.FromCard<Effluvium>(),
        ]);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.CardBlock(this, play); // twice: two separate Block gains
        if (IsReduced) // Gambit
            await AlchemistCardCmd.GiveCard<Effluvium>(this);
    }
}
