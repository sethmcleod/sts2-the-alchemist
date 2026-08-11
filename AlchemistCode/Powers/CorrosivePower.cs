using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Alchemist.AlchemistCode.Powers;

// Weak and Vulnerable only, never "a debuff". Poison is itself a debuff, so the broader trigger
// would answer its own Poison with more Poison and never stop
public class CorrosivePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<PoisonPower>() };

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount <= 0m || applier != Owner) return;
        if (power is not (WeakPower or VulnerablePower)) return;
        if (power.Owner is not { IsAlive: true } enemy || enemy == Owner) return;
        Flash();
        await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), enemy, Amount, Owner, null);
    }
}
