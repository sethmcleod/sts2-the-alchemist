using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Alchemist.AlchemistCode.Character;
using Alchemist.AlchemistCode.Extensions;

namespace Alchemist.AlchemistCode.Potions;

[Pool(typeof(AlchemistPotionPool))]
public abstract class AlchemistPotion : CustomPotionModel
{
    //Loads from Alchemist/images/potions/your_potion.png (+ outlines/your_potion.png)
    public override string CustomPackedImagePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();

    public override string CustomPackedOutlinePath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlinePath();
}
