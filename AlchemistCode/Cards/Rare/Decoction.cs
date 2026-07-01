using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Potions;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Decoction : AlchemistCard
{
    public Decoction() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            null, this);
        foreach (var card in selected)
            await CardCmd.Exhaust(choiceContext, card);

        await PotionCmd.TryToProcure<FoulPotion>(Owner);
    }
}
