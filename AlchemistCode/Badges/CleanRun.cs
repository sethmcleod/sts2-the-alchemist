using Alchemist.AlchemistCode.Analytics;
using Alchemist.AlchemistCode.Extensions;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Badges;

public sealed class CleanRun() : CustomBadge(requiresWin: true, multiplayerOnly: false)
{
    private const int MinPoisonGained = 100;

    public override string CustomBadgeIconPath => "badge_clean_run.png".BadgeImagePath();

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Bronze;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player)
    {
        return RunCounters.CountFor(player, RunCounters.PoisonGained) >= MinPoisonGained
               && RunCounters.CountFor(player, RunCounters.PoisonBled) == 0;
    }
}
