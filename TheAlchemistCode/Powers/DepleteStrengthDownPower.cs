using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Cards.Uncommon;

namespace TheAlchemist.TheAlchemistCode.Powers;

public class DepleteStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<Deplete>();
    protected override bool IsPositive => false;
}
