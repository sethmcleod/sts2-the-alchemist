using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Prime : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Prime() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithBlock(4, 2); // Gambit: 4 (6) Block
        WithTips(_ => new[]
        {
            HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse),
            HoverTipFactory.FromKeyword(AlchemistKeywords.Gambit),
        });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 1);
        if (IsReduced) // Gambit
            await CommonActions.CardBlock(this, play);
    }
}
