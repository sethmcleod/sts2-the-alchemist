using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

// COMPAT-BRANCH: beta

namespace Alchemist.AlchemistCode.Powers;

// BRANCH-SPECIFIC, like Compat/AntitoxinPowerCompat.cs. The signature of this override is the one
// thing that can move between the game branches, so it is isolated here and the logic stays in
// Powers/AnodynePower.cs, which is identical on both.
public partial class AnodynePower
{
    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource) =>
        Prevent(target, amount, props, dealer);
}
