using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Alchemist.AlchemistCode.Powers;

public sealed class AntitoxinLimitVar : DynamicVar
{
    public AntitoxinLimitVar() : base("Limit", 0) { }

    // Through LimitFor, so this and the power's own description never disagree about the ceiling
    public override string ToString() =>
        AntitoxinPower.LimitFor(_owner is AntitoxinPower { Owner: not null } power ? power.Owner : null)
            .ToString();
}
