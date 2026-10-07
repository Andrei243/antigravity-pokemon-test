using System;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A place's name as English says it in a sentence: "on Route 201", "at Lake Verity", "in the Great Marsh",
/// "in Jubilife City". The Journal and the PC's "met" line put a place's name after a word of their own, and "in"
/// before every one read "Caught a Starly in Route 203".
/// </summary>
public static class PlaceWords
{
    /// <summary>The places whose names are said with "the" before them.</summary>
    private static readonly string[] WithThe =
    {
        "Great Marsh", "Distortion World", "Valley Windworks", "Fuego Ironworks", "Pokémon League", "Solaceon Ruins",
        "Old Chateau", "Lost Tower", "Battle Zone", "Fight Area", "Survival Area", "Resort Area", "Battle Frontier"
    };

    /// <summary>The name as it stands in a sentence: "the Great Marsh", "Route 201".</summary>
    public static string Named(string place) => WithThe.Contains(place) ? "the " + place : place;

    /// <summary>Where something happened: "on Route 201", "at Lake Verity", "in the Great Marsh".</summary>
    public static string In(string place) => Preposition(place) + " " + Named(place);

    private static string Preposition(string place)
    {
        // A road, a path, a mountain or an island is walked on
        if (place.StartsWith("Route ", StringComparison.Ordinal) || place.StartsWith("Mt. ", StringComparison.Ordinal)
            || place.EndsWith(" Road", StringComparison.Ordinal) || place.EndsWith(" Path", StringComparison.Ordinal)
            || place.EndsWith(" Island", StringComparison.Ordinal))
            return "on";
        // A lake, a spring, a square, a garden or a works is a spot one is at
        if (place.StartsWith("Lake ", StringComparison.Ordinal) || place.EndsWith(" Lakefront", StringComparison.Ordinal)
            || place.EndsWith(" Pillar", StringComparison.Ordinal) || place.EndsWith(" Spring", StringComparison.Ordinal)
            || place.EndsWith(" Square", StringComparison.Ordinal) || place.EndsWith(" Garden", StringComparison.Ordinal)
            || place.EndsWith("works", StringComparison.Ordinal) || place == "Pokémon League")
            return "at";
        return "in";
    }
}
