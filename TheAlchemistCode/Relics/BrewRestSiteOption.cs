using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace TheAlchemist.TheAlchemistCode.Relics;

public sealed class BrewRestSiteOption : RestSiteOption
{
    public override string OptionId => "BREW";

    public override LocString Description
    {
        get
        {
            if (IsEnabled)
            {
                LocString locString = new LocString("rest_site_ui", "OPTION_" + OptionId + ".description");
                locString.Add("Cards", 1m);
                return locString;
            }
            return new LocString("rest_site_ui", "OPTION_" + OptionId + ".descriptionDisabled");
        }
    }

    public override bool IsEnabled => GetRemovableCardCount(Owner) >= 1;

    public BrewRestSiteOption(Player owner) : base(owner) { }

    public override async Task<bool> OnSelect()
    {
        CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        if (!selected.Any())
            return false;

        foreach (var card in selected)
            await CardPileCmd.RemoveFromDeck(card);

        var potion = PotionFactory.CreateRandomPotionOutOfCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable();
        await PotionCmd.TryToProcure(potion, Owner);
        return true;
    }

    private static int GetRemovableCardCount(Player player)
    {
        return PileType.Deck.GetPile(player).Cards.Count(c => c.IsRemovable);
    }
}
