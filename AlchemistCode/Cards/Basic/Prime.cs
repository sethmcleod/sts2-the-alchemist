using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Prime : AlchemistCard
{
    public Prime() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        WithDamage(7, 3);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        await Infusion.InfuseChosen(choiceContext, this, PileType.Draw, 1);
    }
}
