using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Cards.Uncommon;

namespace Alchemist.AlchemistCode.Powers;

public class DrainDryStrengthDownPower : CustomTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<DrainDry>();
    protected override bool IsPositive => false;
}
