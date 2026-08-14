using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Cards.Token;
using Alchemist.AlchemistCode.Commands;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Eureka : AlchemistCard
{
    public Eureka() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCards(2, 1);
        WithVar("transforms", 2, 0);
        WithUpgradingCardTip<Distillate>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        // The shared helper carries the upgrade onto the Distillate, which the description promises
        for (var i = 0; i < DynamicVars["transforms"].IntValue; i++)
            await AlchemistCardCmd.TransformFromHand<Distillate>(choiceContext, this);
    }
}
