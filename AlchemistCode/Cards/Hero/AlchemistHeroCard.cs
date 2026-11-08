using MegaCrit.Sts2.Core.Entities.Cards;

namespace Alchemist.AlchemistCode.Cards.Hero;

public abstract class AlchemistHeroCard : AlchemistCard
{
    protected AlchemistHeroCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
    }
}
