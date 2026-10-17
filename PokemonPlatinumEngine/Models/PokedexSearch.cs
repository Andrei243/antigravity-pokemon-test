using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>The orders Platinum's Pokédex search offers.</summary>
public enum PokedexOrder { Number, Alphabet, Heaviest, Lightest, Tallest, Smallest }

/// <summary>A Pokédex search: an order, and any of the first letter of the name, one or two types and a body shape.</summary>
public sealed record PokedexQuery(
    PokedexOrder Order = PokedexOrder.Number, int NameGroup = -1, PokemonType? Type1 = null, PokemonType? Type2 = null, string? Shape = null)
{
    /// <summary>A search that keeps every species in number order: the plain list.</summary>
    public bool IsPlain => Order == PokedexOrder.Number && NameGroup < 0 && Type1 == null && Type2 == null && Shape == null;
}

/// <summary>
/// Platinum's Pokédex search: it only looks among the species the player has seen, and the orders by weight and
/// height only among those caught, whose size the Pokédex knows.
/// </summary>
public static class PokedexSearch
{
    /// <summary>The groups of first letters the search offers, as Platinum's does.</summary>
    public static readonly string[] LatinNameGroups = { "ABC", "DEF", "GHI", "JKL", "MNO", "PQR", "STU", "VWX", "YZ" };

    /// <summary>The groups the chosen language offers: its own (<see cref="Language.NameGroups"/>) or the Latin alphabet's.</summary>
    public static IReadOnlyList<string> NameGroups => Text.Language.NameGroups ?? LatinNameGroups;

    /// <summary>The fourteen body shapes the search offers (the Pokédex's shapes, as in <c>species.json</c>).</summary>
    public static readonly string[] Shapes =
    {
        "Ball", "Squiggle", "Fish", "Arms", "Blob", "Upright", "Legs", "Quadruped", "Wings", "Tentacles", "Heads", "Humanoid", "Bug Wings", "Armor"
    };

    public static List<PokedexEntry> Run(Pokedex dex, string mode, PokedexQuery query)
    {
        bool bySize = query.Order is PokedexOrder.Heaviest or PokedexOrder.Lightest or PokedexOrder.Tallest or PokedexOrder.Smallest;
        var found = Pokedex.Entries(mode).Where(e => dex.IsSeen(e.Species.DexNumber) && (!bySize || dex.IsCaught(e.Species.DexNumber)));
        if (query.NameGroup >= 0) found = found.Where(e => NameGroups[query.NameGroup].Contains(GroupLetter(e.Species.DisplayName)));
        if (query.Type1 is { } t1) found = found.Where(e => Has(e.Species, t1));
        if (query.Type2 is { } t2) found = found.Where(e => Has(e.Species, t2));
        if (query.Shape != null) found = found.Where(e => e.Species.Shape == query.Shape);
        return (query.Order switch
        {
            PokedexOrder.Alphabet => found.OrderBy(e => e.Species.DisplayName, Collation()),
            PokedexOrder.Heaviest => found.OrderByDescending(e => e.Species.Weight),
            PokedexOrder.Lightest => found.OrderBy(e => e.Species.Weight),
            PokedexOrder.Tallest => found.OrderByDescending(e => e.Species.Height),
            PokedexOrder.Smallest => found.OrderBy(e => e.Species.Height),
            _ => found.OrderBy(e => e.Number)
        }).ThenBy(e => e.Number).ToList();
    }

    private static bool Has(PokemonSpecies s, PokemonType type) => s.PrimaryType == type || s.SecondaryType == type;

    /// <summary>A name's first letter in capitals, without any accent, past any mark before it.</summary>
    public static char FirstLetter(string name)
    {
        string plain = name.Normalize(NormalizationForm.FormD);
        foreach (char c in plain)
            if (char.IsLetter(c)) return char.ToUpperInvariant(c);
        return ' ';
    }

    /// <summary>
    /// How the alphabetical order sorts the names: English's by character (as it always did), another language's by its
    /// own culture's rules. The pseudo-language brackets every name alike, so it sorts as English.
    /// </summary>
    public static StringComparer Collation()
    {
        var language = Text.Language;
        if (language.Tag == Text.English || language.Pseudo) return StringComparer.Ordinal;
        try { return StringComparer.Create(CultureInfo.GetCultureInfo(language.Tag), ignoreCase: false); }
        catch (CultureNotFoundException) { return StringComparer.Ordinal; }
    }

    /// <summary>
    /// The letter a name is filed under in the chosen language's groups: <see cref="FirstLetter"/> for the Latin
    /// alphabet; for kana the first kana as hiragana without its marks (a katakana name files with the hiragana row
    /// that sounds the same), for Hangul the syllable's initial consonant.
    /// </summary>
    public static char GroupLetter(string name)
    {
        foreach (char c in name)
        {
            if (c is >= '가' and <= '힣') return (char)InitialConsonants[(c - '가') / 588];
            if (c is >= 'ァ' and <= 'ヶ') return Unmarked((char)(c - 0x60));
            if (c is >= 'ぁ' and <= 'ゖ') return Unmarked(c);
            if (char.IsLetter(c) && c < 'ɐ') return FirstLetter(name);
        }
        return FirstLetter(name);
    }

    /// <summary>The compatibility letters of the nineteen initial consonants a Hangul syllable can begin with, in the syllables' order.</summary>
    private static readonly int[] InitialConsonants =
        { 0x3131, 0x3132, 0x3134, 0x3137, 0x3138, 0x3139, 0x3141, 0x3142, 0x3143, 0x3145, 0x3146, 0x3147, 0x3148, 0x3149, 0x314A, 0x314B, 0x314C, 0x314D, 0x314E };

    /// <summary>A hiragana without its dakuten or handakuten (が to か, ぱ to は).</summary>
    private static char Unmarked(char hiragana)
    {
        string plain = hiragana.ToString().Normalize(NormalizationForm.FormD);
        return plain[0];
    }

    /// <summary>What an order is called on the screen.</summary>
    public static string NameOf(PokedexOrder order) => Text.Of($"pokedex.orderName.{order}");

    /// <summary>What a body shape is called on the screen (the data's name, <c>Bug Wings</c>, is the key without its space).</summary>
    public static string NameOfShape(string shape)
    {
        string key = shape.Replace(" ", "");
        return Text.Of($"pokedex.shape.{key}");
    }

    /// <summary>Every name group's label: "A B C".</summary>
    public static string NameOfGroup(int group) => group < 0 ? Text.Of("pokedex.search.any")
        : NameGroups[group].Length <= 4 ? string.Join(' ', NameGroups[group].ToCharArray()) : NameGroups[group][0].ToString();

    /// <summary>The weight a list sorted by size shows beside each species, or null when the order isn't by size.</summary>
    public static string? SizeLabel(PokedexOrder order, PokemonSpecies s) => order switch
    {
        PokedexOrder.Heaviest or PokedexOrder.Lightest => Text.Of("pokedex.kilograms", ("n", s.Weight.ToString("0.0", CultureInfo.InvariantCulture))),
        PokedexOrder.Tallest or PokedexOrder.Smallest => Text.Of("pokedex.metres", ("n", s.Height.ToString("0.0", CultureInfo.InvariantCulture))),
        _ => null
    };
}
