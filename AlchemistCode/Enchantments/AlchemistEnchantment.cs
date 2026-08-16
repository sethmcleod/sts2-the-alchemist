using Alchemist.AlchemistCode.Extensions;
using BaseLib.Abstracts;

namespace Alchemist.AlchemistCode.Enchantments;

public abstract class AlchemistEnchantment : CustomEnchantmentModel
{
    protected override string? CustomIconPath =>
        $"{GetType().Name.ToLowerInvariant()}.png".EnchantmentImagePath();
}
