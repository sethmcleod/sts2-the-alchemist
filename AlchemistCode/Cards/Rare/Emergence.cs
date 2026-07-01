using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Emergence : AlchemistCard
{
    public Emergence() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var selected = (await CardSelectCmd.FromCombatPile(
            choiceContext, PileType.Exhaust.GetPile(Owner), Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (selected == null) return;
        await CardPileCmd.Add(selected, PileType.Hand);
        if (IsUpgraded && !selected.IsUpgraded)
            CardCmd.Upgrade(selected);
    }
}
