using Godot;

namespace Alchemist.AlchemistCode.Extensions;

public static class StringExtensions
{
    public static string ImagePath(this string path)
    {
        return Path.Join(MainFile.ResPath, "images", path);
    }

    public static string CardImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "card_portraits", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find card image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "card_portraits", "card.png");
    }

    // The final art lives in card_portraits/<file>. Until a card has it, the current placeholder is the beta
    // art in card_portraits/beta/<file>, the same layout the base game decompiles to (<pool>/<file> and
    // <pool>/beta/<file>). Real art is added one card at a time: drop a file into card_portraits/ and it wins
    // over the beta placeholder. If neither exists, CardImagePath falls back to the generic card.png
    public static string CardImageOrBetaPath(this string file)
    {
        var real = Path.Join(MainFile.ResPath, "images", "card_portraits", file);
        return ResourceLoader.Exists(real) ? real : Path.Join("beta", file).CardImagePath();
    }

    public static string PowerImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "powers", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find power image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "powers", "power.png");
    }

    public static string BigPowerImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "powers", "big", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find big power image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "powers", "big", "power.png");
    }

    public static string RelicImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "relics", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find relic image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "relics", "relic.png");
    }

    public static string BigRelicImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "relics", "big", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find big relic image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "relics", "big", "relic.png");
    }

    public static string PotionImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "potions", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find potion image path: " + path);
        return path;
    }

    public static string PotionOutlinePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "potions", "outlines", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find potion outline path: " + path);
        return path;
    }

    public static string CharacterUiPath(this string path)
    {
        return Path.Join(MainFile.ResPath, "images", "charui", path);
    }

    public static string EnchantmentImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "enchantments", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find enchantment image path: " + path);
        return path;
    }
}