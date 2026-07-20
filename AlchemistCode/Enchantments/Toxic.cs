using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Enchantments;

// Attack enchantment: whenever the enchanted card deals unblocked damage, apply X Poison to that target
public sealed class Toxic : AlchemistEnchantment
{
    protected override string IconName => "toxic";

    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // IsPoweredAttack keeps this to the card's attack. A card can also deal incidental damage that
        // carries itself as the source, such as a Poison trigger or a loss of HP. That damage is Unpowered,
        // and it must not apply Poison. The base game EnvenomPower has the same guard
        if (cardSource == Card && props.IsPoweredAttack() && result.UnblockedDamage > 0)
            await PowerCmd.Apply<PoisonPower>(choiceContext, target, Amount, Card.Owner.Creature, null);
    }
}
