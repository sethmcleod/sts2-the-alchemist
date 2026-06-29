using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TheAlchemist.TheAlchemistCode.Cards.Uncommon;

namespace TheAlchemist.TheAlchemistCode.Powers;

public class MetaboliseStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<Metabolise>();
    protected override bool IsPositive => true;
}
