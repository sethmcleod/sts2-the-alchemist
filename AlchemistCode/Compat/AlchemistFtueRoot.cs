#if STS2_V0114
using MegaCrit.Sts2.Core.Nodes.Ftue;

namespace Alchemist.AlchemistCode.Compat;

public partial class AlchemistFtueRoot : NFtue
{
    protected override string? SeenOnShowId => null;
}
#endif
