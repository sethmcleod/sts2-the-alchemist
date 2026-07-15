using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;

namespace Alchemist.AlchemistCode.Relics;

public sealed class BrewRestSiteOption : RestSiteOption
{
    // The potion-reward overlay doesn't hide the rest-site choice buttons the way vanilla
    // options do, so we fade them out ourselves via the room's private screen
    private static readonly FieldInfo ChoicesScreenField =
        typeof(NRestSiteRoom).GetField("_choicesScreen", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public override string OptionId => "BREW";

    public override LocString Description
    {
        get
        {
            if (IsEnabled)
                return new LocString("rest_site_ui", "OPTION_" + OptionId + ".description");
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

        var restSiteRoom = NRestSiteRoom.Instance;
        if (restSiteRoom != null)
        {
            var choicesScreen = ChoicesScreenField.GetValue(restSiteRoom) as Control;
            if (choicesScreen != null)
            {
                var tween = restSiteRoom.CreateTween();
                tween.TweenProperty(choicesScreen, "modulate:a", 0f, 0.5);
            }
        }

        var potionReward = new PotionReward(Owner);
        await RewardsCmd.OfferCustom(Owner, [potionReward]);
        return true;
    }

    private static int GetRemovableCardCount(Player player)
    {
        return PileType.Deck.GetPile(player).Cards.Count(c => c.IsRemovable);
    }
}
