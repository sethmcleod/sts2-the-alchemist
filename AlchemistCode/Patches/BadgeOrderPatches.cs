using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Badges;

namespace Alchemist.AlchemistCode.Patches;

// The game over screen and the run history draw badges in the order BadgePool.CreateAll returns them, and
// BaseLib's postfix appends every custom badge after the base set. That strands Potion Sale at the far end
// while Ka-Ching, the other Merchant badge, sits in the middle of the row. Run after BaseLib's postfix and
// move each listed badge into the slot right after its anchor. The run history stores the list in this
// order, so a run that ended before this patch keeps its old order
[HarmonyPatch]
[HarmonyAfter(BaseLib.BaseLibMain.ModId)]
[HarmonyPriority(Priority.Low)]
public static class BadgeOrderPatches
{
    // (anchor id, badge id): the badge moves to the slot after the anchor. Later pairs may anchor on an
    // earlier pair's badge, so the list is applied in order
    private static readonly (string Anchor, string Badge)[] Placement =
    {
        ("KACHING", new Badges.PotionSale().Id),
        // The Alchemist set reads in one fixed order instead of the assembly scan order
        (new Badges.Mixes().Id, new Badges.Fermented().Id),
        (new Badges.Fermented().Id, new Badges.AntitoxinPeak().Id),
        (new Badges.AntitoxinPeak().Id, new Badges.CleanRun().Id),
    };

    // Resolved by name because the beta branch's CreateAll takes a won flag that the main branch's does not
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
