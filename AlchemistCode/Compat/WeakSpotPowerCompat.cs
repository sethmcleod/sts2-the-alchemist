using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

// COMPAT-BRANCH: main

namespace Alchemist.AlchemistCode.Powers;

// BRANCH-SPECIFIC, like Compat/GameCompat.cs. An override cannot be routed through a wrapper, so
// the one override whose signature differs between the game branches lives here. Its logic stays
// in WeakSpotPower.cs, which is identical on both branches.
//
// THIS COPY IS THE main (DEFAULT BRANCH) IMPLEMENTATION: no trailing CardPlay? parameter.
// ON A MERGE FROM beta, KEEP THIS SIDE.
public partial class WeakSpotPower
{
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        PoisonedAttackMultiplier(target, props);
}
