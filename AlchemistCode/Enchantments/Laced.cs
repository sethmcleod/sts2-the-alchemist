using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Enchantments;

public sealed class Laced : AlchemistEnchantment
{
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

    // A card that already carries the Laced keyword would print the word twice and count its Poison
    // twice, so the enchantment refuses it
    public override bool CanEnchant(CardModel card) =>
        base.CanEnchant(card) && !card.Keywords.Contains(AlchemistKeywords.Laced);

    // Prints the enchantment's name in purple above the card text
    public override bool HasExtraCardText => true;

    // Sharp is the base-game shape for this hook, including the IsPoweredAttack guard that keeps the
    // bonus off a card's incidental damage
    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        if (!props.IsPoweredAttack() || !HasCard) return 0m;
        return Card.Owner.Creature.GetPowerAmount<PoisonPower>();
    }
}
