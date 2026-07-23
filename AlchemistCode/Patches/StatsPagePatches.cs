using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Saves;

namespace Alchemist.AlchemistCode.Patches;

// The stats screen builds one character section per base character with a
// hardcoded list in LoadStats, so a mod character never renders without a
// patch. This appends the Alchemist section. The section only appears once the
// save has stats for the character, which matches the base-game behavior.
// The duplicate guard follows RitsuLib's StatsScreenCharacterStatsPatch: a
// generic library can add mod characters on its own, so skip the append when a
// section for the Alchemist already exists
[HarmonyPatch(typeof(NGeneralStatsGrid), nameof(NGeneralStatsGrid.LoadStats))]
class StatsPagePatches
{
    private static readonly AccessTools.FieldRef<NGeneralStatsGrid, Control> ContainerRef =
        AccessTools.FieldRefAccess<NGeneralStatsGrid, Control>("_characterStatContainer");

    private static readonly AccessTools.FieldRef<NCharacterStats, CharacterStats> StatsRef =
        AccessTools.FieldRefAccess<NCharacterStats, CharacterStats>("_characterStats");

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    static void AfterLoadStats(NGeneralStatsGrid __instance)
    {
        var container = ContainerRef(__instance);
        if (container == null)
            return;

        var id = ModelDb.Character<Character.Alchemist>().Id;
        var stats = SaveManager.Instance.Progress.GetStatsForCharacter(id);
        if (stats == null || HasSectionFor(container, id))
            return;

        container.AddChild(NCharacterStats.Create(stats));
    }

    private static bool HasSectionFor(Node container, ModelId id)
    {
        foreach (var child in container.GetChildren())
        {
            if (child is NCharacterStats section && StatsRef(section)?.Id == id)
                return true;
        }
        return false;
    }
}
