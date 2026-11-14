using System.Linq;
using Alchemist.AlchemistCode.Patches;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode;

public static class AlchemistTips
{
    public static IHoverTip Static(string key, string? icon = null)
    {
        var description = new LocString("static_hover_tips", key + ".description");
        description.Add("energyPrefix", EnergyIconHelper.GetPrefix(ModelDb.CardPool<Character.AlchemistCardPool>()));
        return new HoverTip(new LocString("static_hover_tips", key + ".title"), description,
            icon == null
                ? null
                : ResourceLoader.Load<Texture2D>($"{MainFile.ResPath}/images/keywords/{icon}.png",
                    null, ResourceLoader.CacheMode.Reuse));
    }

    public static IHoverTip FermentRef =>
        new HoverTip(new LocString("card_keywords", "ALCHEMIST-FERMENT_REF.title"),
            new LocString("card_keywords", "ALCHEMIST-FERMENT_REF.description"));

    private sealed class Cache
    {
        public required string Language;
        public IHoverTip? Brew, TransformMix, CompoundMix, MixHeader;
        public IHoverTip[]? Mix, MixUpgraded;
    }

    private static Cache? _cache;

    private static Cache Cached
    {
        get
        {
            var language = LocManager.Instance.Language;
            if (_cache?.Language != language) _cache = new Cache { Language = language };
            return _cache;
        }
    }

    public static IHoverTip Brew => Cached.Brew ??= Static("ALCHEMIST-BREW");

    public static IHoverTip TransformMix => Cached.TransformMix ??= Static("ALCHEMIST-TRANSFORM_MIX");

    // The numbers in these strings copy the token classes in Cards/Token, so change both together
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

    private static readonly string[] VolatileKeys =
        { "ALCHEMIST-FUMING_MIX", "ALCHEMIST-ACRID_MIX", "ALCHEMIST-SPARKLING_MIX" };

    private static IHoverTip[] Family(bool upgraded) =>
        BasicKeys.Concat(VolatileKeys).Select(k => Row(k, upgraded)).ToArray();

    public static IHoverTip MixHeader => Cached.MixHeader ??= Static("ALCHEMIST-MIX");

    public static IHoverTip[] Mix => Cached.Mix ??= Family(false);
    public static IHoverTip[] MixUpgraded => Cached.MixUpgraded ??= Family(true);

    public static IHoverTip CompoundMix => Cached.CompoundMix ??= Static("ALCHEMIST-COMPOUND_MIX", "mix_compound");
}
