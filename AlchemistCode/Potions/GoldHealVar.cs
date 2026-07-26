using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Potions;

// smartformat renders a plain {var} through ToString(), so this returns the whole " (N)" suffix and
// leaves it empty on a potion with no player owner, which is the canonical model in the compendium
public sealed class GoldHealVar : DynamicVar
{
    public GoldHealVar() : base("HealTotal", 0) { }

    // Computed on each render, because Gold moves across a run. Caching it in BaseValue at SetOwner time
    // freezes it. The IsMutable gate must come first, because Owner throws on a canonical model
    public override string ToString() =>
        _owner is PotionModel { IsMutable: true, Owner: { } player }
            ? $" (Heals [green]{(int)(player.Gold / 15m)}[/green] HP)"
            : "";
}
