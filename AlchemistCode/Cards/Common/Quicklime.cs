using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Quicklime : AlchemistCard
{
    protected override ReactionCondition Reaction => ReactionCondition.Skill;

    public Quicklime() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(7, 2);
        // The Block is the Reaction payoff now, so it lives in the card's own Block var rather than
        // a separate one; that keeps Dexterity and the {Block:diff()} token working on it
        WithBlock(5, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var reacted = ReactionActive;
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_sandy_impact")).Execute(choiceContext);
        if (reacted)
            await CommonActions.CardBlock(this, play);
    }
}
