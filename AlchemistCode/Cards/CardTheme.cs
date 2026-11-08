using System;

namespace Alchemist.AlchemistCode.Cards;

public enum CardTheme
{
    None,
    Poison,
    Potions,
    Antitoxin,
    Ferment,
    Transform,
    Mix,
}

// The analytics script reads this by regex, so keep it on the line right before the class
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CardThemeAttribute(params CardTheme[] themes) : Attribute
{
    public CardTheme[] Themes { get; } = themes;
}
