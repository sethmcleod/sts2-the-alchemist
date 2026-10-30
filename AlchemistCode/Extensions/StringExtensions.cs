using Godot;

namespace Alchemist.AlchemistCode.Extensions;

public static class StringExtensions
{
    // A res:// path is matched against the pack index, which is keyed by forward slashes only. Never
    // build one with Path.Join: that joins with the platform separator, which is a backslash on
    // Windows, and every asset then fails to load there while macOS and Linux stay correct
    private static string Res(params string[] parts) => string.Join('/', parts);

    public static string ImagePath(this string path)
    {
        return Res(MainFile.ResPath, "images", path);
    }

    public static string CardImagePath(this string path)
    {
        path = Res(MainFile.ResPath, "images", "card_portraits", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Error("Could not find card image path: " + path);
        return Res(MainFile.ResPath, "images", "card_portraits", "card.png");
    }

    // Final art in card_portraits/<file> wins over the beta placeholder in card_portraits/beta/<file>, the
    // same layout the base game decompiles to, so art can land one card at a time. A card's Beta Art choice
    // turns that order around (CardArt.Portrait). With neither file, CardImagePath falls back to the generic
    // card.png. Big is the 1000x760 copy of the final art in card_portraits/big/<file>: the inspect screen
    // draws one card at twice its size, and that is the only view where it is sharper than the 500x380 copy
    public static CardArt ResolveCardArt(this string file)
    {
        var final = ExistingOrNull(Res(MainFile.ResPath, "images", "card_portraits", file));
        var beta = ExistingOrNull(Res(MainFile.ResPath, "images", "card_portraits", "beta", file));
        var big = ExistingOrNull(Res(MainFile.ResPath, "images", "card_portraits", "big", file));
        return new CardArt(final, beta, big, final ?? beta ?? Res("beta", file).CardImagePath());
    }

    private static string? ExistingOrNull(string path) => ResourceLoader.Exists(path) ? path : null;

    public static string PowerImagePath(this string path)
    {
        path = Res(MainFile.ResPath, "images", "powers", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Error("Could not find power image path: " + path);
        return Res(MainFile.ResPath, "images", "powers", "power.png");
    }

    public static string BigPowerImagePath(this string path)
    {
        path = Res(MainFile.ResPath, "images", "powers", "big", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Error("Could not find big power image path: " + path);
        return Res(MainFile.ResPath, "images", "powers", "big", "power.png");
    }

    public static string RelicImagePath(this string path)
    {
        path = Res(MainFile.ResPath, "images", "relics", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Error("Could not find relic image path: " + path);
        return Res(MainFile.ResPath, "images", "relics", "relic.png");
    }

    public static string BigRelicImagePath(this string path)
    {
        path = Res(MainFile.ResPath, "images", "relics", "big", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Error("Could not find big relic image path: " + path);
        return Res(MainFile.ResPath, "images", "relics", "big", "relic.png");
    }

    public static string PotionImagePath(this string path)
    {
        path = Res(MainFile.ResPath, "images", "potions", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Error("Could not find potion image path: " + path);
        return Res(MainFile.ResPath, "images", "potions", "potion.png");
    }

    public static string PotionOutlinePath(this string path)
    {
        path = Res(MainFile.ResPath, "images", "potions", "outlines", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Error("Could not find potion outline path: " + path);
        return Res(MainFile.ResPath, "images", "potions", "outlines", "potion.png");
    }

    public static string BadgeImagePath(this string path)
    {
        path = Res(MainFile.ResPath, "images", "badges", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Error("Could not find badge image path: " + path);
        return Res(MainFile.ResPath, "images", "badges", "badge.png");
    }

    public static string CharacterUiPath(this string path)
    {
        return Res(MainFile.ResPath, "images", "charui", path);
    }

    public static string EnchantmentImagePath(this string path)
    {
        path = Res(MainFile.ResPath, "images", "enchantments", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Error("Could not find enchantment image path: " + path);
        // The base game ships an "unknown enchantment" icon, so no mod placeholder is needed here
        return "res://images/enchantments/missing_enchantment.png";
    }
}