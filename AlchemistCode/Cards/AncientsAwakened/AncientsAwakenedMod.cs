using Alchemist.AlchemistCode.Cards.Basic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.AncientsAwakened;

internal static class AncientsAwakenedMod
{
    internal const string ModId = "AncientsAwakened";
    private const string Code = "AncientsAwakened.AncientsAwakenedCode.CrossModCompatability.";
    internal const string PerfectedRegistry = Code + "CustomPerfectedCardExtension";
    internal const string ExperimentalRegistry = Code + "CustomExperimentalCardExtension";

    private static bool? _loaded;

    internal static bool IsLoaded => _loaded ??= AccessTools.TypeByName(PerfectedRegistry) != null;

    internal static CardPoolModel PerfectedPoolOr(CardPoolModel fallback) =>
        AncientsAwakenedPerfectedInterop.PerfectedCardPool(fallback) ?? fallback;

    internal static void Register()
    {
        if (!IsLoaded) return;
        AncientsAwakenedPerfectedInterop.AddPerfectedCardForCustomCharacters(
            ModelDb.Card<PotentStrike>(), ModelDb.Card<StrikeAlchemist>());
        AncientsAwakenedPerfectedInterop.AddPerfectedCardForCustomCharacters(
            ModelDb.Card<PotentDefend>(), ModelDb.Card<DefendAlchemist>());
        AncientsAwakenedExperimentalInterop.AddExperimentalCardForCustomCharacters(
            ModelDb.Card<Hiccup>(), ModelDb.Character<Character.Alchemist>());
        MainFile.Logger.Info("Registered Potent Strike, Potent Defend and Hiccup with Ancients Awakened");
    }
}
