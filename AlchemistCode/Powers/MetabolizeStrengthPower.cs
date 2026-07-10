using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Cards.Uncommon;

namespace Alchemist.AlchemistCode.Powers;

public class MetabolizeStrengthPower : CustomTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<Metabolize>();
    protected override bool IsPositive => true;
}
