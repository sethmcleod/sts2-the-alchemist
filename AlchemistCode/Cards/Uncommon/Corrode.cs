using System;
using Alchemist.AlchemistCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.Uncommon;

[CardTheme(CardTheme.Ferment, CardTheme.Poison)]
public class Corrode : AlchemistCard
{
    protected override bool Ferments => true;

    public Corrode() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(15, 5);
        WithVar("Poison", 3, 1);
        WithKeyword(CardKeyword.Retain);
        WithTip(typeof(PoisonPower));
    }

    private int _costDiscount;

    protected override void OnFermentTurnsChanged()
    {
        var baseResolved = EnergyCost.GetResolved() + _costDiscount;
        var want = Math.Min(FermentTurns, baseResolved);
        if (want == _costDiscount) return;
        EnergyCost.AddThisCombat(_costDiscount - want);
        _costDiscount = want;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, vfx: HitVfx("vfx/vfx_attack_blunt"),
            tmpSfx: "blunt_attack.mp3").WithAttackerAnim(HeavyAttackAnim, HeavyAttackDelay)
            .Execute(choiceContext);
        if (play.Target is not { IsAlive: true } target) return;
        PoisonSplash(target);
        await PowerCmd.Apply<PoisonPower>(choiceContext, target,
            DynamicVars["Poison"].IntValue, Owner.Creature, this);
    }
}
