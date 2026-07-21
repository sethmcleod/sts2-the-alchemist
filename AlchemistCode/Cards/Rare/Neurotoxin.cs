using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Rare;

// A low-HP payoff: when you are Reduced, the venom paralyzes the target as well as damaging it.
// Gambit gates the Stun so it is only online when you are in danger, and Exhaust caps it to once
// per draw. Base 2-cost Rare single-target attacks sit at 18-20 damage; the Stun rider and Exhaust
// balance the raw number toward the low end of that band
public class Neurotoxin : AlchemistCard
{
    protected override bool IsGambitCard => true;

    public Neurotoxin() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(18, 6);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(StaticHoverTip.Stun);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (IsReduced && play.Target != null)
            await CreatureCmd.Stun(play.Target);
    }
}
