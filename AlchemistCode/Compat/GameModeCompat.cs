using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alchemist.AlchemistCode.Cards;
using Alchemist.AlchemistCode.Patches;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Blitz;
using MegaCrit.Sts2.Core.Models.Enchantments;
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

[HarmonyPatch]
static class EnchantedRewardScopePatch
{
    internal static bool Active { get; private set; }

    static MethodBase? TargetMethod() =>
        typeof(BlitzMerchantEnchantedCardRewardEntry)
            .GetNestedTypes(BindingFlags.NonPublic)
            .Prepend(typeof(BlitzMerchantEnchantedCardRewardEntry))
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .FirstOrDefault(m => m.Name.Contains("EnchantCardsInReward"));

    static void Prefix() => Active = true;

    static void Finalizer() => Active = false;
}

[HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.CanEnchant))]
static class SteadyFermentPatch
{
    static void Postfix(EnchantmentModel __instance, CardModel card, ref bool __result)
    {
        if (!__result || !EnchantedRewardScopePatch.Active || __instance is not Steady) return;
        if (card is not AlchemistCard { IsFermentInline: true }) return;
        if (BlitzRunModel.shopEnchantmentOptions.Any(o => o.Item1 is not Steady && o.Item1.CanEnchant(card)))
            __result = false;
    }
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
