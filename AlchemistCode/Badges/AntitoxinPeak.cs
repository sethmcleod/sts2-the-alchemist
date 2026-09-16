using Alchemist.AlchemistCode.Analytics;
using Alchemist.AlchemistCode.Extensions;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Badges;

public sealed class AntitoxinPeak() : CustomBadge(requiresWin: false, multiplayerOnly: false)
{
    private const int BronzePeak = 30;
    private const int SilverPeak = 50;
    private const int GoldPeak = 80;

    public override string CustomBadgeIconPath => "badge_antitoxin.png".BadgeImagePath();

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player)
    {
        var peak = RunCounters.CountFor(player, RunCounters.AntitoxinPeak);
        if (peak >= GoldPeak) return BadgeRarity.Gold;
        if (peak >= SilverPeak) return BadgeRarity.Silver;
        if (peak >= BronzePeak) return BadgeRarity.Bronze;
        return BadgeRarity.None;
    }

    public override bool IsObtained(SerializableRun run, SerializablePlayer player)
    {
        return Rarity(run, player) != BadgeRarity.None;
    }
}
