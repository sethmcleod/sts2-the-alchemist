using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Antidote : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    protected override bool Ferments => true;

    // Base matches Defend, so holding it is upside rather than a tempo tax
    public Antidote() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithCalculatedBlock(5, static (card, _) =>
                (card.IsUpgraded ? 6m : 4m) * ((AlchemistCard)card).FermentTurns,
            ValueProp.Move, 0, 0);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}
