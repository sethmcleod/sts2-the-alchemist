using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Alchemist.AlchemistCode.Powers;

public sealed class AntitoxinLimitVar : DynamicVar
{
    public AntitoxinLimitVar() : base("Limit", 0) { }

    public override string ToString() =>
        _owner is AntitoxinPower { Owner: not null } power
            ? AntitoxinPower.MaxFor(power.Owner).ToString()
            : AntitoxinPower.BaseMax.ToString();
}
