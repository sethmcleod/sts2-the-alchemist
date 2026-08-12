using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Enchantments;

public sealed class Laced : AlchemistEnchantment
{
    protected override string IconName => "laced";

    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // IsPoweredAttack keeps this to the card's attack. A card also carries itself as the source of
        // incidental damage, such as a Poison trigger or a loss of HP, which must not apply Poison. The
        // base game EnvenomPower has the same guard
        if (cardSource != Card || !props.IsPoweredAttack() || result.UnblockedDamage <= 0) return;

        await PowerCmd.Apply<PoisonPower>(choiceContext, target, Amount, Card.Owner.Creature, null);
        // The same dose that goes on the blade goes into the Alchemist. This is what prices Laced on a
        // card that hits many times, where the enemy Poison used to compound at no cost
        await PowerCmd.Apply<PoisonPower>(choiceContext, Card.Owner.Creature, Amount, Card.Owner.Creature, null);
    }
}
