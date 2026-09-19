using Alchemist.AlchemistCode.Cards.AncientsAwakened;
using Alchemist.AlchemistCode.Cards.Hero;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Cards;

// One answer for every cross-mod card: is it in the game right now? The pool gate and the
// compendium click guard both ask this, so the reward roll and the library never disagree
internal static class CrossMod
{
    internal static bool Offered(CardModel card) => card switch
    {
        AlchemistHeroCard => HeroExpansion.Offered(card),
        AlchemistAncientsCard => AncientsAwakenedMod.IsLoaded,
        _ => true,
    };
}
