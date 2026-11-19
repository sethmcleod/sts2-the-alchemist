using Alchemist.AlchemistCode.Cards.AncientsAwakened;
using Alchemist.AlchemistCode.Cards.Hero;
using Alchemist.AlchemistCode.Cards.ReshuffledSpire;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards;

internal static class CrossMod
{
    internal static bool RollingDustyTome { get; set; }

    internal static bool Offered(CardModel card, bool multiplayer = false) => card switch
    {
        AlchemistHeroCard => HeroExpansion.Offered(card, multiplayer),
        AlchemistAncientsCard => AncientsAwakenedMod.IsLoaded && !RollingDustyTome,
        AlchemistReshuffledCard => ReshuffledOffered(card, multiplayer) && !RollingDustyTome,
        _ => true,
    };

    private static bool ReshuffledOffered(CardModel card, bool multiplayer) =>
        ReshuffledSpireMod.IsLoaded
        || (card.Rarity != CardRarity.Ancient && Config.AlchemistModConfig.ReshuffledCardsWithoutMod && !multiplayer);

    internal static IHoverTip? Tip(CardModel card) => card switch
    {
        AlchemistHeroCard => AlchemistTips.Static(card.Rarity == CardRarity.Event
            ? "ALCHEMIST-HERO_EXPANSION_ONLY"
            : "ALCHEMIST-HERO_EXPANSION_OPTIONAL"),
        AlchemistAncientsCard => AlchemistTips.Static("ALCHEMIST-ANCIENTS_AWAKENED_ONLY"),
        AlchemistReshuffledCard => AlchemistTips.Static(card.Rarity == CardRarity.Ancient
            ? "ALCHEMIST-RESHUFFLED_SPIRE_ONLY"
            : "ALCHEMIST-RESHUFFLED_SPIRE_OPTIONAL"),
        _ => null,
    };

    internal static IEnumerable<IHoverTip> OutOfCombatTips(CardModel card)
    {
        if (card.CombatState == null && Tip(card) is { } tip) yield return tip;
    }
}
