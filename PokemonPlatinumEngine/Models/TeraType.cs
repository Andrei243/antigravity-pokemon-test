using System;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A Tera Type (plan 06 · R23): one of the eighteen types, or Stellar, which is no type of the chart and so is kept
/// beside it rather than in <see cref="PokemonType"/> (whose order the type chart and the data go by). A Stellar
/// Pokémon keeps its own types for defence; <see cref="Type"/> means nothing for it.
/// </summary>
public readonly record struct TeraType(PokemonType Type, bool IsStellar)
{
    /// <summary>The Stellar Tera Type (Terapagos's, and what a Stellar Tera Shard gives in Paldea).</summary>
    public static TeraType Stellar => new(PokemonType.Normal, true);

    /// <summary>A Tera Type of one of the eighteen types.</summary>
    public static TeraType Of(PokemonType type) => new(type, false);

    /// <summary>Its name as the data and a save write it: a type's name, or "Stellar".</summary>
    public string Name => IsStellar ? "Stellar" : Type.ToString();

    public override string ToString() => Name;

    /// <summary>A Tera Type by its name ("Fire", "Stellar"); null for no such name.</summary>
    public static TeraType? Parse(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (name.Equals("Stellar", StringComparison.OrdinalIgnoreCase)) return Stellar;
        return Enum.TryParse(name, true, out PokemonType type) ? Of(type) : null;
    }

    /// <summary>
    /// The Tera Type a species won't have changed (Showdown's <c>forceTeraType</c>): an Ogerpon's is its mask's
    /// (Grass with the Teal Mask, Water with the Wellspring, Fire with the Hearthflame, Rock with the Cornerstone)
    /// and a Terapagos's Stellar. Null for every other species.
    /// </summary>
    public static TeraType? ForcedFor(Pokemon p) => p.Species.Name switch
    {
        "Ogerpon" => Of(p.Form switch
        {
            "Ogerpon-Wellspring-Mask" => PokemonType.Water,
            "Ogerpon-Hearthflame-Mask" => PokemonType.Fire,
            "Ogerpon-Cornerstone-Mask" => PokemonType.Rock,
            _ => PokemonType.Grass
        }),
        "Terapagos" => Stellar,
        _ => null
    };
}
