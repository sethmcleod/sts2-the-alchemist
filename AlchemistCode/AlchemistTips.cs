using Alchemist.AlchemistCode.Patches;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode;

// Tips that more than one kind of entity shows. A card, a power, a relic and a potion each build their
// own tip list, so a keyword named by several of them has one definition here instead of one per caller
public static class AlchemistTips
{
    // Text lives in static_hover_tips.json under {key}.title and {key}.description
    public static IHoverTip Static(string key) =>
        new HoverTip(new LocString("static_hover_tips", key + ".title"),
            new LocString("static_hover_tips", key + ".description"));

    // Brew is a Rest Site option rather than a keyword, so the Kit relics that grant it explain it here
    public static IHoverTip Brew => Static("ALCHEMIST-BREW");


}
