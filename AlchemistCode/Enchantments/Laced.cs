using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Enchantments;

public sealed class Laced : AlchemistEnchantment
{
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

    public override bool CanEnchant(CardModel card) =>
        base.CanEnchant(card) && !card.Keywords.Contains(AlchemistKeywords.Laced);

    public override bool HasExtraCardText => true;

    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        if (!props.IsPoweredAttack() || !HasCard) return 0m;
        return Card.Owner.Creature.GetPowerAmount<PoisonPower>();
    }
}
