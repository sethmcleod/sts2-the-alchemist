using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Alchemist.AlchemistCode.Cards.Uncommon;

namespace Alchemist.AlchemistCode.Powers;

// Temporary Strength granted by Philtre ("this turn") — removed at end of turn.
public class PhiltreStrengthPower : TemporaryStrengthPower, ICustomModel
{
    public override AbstractModel OriginModel => ModelDb.Card<Philtre>();
    protected override bool IsPositive => true;
}
