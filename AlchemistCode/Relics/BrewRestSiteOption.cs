using System.Reflection;
using Alchemist.AlchemistCode.Potions;
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

        await RewardsCmd.OfferCustom(Owner, [CreateBrewReward()]);
        return true;
    }

    // Brew-only potions are outside the potion pool, so the default reward can never roll them. This
    // roll offers one instead, minus any the player already holds
    private PotionReward CreateBrewReward()
    {
        var rng = Owner.PlayerRng.Rewards;
        var exclusives = new PotionModel[]
        {
            ModelDb.Potion<QuicksilverDraught>(),
            ModelDb.Potion<Soporific>(),
            ModelDb.Potion<Alkahest>(),
        }.Where(p => Owner.Potions.All(held => held.Id != p.Id)).ToList();
        if (exclusives.Count > 0 && rng.NextFloat() < Config.AlchemistModConfig.BrewPotionChance / 100f)
            return new PotionReward(rng.NextItem(exclusives)!.ToMutable(), Owner);
        return new PotionReward(Owner);
    }
}
