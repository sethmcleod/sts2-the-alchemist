using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Enchantments;

// Skill enchantment: when the enchanted card is played, add X Foul Vapors into your Hand
public sealed class Fuming : AlchemistEnchantment
{
    protected override string IconName => "fuming";

    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Skill;

    // A nested FoulVapor tip lets a card with Fuming explain what it adds. Take(1) in InfuseTips removes
    // this tip, which keeps the tooltip of the source card short
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromCard<FoulVapor>() };

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Card.CombatState is not { } combat) return;
        for (var i = 0; i < Amount; i++)
        {
            var foulVapor = combat.CreateCard<FoulVapor>(Card.Owner);
            await CardPileCmd.AddGeneratedCardToCombat(foulVapor, PileType.Hand, Card.Owner);
        }
    }
}
