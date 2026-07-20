using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class RollingBoil : AlchemistCard
{
    protected override bool IsFermentCard => true;

    public RollingBoil() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithCalculatedDamage(6, static (card, _) =>
                System.Math.Floor(card.DynamicVars.CalculationBase.BaseValue * (card.IsUpgraded ? 100m : 75m) / 100m
                                  * ((AlchemistCard)card).FermentTurns),
            ValueProp.Move, 0, 0);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, 2).Execute(choiceContext);
    }
}
