using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>What a hollow's floors are made of (plan 44 · SD7): its walls' look, and whether its pools may be crossed.</summary>
public enum DelveTheme
{
    /// <summary>Rock round open floor.</summary>
    Cave,
    /// <summary>Trees round open floor.</summary>
    Wood,
    /// <summary>Rock round open floor, with pools in some squares that only a Water type crosses.</summary>
    SeaCave,
    /// <summary>Built walls round a tower's floors.</summary>
    Tower
}

/// <summary>
/// A hollow (plan 44 · SD7; <c>docs/mechanics/rulings.md</c>, "The delve"): its floors, its level, who lives on its floors
/// and what lies about on them. SD9 writes the four of <c>Data/hollows.json</c>; SD7 has only <see cref="Test"/>, written
/// here. No drawing or input.
/// </summary>
/// <param name="Key">The hollow's own name.</param>
/// <param name="Theme">What its floors look like and whether they have pools.</param>
/// <param name="Floors">How many floors it has; the stairs of the last end the run.</param>
/// <param name="Level">The level its residents are met at and the team goes down at.</param>
/// <param name="Residents">The species its residents are drawn from, each equally.</param>
/// <param name="ResidentsPerFloor">How many residents a floor has.</param>
/// <param name="Finds">The items a floor's finds are drawn from, each equally.</param>
/// <param name="FindsPerFloor">How many finds a floor has.</param>
/// <param name="SnaresPerFloor">How many snares a floor has.</param>
public sealed record DelveHollow(string Key, DelveTheme Theme, int Floors, int Level, IReadOnlyList<string> Residents,
    int ResidentsPerFloor, IReadOnlyList<string> Finds, int FindsPerFloor, int SnaresPerFloor)
{
    /// <summary>The test hollow: a sea cave of three floors at level 12, its residents of the shore and the rock.</summary>
    public static DelveHollow Test { get; } = new(
        "test_hollow", DelveTheme.SeaCave, 3, 12,
        new[] { "Zubat", "Geodude", "Bidoof", "Starly", "Wingull", "Krabby" }, 3,
        new[] { "Oran Berry", "Oran Berry", "Pecha Berry", "Cheri Berry", "Potion", "Escape Rope", "Revive" }, 3,
        2);
}
