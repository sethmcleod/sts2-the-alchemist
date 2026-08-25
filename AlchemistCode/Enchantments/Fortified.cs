using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Enchantments;

public sealed class Fortified : AlchemistEnchantment
{
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Power;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<StrengthPower>() };

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        await PowerCmd.Apply<StrengthPower>(choiceContext, Card.Owner.Creature, Amount, Card.Owner.Creature, Card);
    }
}
