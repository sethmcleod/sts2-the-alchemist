using Alchemist.AlchemistCode.Extensions;
using BaseLib.Abstracts;

namespace Alchemist.AlchemistCode.Enchantments;

public abstract class AlchemistEnchantment : CustomEnchantmentModel
{
    public override bool IsStackable => true;
    public override bool ShowAmount => true;

    protected abstract string IconName { get; }
    protected override string? CustomIconPath => $"{IconName}.png".EnchantmentImagePath();
}
