using System.Collections.Generic;
using Alchemist.AlchemistCode.Config;
using BaseLib.Hooks;

namespace Alchemist.AlchemistCode.Powers;

// Delayed Reaction deals its damage at the end of the applier's next turn. Nothing showed on the enemy
// health bar, so the pending hit was invisible and easy to forget. This previews the pending damage the
// way Poison previews its next tick. BaseLib's HealthBarForecastRegistry calls this for each creature and
// renders the returned segment. Registered in MainFile.Initialize
public sealed class DelayedReactionForecast : IHealthBarForecastSource
{
    public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        // The accessibility toggle can hide the preview entirely; the color is a config color picker
        if (!AlchemistModConfig.ShowDamageForecasts)
            yield break;

        // Show only on the turn the hit will land. The power arms at the applier's first turn end and
        // detonates at the next, so before it is armed the hit is still a turn away and nothing shows yet
        if (context.Creature.GetPower<DelayedReactionPower>() is not { IsArmed: true })
            yield break;

        var amount = context.Creature.GetPowerAmount<DelayedReactionPower>();
        if (amount > 0)
            yield return new HealthBarForecastSegment(amount, AlchemistModConfig.ForecastColor,
                HealthBarForecastDirection.FromRight);
    }
}
