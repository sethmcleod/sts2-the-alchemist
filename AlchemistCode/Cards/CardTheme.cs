using System;

namespace Alchemist.AlchemistCode.Cards;

// The themes a card may serve. Analytics groups runs by the theme with the most cards in the final
// deck, so a card that touches two mechanics lists both. None marks the neutral cards that serve no
// theme, which keeps "forgot to tag" and "deliberately untagged" apart for the linter
public enum CardTheme
{
    None,
    Poison,
    Infuse,
    Potions,
    Antitoxin,
    Ferment,
    Transform,
    Mix,
    Decant,
}

// Read by reflection at runtime for the analytics payload and by regex from tools/analytics/ for the
// dashboard's card metadata, so keep it on the line before the class declaration
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CardThemeAttribute(params CardTheme[] themes) : Attribute
{
    public CardTheme[] Themes { get; } = themes;
}
