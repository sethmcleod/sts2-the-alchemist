using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

// COMPAT-BRANCH: main

namespace Alchemist.AlchemistCode.Powers;

// BRANCH-SPECIFIC, like Compat/GameCompat.cs and Compat/WeakSpotPowerCompat.cs. An override cannot be
// routed through a wrapper, so the one override whose signature differs between the game branches
// lives here. Its logic stays in Powers/AntitoxinPower.cs, which is identical on both branches.
//
// THIS COPY IS THE main (DEFAULT BRANCH) IMPLEMENTATION: no trailing CardPlay? parameter.
// ON A MERGE FROM beta, KEEP THIS SIDE.
public partial class AntitoxinPower
{
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        Absorb(target, amount, props, dealer, cardSource);
}
