using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Cards.Common;

// The enabler. It carries no Reaction of its own; it hands the next Reaction card a free trigger
public class Reagent : AlchemistCard
{
    protected override bool ShowsReactionTip => true;

    public Reagent() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 2);
        // Reactive counts down a live stack, so outside combat it always reads "the next 0 cards".
        WithTips(card => card.CombatState != null
            ? new[] { HoverTipFactory.FromPower<ReactivePower>() }
            : []);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_slash")).Execute(choiceContext);
        await PowerCmd.Apply<ReactivePower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
