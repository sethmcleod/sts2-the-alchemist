using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Cards.Uncommon;

namespace Alchemist.AlchemistCode.Powers;

public class SurgeStrengthPower : CustomTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<Surge>();
    protected override bool IsPositive => true;
}
