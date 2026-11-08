using Alchemist.AlchemistCode;
using System.Collections.Generic;
using Alchemist.AlchemistCode.Character;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Relics;

// Marker only: AlchemistCard.FermentTurns applies the floor
public class MotherOfVinegar : AlchemistRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.FermentRef };
}
