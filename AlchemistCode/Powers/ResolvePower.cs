using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Powers;

// Follows the base game's RedSkull relic: grant real Strength and Dexterity, apply a negative amount to
// take them back, and keep a record of what was granted. That record is an amount, not a flag, so a
// second Resolve stacks cleanly
public class ResolvePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[]
        {
            AlchemistTips.Gambit,
            HoverTipFactory.FromPower<StrengthPower>(),
            HoverTipFactory.FromPower<DexterityPower>(),
        };

    private decimal _granted;

    private bool IsReduced => Gambit.IsActive(Owner);

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) => Sync();

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner) await Sync();
    }

    // Our own stack can grow, for example from a second copy of the card, so re-sync on that change
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
