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

/// <summary>
/// A set offered in the new-game introduction (plan 11 · C10): garments of the list, one to a slot, a slot left out
/// being the look's own; <see cref="For"/> keeps it to the boy or the girl (null: both).
/// </summary>
public sealed class OutfitSet
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Garments { get; set; } = new();
    public PlayerLook? For { get; set; }

    /// <summary>The outfit the set is: each garment in its own slot (garments the data doesn't know are left off).</summary>
    public Outfit Outfit =>
        Garments.Select(ClothingDatabase.Get).OfType<Garment>().Aggregate(Outfit.Own, (o, g) => o.With(g.Slot, g.Id));

    public bool Suits(PlayerLook look) => For == null || For == look;
}

/// <summary>What <c>clothes.json</c> holds: every garment, what each boutique sells by its key, and the introduction's sets.</summary>
public sealed class ClothingData
{
    public List<Garment> Garments { get; set; } = new();
    public Dictionary<string, List<string>> Boutiques { get; set; } = new();
    public List<OutfitSet> Sets { get; set; } = new();

    /// <summary>The salon's cuts (<c>HairCut</c> by name), colours, eyes and tones (plan 43 · LF3).</summary>
    public List<string> Hair { get; set; } = new();
    public List<SalonColour> HairColours { get; set; } = new();
    public List<SalonColour> EyeColours { get; set; } = new();
    public List<SalonColour> SkinTones { get; set; } = new();

    /// <summary>The dye house's dyes (plan 43 · LF3).</summary>
    public List<Dye> Dyes { get; set; } = new();

    /// <summary>What the salon's chair and the dye house's counter ask.</summary>
    public SalonPrices Prices { get; set; } = new();
}

/// <summary>A colour of the salon's (plan 43 · LF3): its id, named in <c>ui.json</c>'s <c>salon</c> section, and <c>#rrggbb</c>.</summary>
public sealed class SalonColour
{
    public string Id { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
}

/// <summary>A dye of the dye house (plan 43 · LF3): the colour and accent it gives a garment, the berry it is made with, and whether a counter sells it.</summary>
public sealed class Dye
{
    public string Id { get; set; } = string.Empty;
    public string Color { get; set; } = "#ffffff";
    public string Accent { get; set; } = "#ffffff";
    public string Berry { get; set; } = string.Empty;
    public bool Sold { get; set; } = true;
}

/// <summary>What each change at the salon costs, and a dye at the counter (plan 43 · LF3).</summary>
public sealed class SalonPrices
{
    public int Cut { get; set; } = 2000;
    public int Colour { get; set; } = 1500;
    public int Eyes { get; set; } = 1000;
    public int Skin { get; set; } = 1000;
    public int Dye { get; set; } = 800;
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

    /// <summary>
    /// A garment by id; a dyed one (<c>striped_tee~mint</c>, plan 43 · LF3) is its garment in the dye's colours, named
    /// for both. Null for an id the data doesn't know, a dye on nothing, or a dye on a garment that has no colour.
    /// </summary>
    public static Garment? Get(string? id)
    {
        if (id == null) return null;
        if (ById.TryGetValue(id, out var g)) return g;
        int at = id.IndexOf(DyeMark);
        if (at <= 0 || !ById.TryGetValue(id[..at], out var plain) || plain.IsNothing || DyeOf(id[(at + 1)..]) is not { } dye) return null;
        // Made afresh each time, so its name follows the language chosen
        return new Garment
        {
            Id = plain.Id + DyeMark + dye.Id, Name = Text.Of("salon.dyedName", ("garment", plain.Name), ("dye", Text.Of($"salon.dye.{dye.Id}"))),
            Slot = plain.Slot, Price = plain.Price, Description = plain.Description,
            Color = dye.Color, Accent = dye.Accent, Hat = plain.Hat, Coat = plain.Coat, ShortSleeves = plain.ShortSleeves,
            Stripes = plain.Stripes, Scarf = plain.Scarf, Skirt = plain.Skirt, Shorts = plain.Shorts
        };
    }

    /// <summary>What joins a garment's id and its dye's in a dyed garment's id.</summary>
    public const char DyeMark = '~';

    /// <summary>A dyed garment's id: the garment's and the dye's.</summary>
    public static string DyedId(string garment, string dye) => PlainOf(garment) + DyeMark + dye;

    /// <summary>A garment's own id with any dye taken off.</summary>
    public static string PlainOf(string garment) => garment.IndexOf(DyeMark) is var at and > 0 ? garment[..at] : garment;

    /// <summary>The dye a garment's id names; null for a garment not dyed.</summary>
    public static Dye? DyeIn(string garment) => garment.IndexOf(DyeMark) is var at and > 0 ? DyeOf(garment[(at + 1)..]) : null;

    public static IReadOnlyList<string> Cuts => Data.Hair;
    public static IReadOnlyList<SalonColour> HairColours => Data.HairColours;
    public static IReadOnlyList<SalonColour> EyeColours => Data.EyeColours;
    public static IReadOnlyList<SalonColour> SkinTones => Data.SkinTones;
    public static IReadOnlyList<Dye> Dyes => Data.Dyes;
    public static SalonPrices Prices => Data.Prices;

    public static Dye? DyeOf(string? id) => Data.Dyes.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));
    public static SalonColour? HairColourOf(string? id) => Data.HairColours.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
    public static SalonColour? EyeColourOf(string? id) => Data.EyeColours.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
    public static SalonColour? SkinToneOf(string? id) => Data.SkinTones.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>What a boutique sells, in the file's order; an unknown key sells nothing.</summary>
    public static List<Garment> Stock(string? boutique) =>
        boutique != null && Data.Boutiques.TryGetValue(boutique, out var ids) ? ids.Select(Get).OfType<Garment>().ToList() : new();

    /// <summary>The free garments: everyone's from the start.</summary>
    public static IEnumerable<Garment> Free => Data.Garments.Where(g => g.IsFree);

    /// <summary>Every set of the introduction, in the file's order.</summary>
    public static IReadOnlyList<OutfitSet> Sets => Data.Sets;

    /// <summary>The sets offered to <paramref name="look"/>, in the file's order.</summary>
    public static List<OutfitSet> SetsFor(PlayerLook look) => Data.Sets.Where(s => s.Suits(look)).ToList();
}
