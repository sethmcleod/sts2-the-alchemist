using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Hero;

// The Hero Expansion (Workshop 3749294247) keeps two static registries for character mods: the card
// its Broken Blade transforms into, and the card its Mysterious Flashlight hands over. Both are
// reached by reflection so this mod neither references its dll nor names it in the manifest: a
// declared dependency that is absent fails the whole mod, and the game loads every mod into one
// AssemblyLoadContext, so a loaded type resolves by name. A card registers itself from its
// constructor, which ModelDb runs after every mod has loaded, the way The Sorceress does
internal static class HeroExpansion
{
    private const string Extensions = "TheHeroExpansion.TheHeroExpansionCode.Extensions.";
    private const string BladeRegistry = Extensions + "CustomBrokenBladeExtension";
    private const string FlashlightRegistry = Extensions + "CustomMysteriousFlashlightExtension";

    private static bool? _loaded;

    // Read after mod loading only: the pool filter and the card constructors both run later
    internal static bool IsLoaded => _loaded ??= AccessTools.TypeByName(FlashlightRegistry) != null;

    // Whether the Hero-only pool cards are offered: the mod is present, or the player asked for them anyway
    internal static bool CardsEnabled => IsLoaded || Config.AlchemistModConfig.HeroCardsWithoutExpansion;

    internal static bool Offered(CardModel card) =>
        card.Rarity == MegaCrit.Sts2.Core.Entities.Cards.CardRarity.Event ? IsLoaded : CardsEnabled;

    internal static void RegisterBlade(CardModel card) =>
        Register(BladeRegistry, "AddBladeCardForCustomCharacter", card);

    internal static void RegisterFlashlight(CardModel card) =>
        Register(FlashlightRegistry, "AddFlashlightCardForCustomCharacter", card);

    private static void Register(string registry, string method, CardModel card)
    {
        var type = AccessTools.TypeByName(registry);
        if (type == null) return;
        var add = AccessTools.DeclaredMethod(type, method);
        if (add == null)
        {
            MainFile.Logger.Warn($"The Hero Expansion is loaded but {registry}.{method} is missing, so {card.Id} is not registered");
            return;
        }
        try
        {
            add.Invoke(null, [card, ModelDb.Character<Character.Alchemist>()]);
            MainFile.Logger.Info($"Registered {card.Id} with The Hero Expansion through {method}");
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Failed to register {card.Id} with The Hero Expansion: {e}");
        }
    }
}
