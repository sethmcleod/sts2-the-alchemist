using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

public class TransmutationPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // originalCost <= 0 skips free and X-cost cards
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Enchantment == null || originalCost <= 0) return false;
        modifiedCost = originalCost - 1;
        return true;
    }
}
