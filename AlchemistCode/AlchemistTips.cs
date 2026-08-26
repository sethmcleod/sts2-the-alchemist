using Alchemist.AlchemistCode.Patches;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace Alchemist.AlchemistCode;

// Tips that more than one kind of entity shows. A card, a power, a relic and a potion each build their
// own tip list, so a keyword named by several of them has one definition here instead of one per caller
public static class AlchemistTips
{
    // Text lives in static_hover_tips.json under {key}.title and {key}.description
    public static IHoverTip Static(string key, string? icon = null) =>
        new HoverTip(new LocString("static_hover_tips", key + ".title"),
            new LocString("static_hover_tips", key + ".description"),
            icon == null
                ? null
                : ResourceLoader.Load<Texture2D>($"{MainFile.ResPath}/images/keywords/{icon}.png",
                    null, ResourceLoader.CacheMode.Reuse));

    // Brew is a Rest Site option rather than a keyword, so the Kit relics that grant it explain it here
    public static IHoverTip Brew => Static("ALCHEMIST-BREW");

    // One header plus a compact row per Mix, the way an Enchanted card lists its enchantments.
    // Four full card tips stack past the screen edge, and one combined tip reads as a wall of
    // text, so each Mix gets its own row with its card-art color as the icon.
    // Built once: LocStrings resolve their text lazily, so caching the tips is locale-safe, and
    // eleven tip providers re-read this on every hover
    private static IHoverTip[]? _mix;

    public static IHoverTip[] Mix => _mix ??=
    [
        Static("ALCHEMIST-MIX"),
        Static("ALCHEMIST-BURSTING_MIX", "mix_bursting"),
        Static("ALCHEMIST-FUMING_MIX", "mix_fuming"),
        Static("ALCHEMIST-SYRUPY_MIX", "mix_syrupy"),
        Static("ALCHEMIST-ZESTY_MIX", "mix_zesty"),
    ];

    // The + rows for cards that hand out upgraded Mixes (Mash). The numbers in these strings and
    // in the base rows above are hand-copies of the token classes in Cards/Token; a change to a
    // Mix's numbers must touch both, or the tips lie the way they did before v0.13.1
    private static IHoverTip[]? _mixUpgraded;

    public static IHoverTip[] MixUpgraded => _mixUpgraded ??=
    [
        Static("ALCHEMIST-MIX"),
        Static("ALCHEMIST-BURSTING_MIX_PLUS", "mix_bursting"),
        Static("ALCHEMIST-FUMING_MIX_PLUS", "mix_fuming"),
        Static("ALCHEMIST-SYRUPY_MIX_PLUS", "mix_syrupy"),
        Static("ALCHEMIST-ZESTY_MIX_PLUS", "mix_zesty"),
    ];
}
