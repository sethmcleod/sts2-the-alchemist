using System.Linq;
using Alchemist.AlchemistCode.Compat;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace Alchemist.AlchemistCode.Relics;


[Pool(typeof(EventRelicPool))]
public class RadiantFlask : FlaskRelic
{
    private const int PotionSlots = 1;

    protected override int Antitoxin => 10;
    protected override bool UpgradedMix => true;

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
