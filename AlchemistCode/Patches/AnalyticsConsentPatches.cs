using Alchemist.AlchemistCode.Analytics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using AlchemistCharacter = Alchemist.AlchemistCode.Character.Alchemist;

namespace Alchemist.AlchemistCode.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen), "SelectCharacter")]
class AnalyticsConsentPatches
{
    [HarmonyPostfix]
    static void AfterSelect(CharacterModel characterModel)
    {
        if (characterModel is AlchemistCharacter) AnalyticsConsent.AskOnce();
    }
}
