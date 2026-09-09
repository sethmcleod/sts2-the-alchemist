using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// TemporaryStrengthPower finds its icon by base-game id. This fails for our custom ids, and the game
// shows the placeholder texture. ICustomPower supplies the icon instead, because BaseLib reads it
// there. Every base temporary-strength power shares one shackles icon for the debuff and one flex
// icon for the buff, so borrow those by sign rather than shipping a copy under the mod path
public abstract class CustomTemporaryStrengthPower : TemporaryStrengthPower, ICustomPower
{
    private string BaseIconName => IsPositive ? "flex_potion_power" : "dark_shackles_power";

    public string? CustomPackedIconPath => ImageHelper.GetImagePath($"atlases/power_atlas.sprites/{BaseIconName}.tres");
    public string? CustomBigIconPath => ImageHelper.GetImagePath($"powers/{BaseIconName}.png");
}
