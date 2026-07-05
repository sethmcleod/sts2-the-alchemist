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
        // Live Block = flat 6, increased 75% (100% upgraded) per fermented turn. {CalculatedBlock}
        // shows the true current value and turns green while fermented (like Congeal's scaled Block).
        WithCalculatedBlock(6, static (card, _) =>
                System.Math.Floor(card.DynamicVars.CalculationBase.BaseValue * (card.IsUpgraded ? 100m : 75m) / 100m
                                  * ((AlchemistCard)card).FermentTurns),
            ValueProp.Move, 0, 0);
        WithKeyword(CardKeyword.Retain);
        WithTips(_ => new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment) });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play); // uses the CalculatedBlock total
        ConsumeFermentTurns();
    }
}
