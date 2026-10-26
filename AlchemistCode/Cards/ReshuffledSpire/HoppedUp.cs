using System;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Cards.ReshuffledSpire;

[CardTheme(CardTheme.Poison)]
public class HoppedUp : AlchemistReshuffledCard
{
    private const int Hits = 4;

    private int _costDiscount;

    public HoppedUp() : base(3, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(4, 1);
        WithVar("Per", 4);
        WithTip(typeof(PoisonPower));
    }

    private void SyncCost()
    {
        if (Pile?.Type.IsCombatPile() != true) return;
        var baseResolved = EnergyCost.GetResolved() + _costDiscount;
        var want = Math.Min((int)Dose(this) / DynamicVars["Per"].IntValue, baseResolved);
        if (want == _costDiscount) return;
        EnergyCost.AddThisCombat(_costDiscount - want);
        _costDiscount = want;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is PoisonPower && IsMutable && power.Owner == Owner?.Creature) SyncCost();
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsMutable) SyncCost();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (IsMutable && player == Owner) SyncCost();
        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card == this) SyncCost();
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, Hits, vfx: HitVfx("vfx/vfx_attack_slash"))
            .Execute(choiceContext);
    }
}
