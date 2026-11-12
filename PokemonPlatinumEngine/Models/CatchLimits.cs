using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The highest level a wild Pokémon may be caught at, by the badges held (plan 33 · GL2; <c>docs/mechanics/rulings.md</c>,
/// "The Wild Area"): Sword and Shield's rule for the strong Pokémon of the Wild Area, which a ball can't take until the
/// gym challenge has gone far enough, as <see cref="Obedience"/>'s levels say who obeys. Galar's row alone; every other
/// region catches anything, as its own games do. Nought is no limit. GPU-free.
/// </summary>
public static class CatchLimits
{
    /// <summary>Galar's row: the ceiling with none to seven of its badges, and none with all eight. Our own numbers, in steps of five from twenty-five.</summary>
    public static readonly IReadOnlyList<int> Galar = new[] { 25, 30, 35, 40, 45, 50, 55, 60 };

    /// <summary>The ceiling in a region with so many of its badges; nought where there is none.</summary>
    public static int CeilingFor(string? region, int badges) =>
        string.Equals(region, Data.RegionDatabase.Galar, StringComparison.OrdinalIgnoreCase) && badges >= 0 && badges < Galar.Count ? Galar[badges] : 0;
}
