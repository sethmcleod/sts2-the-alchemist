using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using System.Linq;
using System.Reflection;
using Alchemist.AlchemistCode.Extensions;
using Alchemist.AlchemistCode.Potions;
using BaseLib.Common.Rewards.LinkedRewardSet;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
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
    // The reward overlay leaves the rest-site buttons up, so hide them through the private screen
    private static readonly FieldInfo ChoicesScreenField =
        typeof(NRestSiteRoom).GetField("_choicesScreen", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public const string BrewOptionId = "BREW";

    public override string OptionId => BrewOptionId;

    public override LocString Description =>
        new LocString("rest_site_ui",
            "OPTION_" + OptionId + (Owner.RunState.IsBlitz() ? ".descriptionBlitz" : ".description"));

    public override bool IsEnabled => true;

    public override IEnumerable<string> AssetPaths => Array.Empty<string>();

    public BrewRestSiteOption(Player owner) : base(owner) { }

    public override async Task<bool> OnSelect()
    {
        // OnSelect runs on every client (RestSiteSynchronizer replays choices), so touch only the local screen
        if (LocalContext.IsMe(Owner) && NRestSiteRoom.Instance is { } restSiteRoom)
        {
            var choicesScreen = ChoicesScreenField.GetValue(restSiteRoom) as Control;
            if (choicesScreen != null)
            {
                var tween = restSiteRoom.CreateTween();
                tween.TweenProperty(choicesScreen, "modulate:a", 0f, 0.5);
            }
        }

        // Roll before OfferCustom: it blocks only on the brewing client, so a later draw desyncs
        UpgradeRandomCard();
        var rewards = new List<Reward>();
        if (CreateBrewReward() is { } reward) rewards.Add(reward);
        await RewardsCmd.OfferCustom(Owner, rewards);
        return true;
    }

    private void UpgradeRandomCard()
    {
        var card = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c is { IsUpgradable: true }).ToList()
            .StableShuffle(Owner.RunState.Rng.Niche).FirstOrDefault();
        if (card != null) CardCmd.Upgrade(card);
    }

    // Must list every IBrewOnly potion: nothing else enumerates them
    private PotionModel[] BrewOnly() =>
    [
        ModelDb.Potion<Quicksilver>(),
        ModelDb.Potion<Anodyne>(),
        ModelDb.Potion<Alkahest>(),
        ModelDb.Potion<Solvent>(),
        ModelDb.Potion<Reduction>(),
        ModelDb.Potion<Sampler>(),
        ModelDb.Potion<FreshCutting>(),
        ModelDb.Potion<GoldLeaf>(),
    ];

    private readonly List<ModelId> _offered = new();

    private Reward CreateBrewReward()
    {
        var rng = Owner.PlayerRng.Rewards;
        var exclusives = BrewOnly()
            .Where(p => Owner.Potions.All(held => held.Id != p.Id) && !_offered.Contains(p.Id))
            .ToList();
        if (exclusives.Count == 0) return new PotionReward(Owner);

        List<Reward> options = new();
        for (var i = 0; i < 3 && exclusives.Count > 0; i++)
        {
            var pick = rng.NextItem(exclusives)!;
            exclusives.Remove(pick);
            _offered.Add(pick.Id);
            Analytics.RunCounters.Tally(Owner, Analytics.RunCounters.BrewOffered + Analytics.RunCounters.Label(pick));
            options.Add(new PotionReward(pick.ToMutable(), Owner));
        }
        return options.Count == 1
            ? options[0]
            : new CustomLinkedRewardSet(options, Owner, LinkedRewardType.Exclusive);
    }
}
