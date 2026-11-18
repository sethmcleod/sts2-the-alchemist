#if !STS2_V0113
using System.Globalization;

namespace Alchemist.AlchemistCode.Cards;

public abstract partial class PreviewVar
{
    protected override decimal GetBaseValueForIConvertible() => PreviewValue;

    public override string ToString() => ((int)PreviewValue).ToString(CultureInfo.InvariantCulture);
}
#endif
