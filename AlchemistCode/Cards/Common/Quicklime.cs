using System.Linq;
using Alchemist.AlchemistCode.Compat;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

public class Quicklime : AlchemistCard
{
    public Quicklime() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(5, 2);
        WithVar("Bonus", 4, 2);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow =>
        IsMutable && Owner != null && CombatState != null
        && CombatState.Enemies.Any(e => e.IsAlive && e.GetPowerAmount<PoisonPower>() > 0);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target == null) return;

        // One attack rather than two, so Strength lands on the total once and the enemy sees a
        // single number
        var bonus = play.Target.HasPower<PoisonPower>() ? DynamicVars["Bonus"].IntValue : 0;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue + bonus)
            .WithHitFx(HitVfx("vfx/vfx_sandy_impact"), null, null)
            .FromCard(this, play)
            .Targeting(play.Target)
            .Execute(choiceContext);
    }
}
