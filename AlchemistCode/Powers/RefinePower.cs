using Alchemist.AlchemistCode.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

public class RefinePower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.Static(StaticHoverTip.ReplayStatic) };

    // A Mix that arrives already upgraded (an upgraded maker, Fresh Batch) gains Replay instead, so
    // the smith and Refine add up rather than overlap. The sources upgrade before they add, so the
    // hook sees the finished card
    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator != Owner.Player || !Mixing.IsMix(card)) return Task.CompletedTask;
        if (card.IsUpgraded) card.BaseReplayCount += (int)Amount;
        else if (card.IsUpgradable) CardCmd.Upgrade(card);
        else return Task.CompletedTask;
        Flash();
        return Task.CompletedTask;
    }
}
