using System;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Common;

[CardTheme(CardTheme.Poison)]
public class Spit : AlchemistCard
{
    public Spit() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 2);
        WithVar("Poison", 2, 1);
        WithTip(typeof(PoisonPower));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_slime_impact")).Execute(choiceContext);

        // The attack resolves first, so a Laced bonus is still priced off the full dose
        var dose = Math.Min(DynamicVars["Poison"].IntValue,
            Owner.Creature.GetPowerAmount<PoisonPower>());
        if (dose <= 0 || play.Target is not { IsAlive: true } target) return;

        await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, -dose, Owner.Creature, this);
        PoisonSplash(target);
        await PowerCmd.Apply<PoisonPower>(choiceContext, target, dose, Owner.Creature, this);
    }
}
