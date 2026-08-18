using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

// The one Common draw card, and it only draws when you are dosed: the reward for carrying the
// dose is the card, not the Block
[CardTheme(CardTheme.Poison)]
public class Congeal : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    protected override bool ConditionalGlow => Dose(this) > 0;

    public Congeal() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(6, 3);
        WithCards(1, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        if (Dose(this) > 0)
            await CommonActions.Draw(this, choiceContext);
    }
}
