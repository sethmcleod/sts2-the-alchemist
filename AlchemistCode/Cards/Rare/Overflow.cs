using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Overflow : AlchemistCard
{
    public Overflow() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(6, 2);
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        // Gain 1 Regen first, so the card always lands at least one hit.
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        var regen = Owner.Creature.GetPowerAmount<RegenPower>();
        if (Owner.Creature.HasPower<RegenPower>())
            await PowerCmd.Remove<RegenPower>(Owner.Creature);
        if (regen <= 0) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(regen)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }
}
