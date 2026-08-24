using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class Puncture : AlchemistCard
{
    public Puncture() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6, 2);
        WithPower<VulnerablePower>(2, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow =>
        IsMutable && CombatState?.Enemies.Where(e => e.IsAlive).ToList() is { Count: > 0 } enemies
        && enemies.TrueForAll(e => e.GetPowerAmount<PoisonPower>() > 0);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_dramatic_stab")).Execute(choiceContext);
        if (play.Target is { IsAlive: true } target && target.GetPowerAmount<PoisonPower>() > 0)
            await CommonActions.Apply<VulnerablePower>(choiceContext, this, play);
    }
}
