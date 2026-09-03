using System.Linq;
using Alchemist.AlchemistCode.Compat;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace Alchemist.AlchemistCode.Relics;

// The upgraded starter, so it belongs in the event pool with the base characters' upgraded starters
// rather than the character pool, which would make it drop as a ninth relic
[Pool(typeof(EventRelicPool))]
public class RadiantFlask : FlaskRelic
{
    private const int PotionSlots = 1;

    protected override int Antitoxin => 6;
    protected override int Dose => 6;

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        await PlayerCmd.GainMaxPotionCount(PotionSlots, Owner);
        var rares = GameCompat.GetPotionOptions(Owner)
            .Where(p => p.Rarity == PotionRarity.Rare).ToList();
        if (Owner.PlayerRng.Rewards.NextItem(rares) is not { } rare) return;
        await PotionCmd.TryToProcure(rare.ToMutable(), Owner);
    }
}
