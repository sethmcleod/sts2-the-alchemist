using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheAlchemist.TheAlchemistCode.Cards.Rare;

public class Overflow : TheAlchemistCard
{
    public Overflow() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(6, 2);
        WithTip(typeof(RegenPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var regen = Owner.Creature.GetPowerAmount<RegenPower>();
        if (Owner.Creature.HasPower<RegenPower>())
            await PowerCmd.Remove<RegenPower>(Owner.Creature);
        if (regen <= 0) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(regen)
            .FromCard(this)
            .TargetingAllOpponents(CombatState!)
            .Execute(choiceContext);
    }
}
