using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Alchemist.AlchemistCode.Powers;

// PowerModel.SmartDescription is not virtual: it builds a fresh LocString from the smartDescription key and
// fills it from DynamicVars, never from the Description override. So an instance tooltip needs these two, or
// it renders the raw {Damage} and {When} placeholders. Both read the power, so the two description paths
// cannot drift. smartformat renders a plain {var} through ToString()

public sealed class UnstableCompoundDamageVar : DynamicVar
{
    public UnstableCompoundDamageVar() : base("Damage", 0) { }

    // Computed on each render, not cached in BaseValue: Hard to Kill and Intangible can change the capped
    // number mid-combat, and the tooltip has to agree with the health bar forecast
    public override string ToString() =>
        _owner is UnstableCompoundPower power ? power.EffectiveDamage.ToString() : "";
}

public sealed class UnstableCompoundWhenVar : DynamicVar
{
    public UnstableCompoundWhenVar() : base("When", 0) { }

    public override string ToString() =>
        _owner is UnstableCompoundPower power ? power.WhenText : "";
}
