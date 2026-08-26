using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

// COMPAT-BRANCH: beta

namespace Alchemist.AlchemistCode.Powers;

// BRANCH-SPECIFIC, like Compat/AntitoxinPowerCompat.cs: the damage hook takes a trailing CardPlay on
// public-beta and not on main. IchorPower adds the owner's Poison to powered card attacks; the rule
// itself lives in DoseBonus. THIS COPY IS THE beta IMPLEMENTATION.
public partial class IchorPower
{
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        DoseBonus(props, dealer, cardSource);
}
