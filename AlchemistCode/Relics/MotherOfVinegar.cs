using Alchemist.AlchemistCode;
using System.Collections.Generic;
using Alchemist.AlchemistCode.Character;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Relics;

// A pure marker: AlchemistCard.FermentTurns adds the floor itself, so combat start, the reset
// after a play, and cards generated mid-combat all begin at 1 with no hook here
public class MotherOfVinegar : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.FermentRef };
}
