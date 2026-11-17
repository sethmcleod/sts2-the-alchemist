using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

// COMPAT-BRANCH: beta

namespace Alchemist.AlchemistCode.Analytics;

public sealed partial class AnalyticsHooks
{
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, CardModel? source,
        bool wasRemovalPrevented, float deathAnimLength) =>
        OnDeath(creature, wasRemovalPrevented);
}
