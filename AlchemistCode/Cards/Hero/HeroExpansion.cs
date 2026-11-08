using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards.Hero;

// Reached by reflection only: a declared dependency that is absent fails this whole mod
internal static class HeroExpansion
{
    private const string Extensions = "TheHeroExpansion.TheHeroExpansionCode.Extensions.";
    private const string BladeRegistry = Extensions + "CustomBrokenBladeExtension";
    private const string FlashlightRegistry = Extensions + "CustomMysteriousFlashlightExtension";

    private static bool? _loaded;

    // Cached on first read, so read it only after every mod has loaded
    internal static bool IsLoaded => _loaded ??= AccessTools.TypeByName(FlashlightRegistry) != null;

    internal static bool CardsEnabled(bool multiplayer) =>
        IsLoaded || (Config.AlchemistModConfig.HeroCardsWithoutExpansion && !multiplayer);

    internal static bool Offered(CardModel card, bool multiplayer) =>
        card.Rarity == MegaCrit.Sts2.Core.Entities.Cards.CardRarity.Event ? IsLoaded : CardsEnabled(multiplayer);

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
