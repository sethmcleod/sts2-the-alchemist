using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

// COMPAT-BRANCH: main

namespace Alchemist.AlchemistCode.Powers;

// BRANCH-SPECIFIC, like Compat/GameCompat.cs and Compat/AntitoxinPowerCompat.cs. The card-location
// hook pair cannot be routed through a wrapper, so the two overrides whose signatures differ between
// the game branches live here. Steeps() in Powers/SteepPower.cs is identical on both branches.
//
// THIS COPY IS THE main (DEFAULT BRANCH) IMPLEMENTATION: the hooks pass a PileType and a
// CardPilePosition as a tuple. beta folds them into one CardLocation struct.
// ON A MERGE FROM beta, KEEP THIS SIDE.
public partial class SteepPower
{
    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card,
        bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
    {
        if (!Steeps(card) || pileType != PileType.Discard) return (pileType, position);
        return (PileType.Draw, CardPilePosition.Random);
    }

    public override Task AfterModifyingCardPlayResultPileOrPosition(CardModel card, PileType pileType,
        CardPilePosition position)
    {
        if (Steeps(card) && pileType == PileType.Draw) Flash();
        return Task.CompletedTask;
    }
}
