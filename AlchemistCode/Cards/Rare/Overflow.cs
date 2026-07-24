using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Rare;

public class Overflow : AlchemistCard
{
    public Overflow() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(4, 1);
        WithKeyword(CardKeyword.Exhaust);
        WithTip(typeof(RegenPower));
        ExplainNumber(DynamicVars.Damage, "ALCHEMIST-OVERFLOW");
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (CombatState == null) return;
        var regen = Owner.Creature.GetPowerAmount<RegenPower>();
        if (regen <= 0) return;
        await PowerCmd.Remove<RegenPower>(Owner.Creature);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(regen)
            .WithHitFx(HitVfx("vfx/vfx_heavy_blunt"), null, "heavy_attack.mp3")
            .FromCard(this, play)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }
}
