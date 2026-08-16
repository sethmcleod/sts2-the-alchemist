using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Enchantments;

public sealed class Laced : AlchemistEnchantment
{
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

    // Sharp is the base-game shape for this hook, including the IsPoweredAttack guard that keeps the
    // bonus off a card's incidental damage
    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        if (!props.IsPoweredAttack() || !HasCard) return 0m;
        return Card.Owner.Creature.GetPowerAmount<PoisonPower>();
    }
}
