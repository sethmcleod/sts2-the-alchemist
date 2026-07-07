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
        WithVar("draw", 1, 1); // draw 1 (2)
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Infuse) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        Infusion.InfuseRandomFromPile(Owner, PileType.Draw, 2);
        await CardPileCmd.Draw(choiceContext, DynamicVars["draw"].IntValue, Owner);
    }
}
