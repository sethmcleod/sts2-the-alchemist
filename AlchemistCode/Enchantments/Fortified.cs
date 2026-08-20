using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Enchantments;

public sealed class Fortified : AlchemistEnchantment
{
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Power;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<AntitoxinPower>() };

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        await PowerCmd.Apply<FortifiedPower>(choiceContext, Card.Owner.Creature, Amount, Card.Owner.Creature, Card);
    }
}
