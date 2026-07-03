using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Culture : AlchemistCard
{
    protected override bool IsFermentCard => true;

    public Culture() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        // Live damage = flat 6, increased 75% (100% upgraded) per fermented turn. {CalculatedDamage}
        // shows the true current value and turns green while fermented (like Cornered's scaled damage).
        WithCalculatedDamage(6, static (card, _) =>
                System.Math.Floor(card.DynamicVars.CalculationBase.BaseValue * (card.IsUpgraded ? 100m : 75m) / 100m
                                  * ((AlchemistCard)card).FermentTurns),
            ValueProp.Move, 0, 0);
        WithKeyword(CardKeyword.Retain);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext); // uses CalculatedDamage total
        ConsumeFermentTurns();
    }
}
