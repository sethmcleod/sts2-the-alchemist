using MegaCrit.Sts2.Core.Entities.Cards;

namespace Alchemist.AlchemistCode.Cards.Hero;

// A card the Alchemist offers while The Hero Expansion is loaded, or while the HeroCardsWithoutExpansion
// setting is on (HeroExpansion.Offered decides). Otherwise HeroCardPoolPatches drops it from its pool,
// so the solo pool stays 20/35/25 and the card sits locked in the compendium. No card references a Hero
// Expansion keyword, power or enchantment, so the two mods stay uncoupled
public abstract class AlchemistHeroCard : AlchemistCard
{
    protected AlchemistHeroCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
    }
}
