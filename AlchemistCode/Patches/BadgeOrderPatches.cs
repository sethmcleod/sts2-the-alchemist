using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Badges;

namespace Alchemist.AlchemistCode.Patches;

// BaseLib appends custom badges after the base set, so each listed badge moves next to its anchor
[HarmonyPatch]
[HarmonyAfter(BaseLib.BaseLibMain.ModId)]
[HarmonyPriority(Priority.Low)]
public static class BadgeOrderPatches
{
    // Applied in order: a later pair may anchor on an earlier pair's badge
    private static readonly (string Anchor, string Badge)[] Placement =
    {
        ("KACHING", new Badges.PotionSale().Id),
        (new Badges.Mixes().Id, new Badges.Fermented().Id),
        (new Badges.Fermented().Id, new Badges.AntitoxinPeak().Id),
        (new Badges.AntitoxinPeak().Id, new Badges.CleanRun().Id),
    };

    // Resolved by name since CreateAll's signature differs between game branches
    private static MethodBase TargetMethod() => AccessTools.Method(typeof(BadgePool), nameof(BadgePool.CreateAll));

    public static void Postfix(ref IReadOnlyCollection<Badge> __result)
    {
        var list = __result.ToList();
        var moved = false;
        foreach (var (anchorId, badgeId) in Placement)
        {
            var anchor = list.FindIndex(b => b.Id == anchorId);
            var index = list.FindIndex(b => b.Id == badgeId);
            if (anchor < 0 || index < 0 || index == anchor + 1) continue;

            var badge = list[index];
            list.RemoveAt(index);
            if (index < anchor) anchor--;
            list.Insert(anchor + 1, badge);
            moved = true;
        }
        if (moved) __result = list;
    }
}
