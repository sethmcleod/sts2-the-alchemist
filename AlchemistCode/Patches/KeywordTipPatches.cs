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

// BaseLib builds custom-keyword hover tips with a null icon; rebuild ours with an icon texture
[HarmonyPatch(typeof(HoverTipFactory), nameof(HoverTipFactory.FromKeyword))]
public static class KeywordTipIconPatch
{
    public static void Postfix(CardKeyword keyword, ref IHoverTip __result)
    {
        string? iconName = null, locKey = null;
        if (keyword == AlchemistKeywords.Ferment) (iconName, locKey) = ("ferment", "ALCHEMIST-FERMENT");
        else if (keyword == AlchemistKeywords.Infuse) (iconName, locKey) = ("infuse", "ALCHEMIST-INFUSE");
        else if (keyword == AlchemistKeywords.Decant) (iconName, locKey) = ("decant", "ALCHEMIST-DECANT");
        if (iconName == null) return;

        __result = KeywordTipFactory.Build(iconName, $"{locKey}.title", $"{locKey}.description");
    }
}
