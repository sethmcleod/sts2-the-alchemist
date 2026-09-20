using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Helpers;
using System.Linq;
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
    public static IHoverTip Static(string key, string? icon = null)
    {
        var description = new LocString("static_hover_tips", key + ".description");
        // Any tip may print an energy icon; the prefix picks the character's own colour
        description.Add("energyPrefix", EnergyIconHelper.GetPrefix(ModelDb.CardPool<Character.AlchemistCardPool>()));
        return new HoverTip(new LocString("static_hover_tips", key + ".title"), description,
            icon == null
                ? null
                : ResourceLoader.Load<Texture2D>($"{MainFile.ResPath}/images/keywords/{icon}.png",
                    null, ResourceLoader.CacheMode.Reuse));
    }

    // The Ferment keyword tip speaks about "this card". Cards that only REFERENCE Ferment
    // (Taste Test, Bloom, Mellow) need the plural reading, or the tip claims the card ferments
    public static IHoverTip FermentRef =>
        new HoverTip(new LocString("card_keywords", "ALCHEMIST-FERMENT_REF.title"),
            new LocString("card_keywords", "ALCHEMIST-FERMENT_REF.description"));

    // Brew is a Rest Site option rather than a keyword, so the Kit relics that grant it explain it here
    private static IHoverTip? _brew, _transformMix, _compoundMix;
    public static IHoverTip Brew => _brew ??= Static("ALCHEMIST-BREW");

    // The base Transform tip promises a random card of any rarity, which a Mix maker does not deliver
    public static IHoverTip TransformMix => _transformMix ??= Static("ALCHEMIST-TRANSFORM_MIX");

    // One header plus a compact row per Mix, the way an Enchanted card lists its enchantments.
    // Six full card tips stack past the screen edge, and one combined tip reads as a wall of
    // text, so each Mix gets its own row with its card-art color as the icon.
    // Built once: LocStrings resolve their text lazily, so caching the tips is locale-safe, and
    // the tip providers re-read this on every hover.
    // The numbers in these strings are hand-copies of the token classes in Cards/Token; a change to
    // a Mix's numbers must touch both, or the tips lie
    private static IHoverTip Row(string mixKey, bool upgraded) =>
        Static(upgraded ? mixKey + "_PLUS" : mixKey, MixIcon(mixKey));

    private static string MixIcon(string mixKey) => mixKey switch
    {
        "ALCHEMIST-BURSTING_MIX" => "mix_bursting",
        "ALCHEMIST-SYRUPY_MIX" => "mix_syrupy",
        "ALCHEMIST-ZESTY_MIX" => "mix_zesty",
        "ALCHEMIST-FUMING_MIX" => "mix_fuming",
        "ALCHEMIST-ACRID_MIX" => "mix_acrid",
        _ => "mix_sparkling",
    };

    private static readonly string[] BasicKeys =
        { "ALCHEMIST-BURSTING_MIX", "ALCHEMIST-SYRUPY_MIX", "ALCHEMIST-ZESTY_MIX" };

    private static readonly string[] SpecialKeys =
        { "ALCHEMIST-FUMING_MIX", "ALCHEMIST-ACRID_MIX", "ALCHEMIST-SPARKLING_MIX" };

    // The row list is for makers that can produce four or more kinds; a maker of three or fewer
    // previews the token cards themselves instead
    private static IHoverTip[] Family(bool upgraded) =>
        new[] { Static("ALCHEMIST-MIX") }
            .Concat(BasicKeys.Concat(SpecialKeys).Select(k => Row(k, upgraded))).ToArray();

    private static IHoverTip[]? _mix, _mixUpgraded;

    // Relics show the family header alone: the six rows overflow the relic view
    public static IHoverTip MixHeader => _mixHeader ??= Static("ALCHEMIST-MIX");
    private static IHoverTip? _mixHeader;

    public static IHoverTip[] Mix => _mix ??= Family(false);
    public static IHoverTip[] MixUpgraded => _mixUpgraded ??= Family(true);

    // A maker that produces one fixed Mix tips the family header plus that Mix's row only
    public static IHoverTip[] MixSingle(string mixKey, string icon) =>
        [Static("ALCHEMIST-MIX"), Static(mixKey, icon)];

    public static IHoverTip CompoundMix => _compoundMix ??= Static("ALCHEMIST-COMPOUND_MIX", "mix_compound");
}
