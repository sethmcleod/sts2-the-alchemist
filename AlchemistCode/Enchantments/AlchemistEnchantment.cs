using Alchemist.AlchemistCode.Extensions;
using BaseLib.Abstracts;

namespace Alchemist.AlchemistCode.Enchantments;

// Shared base for the Infuse enchantments: they stack (X grows per Infuse), show their amount, and pull
// their icon from the mod's images/enchantments folder
public abstract class AlchemistEnchantment : CustomEnchantmentModel
{
    public override bool IsStackable => true;
    public override bool ShowAmount => true;

    protected abstract string IconName { get; }
    protected override string? CustomIconPath => $"{IconName}.png".EnchantmentImagePath();
}
