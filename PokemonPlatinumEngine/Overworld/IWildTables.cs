using System.Collections.Generic;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Wild tables laid over a map's own (plan 13 · V15: Remix's, <see cref="Map.Tables"/>), asked by area key before the
/// area's own tables. Each answer is the whole table met there, slots and levels, with the area's own rate; null where
/// the area keeps its own, which is every area the tables don't name. A table of the area's own that the tables leave
/// out is empty, so nothing is met there under them.
/// </summary>
public interface IWildTables
{
    /// <summary>The grass's (or a cave's floor's) table of an area; null where the area keeps its own.</summary>
    IReadOnlyList<WildEncounterEntry>? Land(string area);

    /// <summary>The water's table of an area, met surfing; null where the area keeps its own.</summary>
    IReadOnlyList<WildEncounterEntry>? Water(string area);

    /// <summary>A rod's table of an area; null where the area keeps its own.</summary>
    IReadOnlyList<WildEncounterEntry>? Rod(string area, FishingRod rod);
}
