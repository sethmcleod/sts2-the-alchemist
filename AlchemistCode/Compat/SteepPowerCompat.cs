using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

// COMPAT-BRANCH: beta

namespace Alchemist.AlchemistCode.Powers;

public partial class SteepPower
{
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
