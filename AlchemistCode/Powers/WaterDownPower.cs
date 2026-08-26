using Alchemist.AlchemistCode.Cards.Rare;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

// Temporary Strength loss, restored at the end of the enemy's turn. The base class does all the
// work; see DyingStarPower for the vanilla twin of this shape
public class WaterDownPower : CustomTemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<WaterDown>();
    protected override bool IsPositive => false;
}
