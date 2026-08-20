using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.RestSite;
using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

using MegaCrit.Sts2.Core.Entities.Creatures;

using MegaCrit.Sts2.Core.Models;

using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

// The upgraded starter, so it belongs in the event pool with the base characters' upgraded starters
// rather than the character pool, which would make it drop as a ninth relic
[Pool(typeof(EventRelicPool))]
public class GildedKit : AlchemistRelic
{
    private const int PotionSlots = 1;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { AlchemistTips.Brew };

    public override async Task AfterObtained()
    {
        await PlayerCmd.GainMaxPotionCount(PotionSlots, Owner);
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        if (options.Any(o => o is BrewRestSiteOption)) return false;
        options.Add(new BrewRestSiteOption(player));
        return true;
    }
}
