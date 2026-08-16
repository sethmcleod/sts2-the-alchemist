using Alchemist.AlchemistCode.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

// An Attack should deal damage, not grant Block. Quicklime is the caustic accelerant, so it speeds the
// reaction up instead: the Common rung below Flare Up, which also applies the Poison it then triggers
public class Quicklime : AlchemistCard
{
    public Quicklime() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(5, 2);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow =>
        IsMutable && Owner != null && CombatState != null
        && CombatState.Enemies.Any(e => e.IsAlive && e.GetPowerAmount<PoisonPower>() > 0);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_sandy_impact")).Execute(choiceContext);
        if (play.Target is { IsAlive: true } target)
            await PoisonTrigger.Once(choiceContext, target);
    }
}
