using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Potions;

// Renders Gold Leaf's current total heal (Gold / 15). Computed when the potion's owner is assigned.
// ToString() emits the whole " (N HP)" suffix so it can vanish entirely when there's nothing to heal
// (e.g. an unowned potion in a shop/reward preview shows 0) — smartformat renders a plain {var} via ToString()
public sealed class GoldHealVar : DynamicVar
{
    public GoldHealVar() : base("HealTotal", 0m) { }

    public override void SetOwner(AbstractModel owner)
    {
        base.SetOwner(owner);
        if (owner is PotionModel { Owner: { } player })
            BaseValue = (int)(player.Gold / 15m);
    }

    // Read IntValue (backed by BaseValue) rather than a private field, so the value survives DynamicVar.Clone()
    // — the var set renders from a clone, and a private field wouldn't be copied
    public override string ToString() => IntValue > 0 ? $" ([green]{IntValue}[/green] HP)" : "";
}
