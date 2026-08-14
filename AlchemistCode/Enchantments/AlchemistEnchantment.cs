using Alchemist.AlchemistCode.Extensions;
using BaseLib.Abstracts;

namespace Alchemist.AlchemistCode.Enchantments;

public abstract class AlchemistEnchantment : CustomEnchantmentModel
{
    public override bool IsStackable => true;
    public override bool ShowAmount => true;

    protected override string? CustomIconPath =>
        $"{GetType().Name.ToLowerInvariant()}.png".EnchantmentImagePath();
}
