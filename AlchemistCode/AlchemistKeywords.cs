using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Alchemist.AlchemistCode;

/// <summary>
/// Custom card keywords for the mod. BaseLib scans <see cref="CustomEnumAttribute"/> fields on mod
/// types, assigns each a unique <see cref="CardKeyword"/> value at init, and registers its tooltip
/// (loc keys <c>ALCHEMIST-GAMBIT</c> / <c>ALCHEMIST-FERMENT</c> in card_keywords.json).
///
/// These are display/tooltip keywords; the actual gameplay lives on <see cref="Cards.AlchemistCard"/>
/// (Gambit = <c>IsReduced</c> HP check; Ferment = the fermented-turn counter).
/// </summary>
public static class AlchemistKeywords
{
    /// <summary>While the owner is at 50% or less HP, the card has an extra effect.</summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Gambit;

    /// <summary>The card's effect grows for each turn it is held in hand.</summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Ferment;
}
