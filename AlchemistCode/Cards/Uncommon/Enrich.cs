using Alchemist.AlchemistCode;
using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Enrich : AlchemistCard
{
    public Enrich() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await Infusion.InfuseChosen(choiceContext, this, PileType.Draw, 2);
        await CardPileCmd.Draw(choiceContext, 2, Owner);
    }
}
