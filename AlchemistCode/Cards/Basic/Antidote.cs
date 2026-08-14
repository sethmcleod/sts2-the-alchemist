using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Basic;

public class Antidote : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Antidote() : base(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        WithCalculatedBlock(4, static (card, _) =>
            card.Owner.Creature.GetPowerAmount<PoisonPower>(), ValueProp.Move, 2, 0);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}
