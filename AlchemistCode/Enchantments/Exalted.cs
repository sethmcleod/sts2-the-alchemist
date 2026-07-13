using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Enchantments;

// Power enchantment: when the enchanted card is played, gain X Strength
public sealed class Exalted : AlchemistEnchantment
{
    protected override string IconName => "exalted";

    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Power;

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        await PowerCmd.Apply<StrengthPower>(choiceContext, Card.Owner.Creature, Amount, Card.Owner.Creature, Card);
    }
}
