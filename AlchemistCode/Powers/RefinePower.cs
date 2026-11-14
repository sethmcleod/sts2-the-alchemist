using System.Runtime.CompilerServices;
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

    private static readonly object Mark = new();
    private static readonly ConditionalWeakTable<CardModel, object> Refined = new();

    internal static void CarryOver(CardModel original, CardModel copy)
    {
        if (Refined.TryGetValue(original, out _)) Refined.AddOrUpdate(copy, Mark);
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card.Owner != Owner.Player || !Mixing.IsMix(card) || Refined.TryGetValue(card, out _))
            return Task.CompletedTask;
        if (card.IsUpgraded) card.BaseReplayCount += (int)Amount;
        else if (card.IsUpgradable) CardCmd.Upgrade(card);
        else return Task.CompletedTask;
        Refined.AddOrUpdate(card, Mark);
        Flash();
        return Task.CompletedTask;
    }
}
