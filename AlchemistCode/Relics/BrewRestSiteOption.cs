using System.Reflection;
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
    // The potion-reward overlay leaves the rest-site choice buttons up, where a vanilla option hides
    // them, so fade them out through the room's private screen
    private static readonly FieldInfo ChoicesScreenField =
        typeof(NRestSiteRoom).GetField("_choicesScreen", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public const string BrewOptionId = "BREW";

    public override string OptionId => BrewOptionId;

    public override LocString Description =>
        new LocString("rest_site_ui", "OPTION_" + OptionId + ".description");

    public override bool IsEnabled => true;

    public override IEnumerable<string> AssetPaths => Array.Empty<string>();

    public BrewRestSiteOption(Player owner) : base(owner) { }

    public override async Task<bool> OnSelect()
    {
        // OnSelect runs on EVERY client when any player brews (RestSiteSynchronizer replays remote
        // choices), so the fade must only touch the brewing player's own screen. Other clients get
        // no UI work here; OfferCustom below is a no-op for non-local players by design
        if (LocalContext.IsMe(Owner) && NRestSiteRoom.Instance is { } restSiteRoom)
        {
            var choicesScreen = ChoicesScreenField.GetValue(restSiteRoom) as Control;
            if (choicesScreen != null)
            {
                var tween = restSiteRoom.CreateTween();
                tween.TweenProperty(choicesScreen, "modulate:a", 0f, 0.5);
            }
        }

        var rewards = new List<Reward>();
        if (CreateBrewReward() is { } reward) rewards.Add(reward);
        await RewardsCmd.OfferCustom(Owner, rewards);
        return true;
    }

    // Every Brew-only potion. Brew is their ONLY source, so this list is the whole set and must be
    // updated whenever one is added; nothing else enumerates IBrewOnly at runtime
    private PotionModel[] BrewOnly() =>
    [
        ModelDb.Potion<QuicksilverDraught>(),
        ModelDb.Potion<Anodyne>(),
        ModelDb.Potion<Alkahest>(),
        ModelDb.Potion<Sampler>(),
        ModelDb.Potion<Solvent>(),
        ModelDb.Potion<Decoction>(),
        ModelDb.Potion<StarterCulture>(),
    ];

    // Brew offers nothing else. Duplicates are filtered out, and holding the whole set falls back to a
    // normal potion so the rest site is never a dead option
    // Tracks what this Brew already offered, so a double Brew cannot roll the same potion twice
    private readonly List<ModelId> _offered = new();

    private Reward CreateBrewReward()
    {
        var rng = Owner.PlayerRng.Rewards;
        var exclusives = BrewOnly()
            .Where(p => Owner.Potions.All(held => held.Id != p.Id) && !_offered.Contains(p.Id))
            .ToList();
        if (exclusives.Count == 0) return new PotionReward(Owner);

        // Choose 1 of 3: the options ride in a linked set, so claiming one clears the others.
        // Everything shown counts as offered, which keeps a second Brew in the same visit fresh
        List<Reward> options = new();
        for (var i = 0; i < 3 && exclusives.Count > 0; i++)
        {
            var pick = rng.NextItem(exclusives)!;
            exclusives.Remove(pick);
            _offered.Add(pick.Id);
            options.Add(new PotionReward(pick.ToMutable(), Owner));
        }
        return options.Count == 1
            ? options[0]
            : new CustomLinkedRewardSet(options, Owner, LinkedRewardType.Exclusive);
    }
}
