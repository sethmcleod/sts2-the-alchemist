using Alchemist.AlchemistCode.Analytics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using AlchemistCharacter = Alchemist.AlchemistCode.Character.Alchemist;

namespace Alchemist.AlchemistCode.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen), "SelectCharacter")]
class AnalyticsConsentPatches
{
    [HarmonyPostfix]
    static void AfterSelect(NCharacterSelectButton charSelectButton)
    {
        if (charSelectButton.Character is AlchemistCharacter) AnalyticsConsent.AskOnce();
    }
}
