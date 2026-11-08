using Alchemist.AlchemistCode.Cards.Rare;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

public class WaterDownPower : CustomTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<WaterDown>();
    protected override bool IsPositive => false;
}
