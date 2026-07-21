using System.Reflection;
using Alchemist.AlchemistCode.Potions;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;

namespace Alchemist.AlchemistCode.Relics;

public sealed class BrewRestSiteOption : RestSiteOption
{
    // The potion-reward overlay does not hide the rest-site choice buttons. A vanilla option hides them.
    // Therefore this code fades them out with the private screen of the room
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

        await RewardsCmd.OfferCustom(Owner, [CreateBrewReward()]);
        return true;
    }

    // Brew-only potions are not in the potion pool, so the default reward can never roll them.
    // A 30% roll offers one of them instead, minus any the player already holds. Across the
    // 3 to 7 Brews of a typical run, this shows at least one Brew-only potion in most runs
    private const float BrewOnlyChance = 0.3f;

    private PotionReward CreateBrewReward()
    {
        var rng = Owner.PlayerRng.Rewards;
        var exclusives = new PotionModel[]
        {
            ModelDb.Potion<QuicksilverDraught>(),
            ModelDb.Potion<Soporific>(),
            ModelDb.Potion<Alkahest>(),
        }.Where(p => Owner.Potions.All(held => held.Id != p.Id)).ToList();
        if (exclusives.Count > 0 && rng.NextFloat() < BrewOnlyChance)
            return new PotionReward(rng.NextItem(exclusives)!.ToMutable(), Owner);
        return new PotionReward(Owner);
    }

    private static int GetRemovableCardCount(Player player)
    {
        return PileType.Deck.GetPile(player).Cards.Count(c => c.IsRemovable);
    }
}
