using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Pokédex entry composed at run time (plan 17 · N3): what a species is, its size and how it evolves, from the
/// templates of <c>dexText</c> in the chosen language's table and the species' names in that language. It replaces the
/// sentence the importer used to write into <c>species.json</c> (plan 03 · decision 1: nothing of the games' own text
/// is read, the entry is ours); in English it is that sentence to the character, which a test holds for all 1025.
/// What English alone does (its "a" and "an" before a word, the "X, Y or Z" of a list) is a rule of
/// <see cref="Core.Language"/> and a template, not a line here. GPU-free.
/// </summary>
public static class DexText
{
    /// <summary>The entry of <paramref name="species"/>: the first of <c>dexText.entry</c>'s sentences, then how it evolves.</summary>
    public static string Compose(PokemonSpecies species) => Compose(species, PokemonDatabase.Everyone());

    /// <summary>
    /// The entry of <paramref name="species"/>, given every species there is (to find the one it evolves from). A species
    /// of our own (generation 0, plan 03 · D14) is told of as first found where the species it evolves from was.
    /// </summary>
    public static string Compose(PokemonSpecies species, IEnumerable<PokemonSpecies> all)
    {
        var from = all.FirstOrDefault(o => o.Evolutions?.Any(e => e.TargetSpecies == species.Name) == true);
        int generation = Math.Clamp(species.Generation == 0 && from != null ? FoundIn(species, from) : species.Generation, 1, 9);
        string region = Text.Of($"dexText.region.{generation}");
        string types = species.SecondaryType is { } second
            ? Text.Of("dexText.typeTwo", ("first", Words.Type(species.PrimaryType)), ("second", Words.Type(second)))
            : Text.Of("dexText.typeOne", ("type", Words.Type(species.PrimaryType)));
        string text = Text.Of("dexText.entry",
            ("name", species.DisplayName), ("article", Text.Language.IndefiniteArticle(species.Category)), ("kind", species.DisplayKind),
            ("types", types), ("region", region), ("height", Measure(species.Height)), ("weight", Measure(species.Weight)));

        if (from != null) text += " " + Text.Of("dexText.from", ("species", from.DisplayName));
        if (species.Evolutions is { Count: > 0 } evolutions)
        {
            var into = evolutions.Select(e => e.TargetSpecies).Distinct().Select(n => LocalNames.Species(n)).ToList();
            text += " " + (into.Count == 1
                ? Text.Of("dexText.intoOne", ("species", into[0]))
                : Text.Of("dexText.intoMany", ("list", Text.Of("dexText.listEnd",
                    ("rest", string.Join(Text.Of("dexText.listSeparator"), into.SkipLast(1))), ("last", into[^1])))));
        }
        return text;
    }

    /// <summary>
    /// The generation a species of ours is told of as first found in: its forebear's, or, grown from a regional form
    /// (plan 03 · D14, the Galar supplement), the generation that form came with (Alola's 7, Galar's and Hisui's 8, Paldea's 9).
    /// </summary>
    private static int FoundIn(PokemonSpecies ours, PokemonSpecies from)
    {
        var form = from.Evolutions!.First(e => e.TargetSpecies == ours.Name).FromForm;
        if (form == null) return from.Generation;
        var parts = form.Split('-');
        return parts.Contains("Alola") ? 7 : parts.Contains("Galar") || parts.Contains("Hisui") ? 8 : parts.Contains("Paldea") ? 9 : from.Generation;
    }

    /// <summary>A height or weight to up to two places, at least one, with the language's own decimal mark (<c>0.7</c>, <c>1.25</c>, <c>905.0</c>).</summary>
    private static string Measure(float value) =>
        value.ToString("0.0#", CultureInfo.InvariantCulture).Replace(".", Text.Language.Decimal);
}
