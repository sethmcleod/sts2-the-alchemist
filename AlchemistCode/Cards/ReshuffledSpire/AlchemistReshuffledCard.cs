using MegaCrit.Sts2.Core.Entities.Cards;

namespace Alchemist.AlchemistCode.Cards.ReshuffledSpire;

public abstract class AlchemistReshuffledCard : AlchemistCard
{
    protected AlchemistReshuffledCard(int cost, CardType type, CardRarity rarity, TargetType target)
        : base(cost, type, rarity, target)
    {
    }
}
