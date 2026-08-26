using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

// COMPAT-BRANCH: main

namespace Alchemist.AlchemistCode.Powers;

// BRANCH-SPECIFIC, like Compat/AntitoxinPowerCompat.cs: the damage hook takes a trailing CardPlay on
// public-beta and not on main. IchorPower adds the owner's Poison to powered card attacks; the rule
// itself lives in DoseBonus.
//
// THIS COPY IS THE main (DEFAULT BRANCH) IMPLEMENTATION: no trailing CardPlay? parameter.
// ON A MERGE FROM beta, KEEP THIS SIDE.
public partial class IchorPower
{
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        DoseBonus(props, dealer, cardSource);
}
