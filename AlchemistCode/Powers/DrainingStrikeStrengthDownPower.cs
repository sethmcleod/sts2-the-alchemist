using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Cards.Uncommon;

namespace Alchemist.AlchemistCode.Powers;

public class DrainingStrikeStrengthDownPower : CustomTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<DrainingStrike>();
    protected override bool IsPositive => false;
}
