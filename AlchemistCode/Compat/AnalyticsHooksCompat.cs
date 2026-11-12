using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

// COMPAT-BRANCH: main

namespace Alchemist.AlchemistCode.Analytics;

public sealed partial class AnalyticsHooks
{
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength) =>
        OnDeath(creature, wasRemovalPrevented);
}
