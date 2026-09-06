using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

// COMPAT-BRANCH: beta

namespace Alchemist.AlchemistCode.Powers;

// BRANCH-SPECIFIC, like Compat/GameCompat.cs and Compat/AntitoxinPowerCompat.cs. The card-location
// hook pair cannot be routed through a wrapper, so the two overrides whose signatures differ between
// the game branches live here. Steeps() in Powers/SteepPower.cs is identical on both branches.
//
// THIS COPY IS THE beta (PUBLIC-BETA BRANCH) IMPLEMENTATION: the hooks pass one CardLocation struct.
// main passes a PileType and a CardPilePosition as a tuple.
// ON A MERGE INTO main, KEEP main's SIDE.
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
