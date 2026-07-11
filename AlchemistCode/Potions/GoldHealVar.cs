using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Potions;

// Renders Gold Leaf's current total heal (Gold / 15). Computed when the potion's owner is assigned
public sealed class GoldHealVar : DynamicVar
{
    public GoldHealVar() : base("HealTotal", 0m) { }

    public override void SetOwner(AbstractModel owner)
    {
        base.SetOwner(owner);
        if (owner is PotionModel { Owner: { } player })
            BaseValue = (int)(player.Gold / 15m);
    }
}
