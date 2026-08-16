using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

// COMPAT-BRANCH: beta

namespace Alchemist.AlchemistCode.Cards;

// BRANCH-SPECIFIC, like Compat/GameCompat.cs. Picking the result location is the clean way to take a
// Ferment card out of combat: it leaves the way a Power does, firing no Exhaust triggers. Transforming
// it here instead would strand the replacement, because the engine's move out of the Play pile is
// guarded on THIS card still being in it.
//
// THIS COPY IS THE beta IMPLEMENTATION. The main branch has no CardLocation type and no
// GetResultLocationForCardPlay to override, so it Exhausts the card instead.
public abstract partial class AlchemistCard
{
    protected override CardLocation GetResultLocationForCardPlay() =>
        IsFermentCard
            ? new CardLocation(Owner, PileType.None, CardPilePosition.Bottom)
            : base.GetResultLocationForCardPlay();

    private Task RemoveFermentFromCombat(PlayerChoiceContext choiceContext) => Task.CompletedTask;
}
