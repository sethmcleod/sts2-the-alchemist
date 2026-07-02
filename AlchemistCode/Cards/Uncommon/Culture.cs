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
        // Live damage = base 6 (9), increased 125% per fermented turn. {CalculatedDamage} shows the
        // true current value and turns green while fermented (like Cornered's HP-scaled damage).
        WithCalculatedDamage(6, static (card, _) =>
                System.Math.Floor(card.DynamicVars.CalculationBase.BaseValue * 125m / 100m
                                  * ((AlchemistCard)card).FermentTurns),
            ValueProp.Move, 3, 0);
        WithKeyword(CardKeyword.Retain);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext); // uses CalculatedDamage total
        ConsumeFermentTurns();
    }
}
