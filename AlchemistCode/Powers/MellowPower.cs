using Alchemist.AlchemistCode;
using System.Collections.Generic;
using Alchemist.AlchemistCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

public class MellowPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.FermentRef, HoverTipFactory.Static(StaticHoverTip.Block) };

    internal async Task OnFermented(int turns)
    {
        if (turns <= 0) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount * turns, ValueProp.Unpowered, null, fast: true);
    }
}
