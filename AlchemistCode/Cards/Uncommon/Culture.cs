using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Culture : AlchemistCard
{
    protected override bool IsFermentCard => true;

    public Culture() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, static (card, _) =>
                System.Math.Floor(card.DynamicVars.CalculationBase.BaseValue * (card.IsUpgraded ? 125m : 100m) / 100m
                                  * ((AlchemistCard)card).FermentTurns),
            ValueProp.Move, 0, 0);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        ConsumeFermentTurns();
    }
}
