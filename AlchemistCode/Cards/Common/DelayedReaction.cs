using Alchemist.AlchemistCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Alchemist.AlchemistCode.Cards.Common;

public class DelayedReaction : AlchemistCard
{
    public DelayedReaction() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithVar("Dmg", 16, 6);
        WithKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target == null) return;
        // A second play on the same enemy adds to the pending amount; the timer does not reset
        await PowerCmd.Apply<DelayedReactionPower>(choiceContext, play.Target,
            DynamicVars["Dmg"].IntValue, Owner.Creature, this);
    }
}
