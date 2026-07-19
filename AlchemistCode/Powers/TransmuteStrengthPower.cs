using MegaCrit.Sts2.Core.Models;
using Alchemist.AlchemistCode.Cards.Uncommon;

namespace Alchemist.AlchemistCode.Powers;

public class TransmuteStrengthPower : CustomTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<Transmute>();
    protected override bool IsPositive => true;
}
