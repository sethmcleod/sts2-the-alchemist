using MegaCrit.Sts2.Core.Entities.Relics;

namespace Alchemist.AlchemistCode.Relics;

// The extra Poison trigger lives in PoisonPatches, which reads this relic on the poisoned enemy's opponents
public class GlowingShard : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}
