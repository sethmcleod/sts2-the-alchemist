using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Potions;

// This shows the current total of Gold Leaf, which is Gold / 15. That one number is both the heal and
// the Block. The code computes it when the potion gets an owner. ToString() returns the whole " (N)"
// suffix, so the suffix disappears when the potion has no owner, which is the canonical model in the
// compendium. smartformat renders a plain {var} with ToString()
public sealed class GoldHealVar : DynamicVar
{
    // -1 means "no owner yet", which is the canonical model. A potion in a run always sets a real
    // count, and 0 is a real count: a run with less than 15 Gold shows "(0)". The sentinel lives in
    // BaseValue, not in a private field, for the Clone() reason below
    private const int NoOwner = -1;

    public GoldHealVar() : base("HealTotal", NoOwner) { }

    public override void SetOwner(AbstractModel owner)
    {
        base.SetOwner(owner);
        if (owner is PotionModel { Owner: { } player })
            BaseValue = (int)(player.Gold / 15m);
    }

    // Read IntValue, which BaseValue backs, and do not use a private field. The value then survives
    // DynamicVar.Clone(). The var set renders from a clone, and a clone does not copy a private field
    public override string ToString() => IntValue > NoOwner ? $" ([green]{IntValue}[/green])" : "";
}
