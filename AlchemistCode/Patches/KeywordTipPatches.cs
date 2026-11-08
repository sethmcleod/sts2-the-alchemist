using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Patches;

public static class KeywordTipFactory
{
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

// BaseLib builds custom-keyword tips with no icon
[HarmonyPatch(typeof(HoverTipFactory), nameof(HoverTipFactory.FromKeyword))]
public static class KeywordTipIconPatch
{
    public static void Postfix(CardKeyword keyword, ref IHoverTip __result)
    {
        string? iconName = null, locKey = null;
        if (keyword == AlchemistKeywords.Ferment) (iconName, locKey) = ("ferment", "ALCHEMIST-FERMENT");
        else if (keyword == AlchemistKeywords.Laced) (iconName, locKey) = ("laced", "ALCHEMIST-LACED");
        if (iconName == null) return;

        __result = KeywordTipFactory.Build(iconName, $"{locKey}.title", $"{locKey}.description");
    }
}
