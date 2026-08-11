using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Alchemist.AlchemistCode.Powers;

/// <summary>
/// TEMPORARY. See Compat/ExtraTurn.cs for what to delete and when.
/// </summary>
/// <remarks>
/// Stands in for the base game's AmbergrisPower, which the beta has and the default branch does
/// not. Built only on ShouldTakeExtraTurn and AfterTakingExtraTurn, which both branches expose, so
/// one implementation serves both and the branches do not diverge over extra turns.
///
/// Unlike AmbergrisPower this one is visible in the power bar. That is a deliberate trade: a
/// visible power on both branches beats an invisible one on beta and a visible one on main.
/// </remarks>
public class ExtraTurnPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldTakeExtraTurn(Player player) => player == Owner.Player;

    // One stack is one extra turn, so spend a stack per turn taken rather than clearing the lot
    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player != Owner.Player) return;
        await PowerCmd.Decrement(this);
    }
}
