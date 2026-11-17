using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Powers;

public class SteepPower : AlchemistPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { AlchemistTips.FermentRef };

    private bool Steeps(CardModel card) =>
        card.Owner?.Creature == Owner && card is AlchemistCard { IsFermentInline: true };

    public override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay,
        ResourceInfo resources, CardLocation location)
    {
        if (!Steeps(card) || location.pileType != PileType.Discard) return location;
        location.pileType = PileType.Draw;
        location.position = CardPilePosition.Random;
        return location;
    }

    public override Task AfterModifyingCardPlayResultLocation(CardModel card, CardLocation location)
    {
        if (Steeps(card) && location.pileType == PileType.Draw) Flash();
        return Task.CompletedTask;
    }
}
