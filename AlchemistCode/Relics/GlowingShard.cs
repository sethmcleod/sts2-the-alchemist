using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Relics;

// The extra Poison trigger lives in PoisonPatches, which reads this relic on the poisoned enemy's opponents
public class GlowingShard : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };
}
