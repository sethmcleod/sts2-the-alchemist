using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Potions;

// This shows the current total heal of Gold Leaf, which is Gold / 15. The code computes it when the
// potion gets an owner. ToString() returns the whole " (N HP)" suffix, so the suffix can disappear when
// there is nothing to heal. For example, a potion with no owner in a shop preview shows 0. smartformat
// renders a plain {var} with ToString()
public sealed class GoldHealVar : DynamicVar
{
    public GoldHealVar() : base("HealTotal", 0m) { }

    public override void SetOwner(AbstractModel owner)
    {
        base.SetOwner(owner);
        if (owner is PotionModel { Owner: { } player })
            BaseValue = (int)(player.Gold / 15m);
    }

    // Read IntValue, which BaseValue backs, and do not use a private field. The value then survives
    // DynamicVar.Clone(). The var set renders from a clone, and a clone does not copy a private field
    public override string ToString() => IntValue > 0 ? $" ([green]{IntValue}[/green] HP)" : "";
}
