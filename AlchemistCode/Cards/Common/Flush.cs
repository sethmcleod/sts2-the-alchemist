using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

// The 0-cost Block slot vanilla fills three times and this pool had empty. Boost Away is the model:
// above-rate Block paid for with a real drawback. Poison damage is Unblockable, so the Block never
// covers the tick this causes; it covers the enemy while you settle the bill early
public class Flush : AlchemistCard
{
    protected internal override bool PlaysCastAnimation => false;

    public Flush() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(6, 3);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow =>
        IsMutable && Owner != null && Owner.Creature.GetPowerAmount<PoisonPower>() > 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, play);
        await PoisonTrigger.Once(choiceContext, Owner.Creature);
    }
}
