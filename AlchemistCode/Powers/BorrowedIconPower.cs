using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

public abstract class BorrowedIconPower<TBase> : AlchemistPower where TBase : PowerModel
{
    private static string BaseIconName => ModelDb.GetId<TBase>().Entry.ToLowerInvariant();

    public override string CustomPackedIconPath => ImageHelper.GetImagePath($"atlases/power_atlas.sprites/{BaseIconName}.tres");
    public override string CustomBigIconPath => ImageHelper.GetImagePath($"powers/{BaseIconName}.png");
}
