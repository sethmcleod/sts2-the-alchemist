using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// TemporaryStrengthPower looks its icon up by base id, which fails for ours, so borrow the base icons
public abstract class CustomTemporaryStrengthPower : TemporaryStrengthPower, ICustomPower
{
    private string BaseIconName => IsPositive ? "flex_potion_power" : "dark_shackles_power";

    public string? CustomPackedIconPath => ImageHelper.GetImagePath($"atlases/power_atlas.sprites/{BaseIconName}.tres");
    public string? CustomBigIconPath => ImageHelper.GetImagePath($"powers/{BaseIconName}.png");
}
