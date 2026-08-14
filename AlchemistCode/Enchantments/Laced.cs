using Alchemist.AlchemistCode;
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
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // IsPoweredAttack keeps this to the card's attack. A card also carries itself as the source of
        // incidental damage, such as a Poison trigger or a loss of HP, which must not apply Poison. The
        // base game EnvenomPower has the same guard
        if (cardSource != Card || !props.IsPoweredAttack() || result.UnblockedDamage <= 0) return;

        await PowerCmd.Apply<PoisonPower>(choiceContext, target, Amount, Card.Owner.Creature, null);

        // The self-half stays at 1 however high Refine stacks the enchantment, and only the Alchemist
        // pays it: an ally handed a Laced card by Bestow has no Antitoxin and no Poison payoff
        if (Card.Owner.Character is not Character.Alchemist) return;
        await PowerCmd.Apply<PoisonPower>(choiceContext, Card.Owner.Creature, 1, Card.Owner.Creature, null);
    }
}
