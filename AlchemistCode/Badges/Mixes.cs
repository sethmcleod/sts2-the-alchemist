using Alchemist.AlchemistCode.Analytics;
using Alchemist.AlchemistCode.Extensions;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Badges;

// BaseLib scans mod assemblies for CustomBadge, so a badge needs no registration call. It derives the id
// from the type name, giving ALCHEMIST-MIXES, and that id is both the key prefix in the "badges" loc table
// and the name the run history screen looks the icon up by
public sealed class Mixes() : CustomBadge(requiresWin: false, multiplayerOnly: false)
{
    private const int BronzeMixes = 10;
    private const int SilverMixes = 20;
    private const int GoldMixes = 30;

    public override string CustomBadgeIconPath => "badge_brew.png".BadgeImagePath();

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player)
    {
        var mixes = CountMixes(player);
        if (mixes >= GoldMixes) return BadgeRarity.Gold;
        if (mixes >= SilverMixes) return BadgeRarity.Silver;
        if (mixes >= BronzeMixes) return BadgeRarity.Bronze;
        return BadgeRarity.None;
    }

    public override bool IsObtained(SerializableRun run, SerializablePlayer player)
    {
        return Rarity(run, player) != BadgeRarity.None;
    }

    private static int CountMixes(SerializablePlayer player) =>
        RunCounters.CountFor(player, RunCounters.MixBursting)
        + RunCounters.CountFor(player, RunCounters.MixFuming)
        + RunCounters.CountFor(player, RunCounters.MixSyrupy)
        + RunCounters.CountFor(player, RunCounters.MixZesty);
}
