using Alchemist.AlchemistCode.Cards.AncientsAwakened;
using Alchemist.AlchemistCode.Cards.Hero;
using Alchemist.AlchemistCode.Cards.ReshuffledSpire;
using MegaCrit.Sts2.Core.Entities.Cards;
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
}
