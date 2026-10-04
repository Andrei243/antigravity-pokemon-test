using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
    public static readonly string[] NameGroups = { "ABC", "DEF", "GHI", "JKL", "MNO", "PQR", "STU", "VWX", "YZ" };

    /// <summary>The fourteen body shapes the search offers (the Pokédex's shapes, as in <c>species.json</c>).</summary>
    public static readonly string[] Shapes =
    {
        "Ball", "Squiggle", "Fish", "Arms", "Blob", "Upright", "Legs", "Quadruped", "Wings", "Tentacles", "Heads", "Humanoid", "Bug Wings", "Armor"
    };

    public static List<PokedexEntry> Run(Pokedex dex, PokedexMode mode, PokedexQuery query)
    {
        bool bySize = query.Order is PokedexOrder.Heaviest or PokedexOrder.Lightest or PokedexOrder.Tallest or PokedexOrder.Smallest;
        var found = Pokedex.Entries(mode).Where(e => dex.IsSeen(e.Species.DexNumber) && (!bySize || dex.IsCaught(e.Species.DexNumber)));
        if (query.NameGroup >= 0) found = found.Where(e => NameGroups[query.NameGroup].Contains(FirstLetter(e.Species.Name)));
        if (query.Type1 is { } t1) found = found.Where(e => Has(e.Species, t1));
        if (query.Type2 is { } t2) found = found.Where(e => Has(e.Species, t2));
        if (query.Shape != null) found = found.Where(e => e.Species.Shape == query.Shape);
        return (query.Order switch
        {
            PokedexOrder.Alphabet => found.OrderBy(e => e.Species.Name, StringComparer.Ordinal),
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

    /// <summary>What an order is called on the screen.</summary>
    public static string NameOf(PokedexOrder order) => order switch
    {
        PokedexOrder.Number => "Number",
        PokedexOrder.Alphabet => "A to Z",
        _ => order.ToString()
    };

    /// <summary>Every name group's label: "A B C".</summary>
    public static string NameOfGroup(int group) => group < 0 ? "Any" : string.Join(' ', NameGroups[group].ToCharArray());

    /// <summary>The weight a list sorted by size shows beside each species, or null when the order isn't by size.</summary>
    public static string? SizeLabel(PokedexOrder order, PokemonSpecies s) => order switch
    {
        PokedexOrder.Heaviest or PokedexOrder.Lightest => s.Weight.ToString("0.0", CultureInfo.InvariantCulture) + " kg",
        PokedexOrder.Tallest or PokedexOrder.Smallest => s.Height.ToString("0.0", CultureInfo.InvariantCulture) + " m",
        _ => null
    };
}
