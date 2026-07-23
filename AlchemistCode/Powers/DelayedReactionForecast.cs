using System.Collections.Generic;
using BaseLib.Hooks;
using Godot;

namespace Alchemist.AlchemistCode.Powers;

// Delayed Reaction deals its damage at the end of the applier's next turn. Nothing showed on the enemy
// health bar, so the pending hit was invisible and easy to forget. This previews the pending damage the
// way Poison previews its next tick. BaseLib's HealthBarForecastRegistry calls this for each creature and
// renders the returned segment. Registered in MainFile.Initialize
public sealed class DelayedReactionForecast : IHealthBarForecastSource
{
    // A pink-purple that matches the bomb icon and reads apart from Doom's deeper magenta. Tune freely
    private static readonly Color SegmentColor = new("B15CD1");

    public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        // Show only on the turn the hit will land. The power arms at the applier's first turn end and
        // detonates at the next, so before it is armed the hit is still a turn away and nothing shows yet
        if (context.Creature.GetPower<DelayedReactionPower>() is not { IsArmed: true })
            yield break;

        var amount = context.Creature.GetPowerAmount<DelayedReactionPower>();
        if (amount > 0)
            yield return new HealthBarForecastSegment(amount, SegmentColor, HealthBarForecastDirection.FromRight);
    }
}
