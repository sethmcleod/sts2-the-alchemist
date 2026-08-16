using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

// COMPAT-BRANCH: main

namespace Alchemist.AlchemistCode.Cards;

// BRANCH-SPECIFIC, like Compat/GameCompat.cs. The public branch has no CardLocation type and no
// GetResultLocationForCardPlay to override, so a Ferment card cannot be routed out of the Play pile
// by picking its result location. It is Exhausted instead, which removes it for the combat the same way.
//
// THIS COPY IS THE main IMPLEMENTATION. The beta copy overrides GetResultLocationForCardPlay and
// leaves this method a no-op.
public abstract partial class AlchemistCard
{
    private Task RemoveFermentFromCombat(PlayerChoiceContext choiceContext) =>
        IsFermentCard ? CardCmd.Exhaust(choiceContext, this) : Task.CompletedTask;
}
