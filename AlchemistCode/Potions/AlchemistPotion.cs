using BaseLib.Abstracts;
using BaseLib.Utils;
using Alchemist.AlchemistCode.Character;

namespace Alchemist.AlchemistCode.Potions;

[Pool(typeof(AlchemistPotionPool))]
public abstract class AlchemistPotion : CustomPotionModel;