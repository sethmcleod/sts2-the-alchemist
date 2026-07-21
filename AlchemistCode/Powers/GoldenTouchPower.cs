using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

public class GoldenTouchPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // originalCost <= 0 skips free and X-cost cards
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Enchantment == null || originalCost <= 0) return false;
        // Each stack reduces the cost by 1, never below 0
        modifiedCost = Math.Max(0, originalCost - Amount);
        return true;
    }
}
