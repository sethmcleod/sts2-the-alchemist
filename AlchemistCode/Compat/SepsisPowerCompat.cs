using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// BRANCH-SPECIFIC, like Compat/GameCompat.cs. An override cannot be routed through a wrapper, so
// the one override whose signature differs between the game branches lives here. Its logic stays
// in SepsisPower.cs, which is identical on both branches.
//
// THIS COPY IS THE beta IMPLEMENTATION: it takes the trailing CardPlay? that main does not have.
public partial class SepsisPower
{
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        PoisonedAttackMultiplier(target, props);
}
