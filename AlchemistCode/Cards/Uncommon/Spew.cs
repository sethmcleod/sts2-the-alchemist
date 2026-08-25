using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Poison)]
public class Spew : AlchemistCard
{
    public Spew() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(7, 3);
        WithVar("Poison", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override bool ConditionalGlow =>
        IsMutable && CombatState?.Enemies.Where(e => e.IsAlive).ToList() is { Count: > 0 } enemies
        && enemies.TrueForAll(e => e.GetPowerAmount<PoisonPower>() > 0);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);
        if (play.Target is not { IsAlive: true } target) return;
        var poison = DynamicVars["Poison"].IntValue;
        if (target.GetPowerAmount<PoisonPower>() > 0) poison *= 2;
        PoisonSplash(target);
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, poison, Owner.Creature, this);
    }
}
