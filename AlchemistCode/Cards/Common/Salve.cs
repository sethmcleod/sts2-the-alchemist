using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Salve : AlchemistCard
{
    public Salve() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(4, 2);
        WithCards(1, 0);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await CommonActions.Draw(this, choiceContext);
        await Infusion.InfuseChosen(choiceContext, this, PileType.Hand, 1);
    }
}
