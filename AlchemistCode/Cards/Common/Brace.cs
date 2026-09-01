using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

// The dosed body hardens: the one block card that reads your own Poison. Base Mirage reads
// the ENEMIES' Poison, so the axes stay distinct
[CardTheme(CardTheme.Poison)]
public class Brace : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Brace() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCalculatedBlock(2, static (card, _) => Dose(card), ValueProp.Move, 2, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}
