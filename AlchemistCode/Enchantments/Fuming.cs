using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;

namespace Alchemist.AlchemistCode.Enchantments;

public sealed class Fuming : AlchemistEnchantment
{
    protected override string IconName => "fuming";

    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Skill;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<PoisonPower>(),
    };

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Card.CombatState is not { } combat) return;
        var owner = Card.Owner;

        // A Skill is usually untargeted, so the debuffs need a target of their own. One random enemy
        // takes the whole stack rather than one per stack, so a stacked Fuming reads as bigger numbers
        // on one enemy instead of a scatter the player cannot follow
        var target = owner.RunState.Rng.CombatTargets.NextItem(combat.HittableEnemies);
        if (target != null)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, target, Amount, owner.Creature, Card);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, target, Amount, owner.Creature, Card);
        }

        // The self-poison lands either way; it is the price of the enchantment, not part of the hit
        await PowerCmd.Apply<PoisonPower>(choiceContext, owner.Creature, Amount, owner.Creature, Card);
    }
}
