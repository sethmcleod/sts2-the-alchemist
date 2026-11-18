using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Analytics;

public sealed partial class AnalyticsHooks
{
#if STS2_V0113
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, CardModel? source,
        bool wasRemovalPrevented, float deathAnimLength) =>
        OnDeath(creature, wasRemovalPrevented);
#else
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength) =>
        OnDeath(creature, wasRemovalPrevented);
#endif
}
