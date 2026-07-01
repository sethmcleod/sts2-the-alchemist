using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

public class Reinforce : AlchemistCard
{
    public Reinforce() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCalculatedBlock(8, 4, (card, _) =>
            card.Owner.Creature.Powers.Count(p => p.TypeForCurrentAmount == PowerType.Debuff),
            ValueProp.Move, 0, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
    }
}
