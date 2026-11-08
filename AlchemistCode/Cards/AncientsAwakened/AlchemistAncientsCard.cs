using MegaCrit.Sts2.Core.Entities.Cards;

namespace Alchemist.AlchemistCode.Cards.AncientsAwakened;

public abstract class AlchemistAncientsCard : AlchemistCard
{
    protected AlchemistAncientsCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
    }
}
