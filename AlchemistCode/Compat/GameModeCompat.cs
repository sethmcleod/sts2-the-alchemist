using System.Collections.Generic;
using System.Linq;
using Alchemist.AlchemistCode.Patches;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using MegaCrit.Sts2.Core.Saves.Managers;

// COMPAT-BRANCH: beta

namespace Alchemist.AlchemistCode.Compat;

[HarmonyPatch(typeof(NBlitzScreen))]
static class BlitzSelectBgPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NBlitzScreen.SelectCharacter))]
    static void AfterSelect(NBlitzScreen __instance) => CharSelectBgPatches.Apply(__instance);

    [HarmonyPostfix]
    [HarmonyPatch("DoRandomCharacterChange")]
    static void AfterRandom(NBlitzScreen __instance) => CharSelectBgPatches.Apply(__instance);
}

[HarmonyPatch(typeof(NDreadScreen), "SetSelectedCharacterVisuals")]
static class DreadSelectBgPatch
{
    static void Postfix(NDreadScreen __instance) => CharSelectBgPatches.Apply(__instance, "%AnimatedBg");
}

[HarmonyPatch(typeof(BlitzMerchantPrismaticCardRewardEntry), MethodType.Constructor, typeof(Player))]
static class PrismaticPoolPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        PoolPatches.StripCharacterReads(instructions, "The Blitz Prismatic card reward");
}

[HarmonyPatch(typeof(ProgressSaveManager), "UpdateEpochsPostRun")]
static class BaseCharacterEpochGatePatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var getter = AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters));
        var reads = codes.Where(c => c.Calls(getter)).ToList();
        if (reads.Count == 0)
        {
            MainFile.Logger.Warn("[Epochs] UpdateEpochsPostRun no longer reads ModelDb.AllCharacters, so modded "
                + "characters can block the Dread, Daily Run and Orobas unlocks");
            return codes;
        }
        var replacement = AccessTools.Method(typeof(BaseCharacterEpochGatePatch), nameof(BaseCharacters));
        foreach (var read in reads) read.operand = replacement;
        return codes;
    }

    static IEnumerable<CharacterModel> BaseCharacters() =>
        ModelDb.AllCharacters.Where(c => c.GetType().Assembly == typeof(CharacterModel).Assembly).ToList();
}
