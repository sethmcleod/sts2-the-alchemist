using System.Collections.Generic;
using Alchemist.AlchemistCode.Cards.Ancient;
using Alchemist.AlchemistCode.Cards.Basic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Alchemist.AlchemistCode.Patches;

// ArchaicTooth holds the starter card that each character transcends, in a hardcoded dictionary of the
// five base characters: Bash -> Break, Neutralize -> Suppress, and so on. A modded character is absent,
// so two things go wrong. The Orobas event offers Archaic Tooth only when SetupForPlayer finds a starter
// card in that dictionary, so the Alchemist never sees the relic. Dusty Tome then excludes the
// transcendence cards through the same dictionary, so it can hand out Aureate, which is the one Ancient
// card that must come from Prime. Add the Prime -> Aureate pair, and both follow from it
[HarmonyPatch]
public static class TranscendencePatches
{
    [HarmonyPatch(typeof(ArchaicTooth), "TranscendenceUpgrades", MethodType.Getter)]
    [HarmonyPostfix]
    private static void AddAlchemistTranscendence(ref Dictionary<ModelId, CardModel> __result)
    {
        __result[ModelDb.Card<Prime>().Id] = ModelDb.Card<Aureate>();
    }
}
