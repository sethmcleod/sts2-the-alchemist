using BaseLib.Abstracts;
using BaseLib.Utils;
using TheAlchemist.TheAlchemistCode.Character;

namespace TheAlchemist.TheAlchemistCode.Potions;

[Pool(typeof(TheAlchemistPotionPool))]
public abstract class TheAlchemistPotion : CustomPotionModel;