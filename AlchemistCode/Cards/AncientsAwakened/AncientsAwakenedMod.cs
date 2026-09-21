using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Alchemist.AlchemistCode.Cards.Basic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.AncientsAwakened;

// Ancients Awakened (Workshop 3747492675) reads two pieces of character content: Mithrix's Ancient
// Scepter turns the basic Strikes and Defends into a perfected pair, and Sebastian's Experimental
// Serum hands over an Ancient card. All of it is reached by reflection, no dll reference and no
// manifest entry, the same way The Sorceress and The Iudex do it. The Scepter keys its lookup by
// the starter card's id while the mod's own extension method files a custom entry under the
// character's id, which the Scepter never reads, so the two dictionaries are written directly and
// the Scepter's static caches are cleared afterwards. The Serum's extension method works as written
internal static class AncientsAwakenedMod
{
    private const string Code = "AncientsAwakened.AncientsAwakenedCode.";
    private const string PerfectedRegistry = Code + "Extensions.CustomPerfectedCardExtension";
    private const string ExperimentalRegistry = Code + "Extensions.CustomExperimentalCardExtension";
    private const string Scepter = Code + "Relics.Mithrix.AncientScepter";
    private const string PerfectedPool = Code + "Pools.Mithrix.PerfectedPool";

    private static bool? _loaded;
    private static bool _lookedForPool;
    private static CardPoolModel? _pool;

    internal static bool IsLoaded => _loaded ??= AccessTools.TypeByName(PerfectedRegistry) != null;

    // The cyan "perfected" frame the mod gives every character's pair, or ours when it is absent
    internal static CardPoolModel PerfectedPoolOr(CardPoolModel fallback)
    {
        if (!_lookedForPool)
        {
            _lookedForPool = true;
            var type = AccessTools.TypeByName(PerfectedPool);
            var cardPool = typeof(ModelDb).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "CardPool" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            if (type != null && cardPool != null)
            {
                try { _pool = cardPool.MakeGenericMethod(type).Invoke(null, null) as CardPoolModel; }
                catch (Exception e) { MainFile.Logger.Warn($"Ancients Awakened is loaded but its Perfected pool could not be read: {e.Message}"); }
            }
        }
        return _pool ?? fallback;
    }

    // Runs from the ModelDb.Init postfix, when every model and the other mod's statics exist
    internal static void Register()
    {
        if (!IsLoaded) return;
        var strike = RegisterPerfected("CustomPerfectedStrikeCards", ModelDb.Card<StrikeAlchemist>().Id!, ModelDb.Card<PotentStrike>().Id!);
        var defend = RegisterPerfected("CustomPerfectedDefendCards", ModelDb.Card<DefendAlchemist>().Id!, ModelDb.Card<PotentDefend>().Id!);
        if (strike || defend) ClearScepterCaches();
        RegisterExperimental(ModelDb.Card<Hiccup>());
    }

    private static bool RegisterPerfected(string dictionaryField, ModelId starter, ModelId perfected)
    {
        var type = AccessTools.TypeByName(PerfectedRegistry);
        var field = type?.GetField(dictionaryField, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (field?.GetValue(null) is not IDictionary dictionary)
        {
            MainFile.Logger.Warn($"Ancients Awakened is loaded but {PerfectedRegistry}.{dictionaryField} is missing, so {perfected} is not registered");
            return false;
        }
        dictionary[starter] = perfected;
        MainFile.Logger.Info($"Registered {perfected} with Ancients Awakened as the perfected {starter}");
        return true;
    }

    private static void ClearScepterCaches()
    {
        var type = AccessTools.TypeByName(Scepter);
        foreach (var name in new[] { "_perfectedStrikeUpgrades", "_perfectedDefendUpgrades" })
            type?.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
    }

    private static void RegisterExperimental(CardModel card)
    {
        var type = AccessTools.TypeByName(ExperimentalRegistry);
        var add = type == null ? null : AccessTools.DeclaredMethod(type, "AddExperimentalCardForCustomCharacters");
        if (add == null)
        {
            MainFile.Logger.Warn($"Ancients Awakened is loaded but {ExperimentalRegistry}.AddExperimentalCardForCustomCharacters is missing, so {card.Id} is not registered");
            return;
        }
        try
        {
            add.Invoke(null, [card, ModelDb.Character<Character.Alchemist>()]);
            MainFile.Logger.Info($"Registered {card.Id} with Ancients Awakened as the Experimental Serum card");
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Failed to register {card.Id} with Ancients Awakened: {e}");
        }
    }
}
