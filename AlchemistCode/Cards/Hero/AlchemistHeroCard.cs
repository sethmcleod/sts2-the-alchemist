using MegaCrit.Sts2.Core.Entities.Cards;

namespace Alchemist.AlchemistCode.Cards.Hero;

// A card the Alchemist only offers while The Hero Expansion is loaded. HeroCardPoolPatches drops every
// one of these from its pool when it is not, so the solo pool stays 20/35/25 on its own and the cards
// sit locked in the compendium.None of them reference Hero Expansion keyword, power or enchantment so
// the two mods stay uncoupled
public abstract class AlchemistHeroCard : AlchemistCard
{
    protected AlchemistHeroCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
    }
}
