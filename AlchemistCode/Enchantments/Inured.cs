using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Alchemist.AlchemistCode.Powers;

namespace Alchemist.AlchemistCode.Enchantments;

public sealed class Inured : AlchemistEnchantment
{
    protected override string IconName => "inured";

    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Skill;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<AntitoxinPower>() };

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        var owner = Card.Owner;
        await PowerCmd.Apply<AntitoxinPower>(choiceContext, owner.Creature, Amount, owner.Creature, Card);
    }
}
