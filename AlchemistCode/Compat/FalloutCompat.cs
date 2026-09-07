using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

// COMPAT-BRANCH: beta

namespace Alchemist.AlchemistCode.Cards.Common;

// BRANCH-SPECIFIC, like Compat/AntitoxinPowerCompat.cs. The damage hook's signature differs between
// the game branches, so the override lives here and the logic stays in Cards/Common/Fallout.cs.
//
// THIS COPY IS THE beta IMPLEMENTATION: it takes the trailing CardPlay? that main does not have.
public partial class Fallout
{
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        BonusFor(target, cardSource);
}
