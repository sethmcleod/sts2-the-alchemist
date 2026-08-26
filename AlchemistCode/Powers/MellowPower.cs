using System.Collections.Generic;
using Alchemist.AlchemistCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace Alchemist.AlchemistCode.Powers;

// The Ferment payoff power. Driven by AlchemistCard.AdvanceFerment rather than a hook, because
// fermentation is our own mechanic and has no base-game event; same poke pattern as PassItOnPower.
// One payout per turn of fermentation gained, so Taste Test, Steep and Bloom pay per trigger
public class MellowPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.FromKeyword(AlchemistKeywords.Ferment), HoverTipFactory.Static(StaticHoverTip.Block) };

    internal async Task OnFermented(int turns)
    {
        if (turns <= 0) return;
        Flash();
        // Unpowered, matching base AfterimagePower: an automatic per-event trickle does not scale
        await CreatureCmd.GainBlock(Owner, Amount * turns, ValueProp.Unpowered, null, fast: true);
    }
}
