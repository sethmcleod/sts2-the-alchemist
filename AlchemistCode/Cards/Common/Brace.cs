using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Ferment)]
public class Brace : AlchemistCard
{
    protected override bool Ferments => true;

    protected internal override bool PlaysCastAnimation => false;

    public Brace() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCalculatedBlock(3, static (card, _) => 2m * ((AlchemistCard)card).FermentTurns,
            ValueProp.Move, 2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}
