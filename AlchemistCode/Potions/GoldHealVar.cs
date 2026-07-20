using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Potions;

// Shows the current total of Gold Leaf, which is Gold / 15. That one number is both the heal and the
// Block. ToString() returns the whole " (N)" suffix, so the suffix is empty when the potion has no player
// owner, which is the canonical model in the compendium. smartformat renders a plain {var} with ToString()
public sealed class GoldHealVar : DynamicVar
{
    public GoldHealVar() : base("HealTotal", 0) { }

    // Compute live on each render, because Gold changes across a run and combat. The old version cached the
    // total in BaseValue at SetOwner time, which froze it: the value went stale as Gold changed, and it
    // stayed empty for a potion whose var set was first built before the player owner was wired.
    // base._owner is the PotionModel, set through DynamicVarSet.InitializeWithOwner. IsMutable must gate the
    // Owner read, because Owner throws on a canonical model, and the pattern checks it first
    public override string ToString() =>
        _owner is PotionModel { IsMutable: true, Owner: { } player }
            ? $" ([green]{(int)(player.Gold / 15m)}[/green])"
            : "";
}
