using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Alchemist.AlchemistCode.Cards.Token;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Sublimate : AlchemistCard
{
    public Sublimate() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeyword(CardKeyword.Exhaust);
        WithUpgradingCardTip<Distillate>();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Min 0, max unbounded, i.e. "any number"
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 0, 999999999),
            null, this);
        foreach (var card in selected)
        {
            var distillate = CombatState!.CreateCard<Distillate>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(distillate);
            await CardCmd.Transform(card, distillate);
        }
    }
}
