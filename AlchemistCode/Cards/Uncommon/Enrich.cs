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
        WithVar("draw", 1, 1);
        WithTips(_ => Infusion.InfuseTips());
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Infuse first, so the draw can find the infused cards
        await Infusion.InfuseChosen(choiceContext, this, PileType.Draw, 0, 2);
        await CardPileCmd.Draw(choiceContext, DynamicVars["draw"].IntValue, Owner);
    }
}
