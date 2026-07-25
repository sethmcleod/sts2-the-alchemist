using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// Grants real Strength and Dexterity while you sit at or below half HP, and takes them back when you heal
// past it. This follows the base game's RedSkull relic: apply a negative amount to remove, and keep a
// record of what is granted. The record is an amount, not a flag, so a second Resolve stacks cleanly
public class ResolvePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private decimal _granted;

    // The same threshold the Gambit keyword uses
    private bool IsReduced => Owner is { } c && c.CurrentHp * 2 <= c.MaxHp;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) => Sync();

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner) await Sync();
    }

    // Our own stack can grow, for example a second copy of the card. Re-sync on that change only
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this) await Sync();
    }

    private async Task Sync()
    {
        var target = IsReduced ? Amount : 0m;
        var delta = target - _granted;
        if (delta == 0m) return;
        _granted = target;
        Flash();
        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(ctx, Owner, delta, Owner, null);
        await PowerCmd.Apply<DexterityPower>(ctx, Owner, delta, Owner, null);
    }
}
