using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Patches;

public static class KeywordTipFactory
{
    // A null texture is valid here: it is what BaseLib itself produces for a custom keyword
    public static IHoverTip Build(string iconName, string titleKey, string descriptionKey)
    {
        var tex = ResourceLoader.Load<Texture2D>(
            $"{MainFile.ResPath}/images/keywords/{iconName}.png", null, ResourceLoader.CacheMode.Reuse);
        return new HoverTip(
            new LocString("card_keywords", titleKey),
            new LocString("card_keywords", descriptionKey),
            tex);
    }
}

// BaseLib builds custom-keyword hover tips with a null icon; rebuild ours with an icon texture.
//
// Reaction is handled here only as a fallback for a caller that has nothing but the keyword. Cards must
// NOT use this path: AlchemistCard builds its own Reaction tip so the text can name that card's own
// condition. The trap is that this fallback returns a well-formed, correctly-iconed tip with the GENERIC
// wording, so a card calling FromKeyword(Reaction) looks right and reads wrong. That is exactly how the
// condition-specific wording regressed once already
[HarmonyPatch(typeof(HoverTipFactory), nameof(HoverTipFactory.FromKeyword))]
public static class KeywordTipIconPatch
{
    public static void Postfix(CardKeyword keyword, ref IHoverTip __result)
    {
        string? iconName = null, locKey = null;
        if (keyword == AlchemistKeywords.Ferment) (iconName, locKey) = ("ferment", "ALCHEMIST-FERMENT");
        else if (keyword == AlchemistKeywords.Gambit) (iconName, locKey) = ("gambit", "ALCHEMIST-GAMBIT");
        else if (keyword == AlchemistKeywords.Reaction) (iconName, locKey) = ("reaction", "ALCHEMIST-REACTION");
        else if (keyword == AlchemistKeywords.Infuse) (iconName, locKey) = ("infuse", "ALCHEMIST-INFUSE");
        if (iconName == null) return;

        __result = KeywordTipFactory.Build(iconName, $"{locKey}.title", $"{locKey}.description");
    }
}
