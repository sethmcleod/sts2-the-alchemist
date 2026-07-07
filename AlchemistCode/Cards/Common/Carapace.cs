using Alchemist.AlchemistCode;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Carapace : AlchemistCard
{
    protected override bool IsFermentCard => true;

    public Carapace() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCalculatedBlock(6, static (card, _) =>
                System.Math.Floor(card.DynamicVars.CalculationBase.BaseValue * (card.IsUpgraded ? 75m : 50m) / 100m
                                  * ((AlchemistCard)card).FermentTurns),
            ValueProp.Move, 0, 0);
        WithKeyword(CardKeyword.Retain);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        ConsumeFermentTurns();
    }
}
