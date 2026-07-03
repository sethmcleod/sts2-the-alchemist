using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode.Patches;

/// <summary>
/// BaseLib builds custom-keyword hover tips with a null icon. This postfix rebuilds the tip
/// for our keywords with the same title/description plus an icon texture, so Ferment and
/// Gambit tooltips render an icon like power tips do.
/// </summary>
[HarmonyPatch(typeof(HoverTipFactory), nameof(HoverTipFactory.FromKeyword))]
public static class KeywordTipIconPatch
{
    public static void Postfix(CardKeyword keyword, ref IHoverTip __result)
    {
        string? iconName = null, locKey = null;
        if (keyword == AlchemistKeywords.Ferment) (iconName, locKey) = ("ferment", "ALCHEMIST-FERMENT");
        else if (keyword == AlchemistKeywords.Gambit) (iconName, locKey) = ("gambit", "ALCHEMIST-GAMBIT");
        else if (keyword == AlchemistKeywords.Seep) (iconName, locKey) = ("seep", "ALCHEMIST-SEEP");
        else if (keyword == AlchemistKeywords.Infuse) (iconName, locKey) = ("infuse", "ALCHEMIST-INFUSE");
        if (iconName == null) return;

        var tex = ResourceLoader.Load<Texture2D>(
            $"{MainFile.ResPath}/images/keywords/{iconName}.png", null, ResourceLoader.CacheMode.Reuse);
        if (tex == null) return;
        __result = new HoverTip(
            new LocString("card_keywords", $"{locKey}.title"),
            new LocString("card_keywords", $"{locKey}.description"),
            tex);
    }
}
