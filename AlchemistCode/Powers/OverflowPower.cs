using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace Alchemist.AlchemistCode.Powers;

// fermentation passes through without effect
public class OverflowPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.FermentRef };

    internal void OnFermented(AlchemistCard card)
    {
        if (!card.IsUpgradable) return;
        Flash();
        CardCmd.Upgrade(card);
        // Upgrade only previews a Deck card on its own; a held card needs the flip to show the change
        CardCmd.Preview(card);
    }
}
