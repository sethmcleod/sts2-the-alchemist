using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Cards.Uncommon;

namespace Alchemist.AlchemistCode.Powers;

public class DepleteStrengthDownPower : CustomTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<Deplete>();
    protected override bool IsPositive => false;
}
