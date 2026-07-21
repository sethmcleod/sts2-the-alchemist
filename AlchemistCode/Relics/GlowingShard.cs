using MegaCrit.Sts2.Core.Entities.Relics;

namespace Alchemist.AlchemistCode.Relics;

// Passive: while you hold this, an enemy's Poison triggers one additional time. The extra trigger is added
// in PoisonPatches, which reads this relic on the poisoned enemy's opponents.
public class GlowingShard : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}
