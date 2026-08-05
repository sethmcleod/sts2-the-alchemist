using Alchemist.AlchemistCode.Extensions;
using Alchemist.AlchemistCode.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Alchemist.AlchemistCode.Badges;

// BaseLib scans mod assemblies for CustomBadge, so a badge needs no registration call. It derives the id
// from the type name, giving ALCHEMIST-BREW, and that id is both the key prefix in the "badges" loc table
// and the name the run history screen looks the icon up by
public sealed class Brew() : CustomBadge(requiresWin: false, multiplayerOnly: false)
{
    private const int BronzeBrews = 3;
    private const int SilverBrews = 6;
    private const int GoldBrews = 9;

    public override string CustomBadgeIconPath => "badge_brew.png".BadgeImagePath();

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player)
    {
        var brews = CountBrews(run, player);
        if (brews >= GoldBrews) return BadgeRarity.Gold;
        if (brews >= SilverBrews) return BadgeRarity.Silver;
        if (brews >= BronzeBrews) return BadgeRarity.Bronze;
        return BadgeRarity.None;
    }

    public override bool IsObtained(SerializableRun run, SerializablePlayer player)
    {
        return Rarity(run, player) != BadgeRarity.None;
    }

    // Only a Rest Site writes RestSiteChoices, and only the Alchemist's starter Kit relics offer Brew, so
    // counting the option id alone needs no room type or character filter
    private static int CountBrews(SerializableRun run, SerializablePlayer player)
    {
        var brews = 0;
        foreach (var act in run.MapPointHistory)
        {
            foreach (var mapPoint in act)
            {
                foreach (var stats in mapPoint.PlayerStats)
                {
                    if (stats.PlayerId != player.NetId) continue;
                    brews += stats.RestSiteChoices.Count(id => id == BrewRestSiteOption.BrewOptionId);
                }
            }
        }
        return brews;
    }
}
