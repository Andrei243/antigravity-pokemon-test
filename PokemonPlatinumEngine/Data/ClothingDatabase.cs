using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One garment of <c>clothes.json</c>: what it is called, where it is worn, what it costs and how it looks, as
/// values the character kit already has (a colour and a second colour, and the shape's few switches). A garment that
/// costs nothing is everyone's from the start (no hat, no bag).
/// </summary>
public sealed class Garment
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ClothingSlot Slot { get; set; }
    public int Price { get; set; }

    /// <summary>A line about it for the wardrobe's card.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Its main colour as <c>#rrggbb</c>: the hat's crown, the top, the bottoms, the shoes or the bag. A hat or a
    /// bag with no colour is none at all (bare hair, nothing on the back).
    /// </summary>
    public string? Color { get; set; }

    /// <summary>Its second colour: the hat's band, the top's collar or scarf, the shoes' soles.</summary>
    public string? Accent { get; set; }

    /// <summary>A hat's kind: <c>Beret</c> or <c>Cap</c>.</summary>
    public string? Hat { get; set; }

    // A top's shape
    public bool Coat { get; set; }
    public bool ShortSleeves { get; set; }
    public bool Stripes { get; set; }
    public bool Scarf { get; set; }

    // The bottoms' shape: trousers unless one of these
    public bool Skirt { get; set; }
    public bool Shorts { get; set; }

    public bool IsFree => Price <= 0;

    /// <summary>True for "no hat" and "no bag": the slot left empty.</summary>
    public bool IsNothing => Color == null;

    /// <summary>A <c>#rrggbb</c> colour as its three channels.</summary>
    public static (byte R, byte G, byte B) Rgb(string hex)
    {
        string h = hex.TrimStart('#');
        if (h.Length != 6 || !int.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int v))
            throw new FormatException($"'{hex}' is not a colour written #rrggbb");
        return ((byte)(v >> 16), (byte)(v >> 8), (byte)v);
    }
}

/// <summary>What <c>clothes.json</c> holds: every garment, and what each boutique sells by its key.</summary>
public sealed class ClothingData
{
    public List<Garment> Garments { get; set; } = new();
    public Dictionary<string, List<string>> Boutiques { get; set; } = new();
}

/// <summary>
/// The clothes the player can wear (plan 11 · C9 and C10, first part): the garments, and the boutiques' stock by key
/// (<c>wardrobe "jubilife"</c> in a script). Our own clothes; Platinum has no boutique, so none of this is imported.
/// </summary>
public static class ClothingDatabase
{
    public const string FileName = "clothes.json";

    private static readonly ClothingData Data = GameDataFiles.Load<ClothingData>(FileName);
    private static readonly Dictionary<string, Garment> ById = Data.Garments.ToDictionary(g => g.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<Garment> All => Data.Garments;

    public static IReadOnlyDictionary<string, List<string>> Boutiques => Data.Boutiques;

    public static Garment? Get(string? id) => id != null && ById.TryGetValue(id, out var g) ? g : null;

    /// <summary>What a boutique sells, in the file's order; an unknown key sells nothing.</summary>
    public static List<Garment> Stock(string? boutique) =>
        boutique != null && Data.Boutiques.TryGetValue(boutique, out var ids) ? ids.Select(Get).OfType<Garment>().ToList() : new();

    /// <summary>The free garments: everyone's from the start.</summary>
    public static IEnumerable<Garment> Free => Data.Garments.Where(g => g.IsFree);
}
