using System.Collections.Generic;
using Alchemist.AlchemistCode.Config;
using BaseLib.Hooks;

namespace Alchemist.AlchemistCode.Powers;

// Previews Delayed Reaction's pending damage the way Poison previews its next tick, so the hit is not
// invisible. BaseLib's HealthBarForecastRegistry calls this per creature and renders the segment
public sealed class DelayedReactionForecast : IHealthBarForecastSource
{
    public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        if (!AlchemistModConfig.ShowDamageForecasts)
            yield break;

        // Only on the turn the hit lands. The power arms at the applier's first turn end and detonates
        // at the next, so before it is armed the hit is still a turn away
        if (context.Creature.GetPower<DelayedReactionPower>() is not { IsArmed: true })
            yield break;

        var amount = context.Creature.GetPowerAmount<DelayedReactionPower>();
        if (amount > 0)
            yield return new HealthBarForecastSegment(amount, AlchemistModConfig.ForecastColor,
                HealthBarForecastDirection.FromRight);
    }
}
