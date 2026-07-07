using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Winnow : AlchemistCard
{
    public Winnow() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(7, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        var drawPile = PileType.Draw.GetPile(Owner);
        var cardOptions = drawPile.Cards.ToList()
            .StableShuffle(Owner.RunState.Rng.CombatCardSelection)
            .Take(3);
        var selected = (await CardSelectCmd.FromCombatPile(
            choiceContext, drawPile, Owner,
            new CardSelectorPrefs(new LocString("card_selection", "CHOOSE_CARD_HEADER"), 1),
            c => cardOptions.Contains(c))).FirstOrDefault();
        if (selected != null)
            await CardPileCmd.Add(selected, PileType.Hand);
    }
}
