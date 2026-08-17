using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Powers;

public class FreshCoatPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromPower<AntitoxinPower>() };

    // Called from Patches.InfusionPatches, which already postfixes every CardCmd.Enchant
    internal async Task OnEnchanted()
    {
        Flash();
        var context = new ThrowingPlayerChoiceContext();
        // Capacity first, so the gain lands inside the new limit instead of spilling to Block
        await PowerCmd.Apply<AntitoxinCapacityPower>(context, Owner, Amount, Owner, null);
        await PowerCmd.Apply<AntitoxinPower>(context, Owner, Amount, Owner, null);
    }
}
