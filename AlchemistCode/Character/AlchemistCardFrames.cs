using Alchemist.AlchemistCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Alchemist.AlchemistCode.Character;

// The painted card frame set: a frame per card type, a portrait border per type and rarity, and a title
// banner and a type plaque per rarity. A card takes the set only when all four of its pieces exist, so a
// card never mixes painted pieces with base ones. The file names are the artist's, so a new delivery
// goes into images/ui/cards without a rename
public static class AlchemistCardFrames
{
    public sealed record Set(Texture2D Frame, Texture2D Border, Texture2D Banner, Texture2D Plaque);

    // The artist's name for the set. It is part of every file name
    private const string Style = "ferment";

    private static readonly Dictionary<string, Texture2D?> Loaded = new();

    public static Set? For(CardModel card)
    {
        var type = TypeName(card.Type);
        var rarity = RarityName(card.Rarity);
        var frame = Load($"frame_{type}_{Style}.png");
        var border = Load($"border_{type}_{Style}_{rarity}.png");
        var banner = Load($"banner_{Style}_{rarity}.png");
        var plaque = Load($"plaque_{Style}_{rarity}.png");
        if (frame == null || border == null || banner == null || plaque == null) return null;
        return new Set(frame, border, banner, plaque);
    }

    // The base game draws Status and Curse cards on the Skill frame. The pool has neither
    private static string TypeName(CardType type) => type switch
    {
        CardType.Attack => "attack",
        CardType.Power => "power",
        _ => "skill",
    };

    // The base game tints Basic and Token cards with the Common banner material, so they share its pieces.
    // Ancient cards draw their own frame and never match a file
    private static string RarityName(CardRarity rarity) => rarity switch
    {
        CardRarity.Uncommon => "uncommon",
        CardRarity.Rare => "rare",
        CardRarity.Ancient => "ancient",
        _ => "common",
    };

    private static Texture2D? Load(string file)
    {
        if (Loaded.TryGetValue(file, out var texture)) return texture;

        var path = $"ui/cards/{file}".ImagePath();
        texture = ResourceLoader.Exists(path)
            ? ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse)
            : null;
        Loaded[file] = texture;
        return texture;
    }
}
