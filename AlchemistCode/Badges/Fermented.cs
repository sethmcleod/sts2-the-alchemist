using Alchemist.AlchemistCode.Analytics;
using Alchemist.AlchemistCode.Extensions;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Badges;

public sealed class Fermented() : CustomBadge(requiresWin: false, multiplayerOnly: false)
{
    private const int BronzeTurns = 100;
    private const int SilverTurns = 200;
    private const int GoldTurns = 300;

    public override string CustomBadgeIconPath => "badge_fermented.png".BadgeImagePath();

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player)
    {
        var turns = RunCounters.TallyFor(player).GetValueOrDefault(RunCounters.FermentTurns);
        if (turns >= GoldTurns) return BadgeRarity.Gold;
        if (turns >= SilverTurns) return BadgeRarity.Silver;
        if (turns >= BronzeTurns) return BadgeRarity.Bronze;
        return BadgeRarity.None;
    }

    public override bool IsObtained(SerializableRun run, SerializablePlayer player)
    {
        return Rarity(run, player) != BadgeRarity.None;
    }
}
