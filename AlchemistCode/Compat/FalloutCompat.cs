using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

// COMPAT-BRANCH: main

namespace Alchemist.AlchemistCode.Cards.Common;

// BRANCH-SPECIFIC, like Compat/AntitoxinPowerCompat.cs. The damage hook's signature differs between
// the game branches, so the override lives here and the logic stays in Cards/Common/Fallout.cs.
//
// THIS COPY IS THE main (DEFAULT BRANCH) IMPLEMENTATION: no trailing CardPlay? parameter.
// ON A MERGE FROM beta, KEEP THIS SIDE.
public partial class Fallout
{
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        BonusFor(target, cardSource);
}
