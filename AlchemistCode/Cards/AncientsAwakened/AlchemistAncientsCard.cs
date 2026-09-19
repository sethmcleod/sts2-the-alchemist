using MegaCrit.Sts2.Core.Entities.Cards;

namespace Alchemist.AlchemistCode.Cards.AncientsAwakened;

// A card only Ancients Awakened can hand over. CrossMod drops these from every pool when the mod is
// absent, so they sit locked in the compendium, and none of them names one of its keywords
public abstract class AlchemistAncientsCard : AlchemistCard
{
    protected AlchemistAncientsCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
    }
}
