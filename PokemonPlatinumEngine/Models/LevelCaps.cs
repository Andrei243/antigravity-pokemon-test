using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The level cap by Badge (plan 13 · V3; <c>docs/mechanics/rulings.md</c>, "Ways to play"): with <see cref="RunRules.LevelCap"/>
/// on, EXP stops at the level of the next Gym Leader's best Pokémon until the player holds that Leader's Badge, and at
/// the Elite Four and the Champion's (62) once all eight are held, as the community's level-cap runs do. GPU-free, and
/// the numbers are plan 02's pacing targets, each Leader's highest level in Platinum's data (<c>LevelCapTests</c> holds
/// the table to <c>trainers.json</c>). A region with no table is uncapped until its own plan writes one, and a region
/// whose story is finished (its Hall of Fame) is uncapped too.
/// </summary>
public static class LevelCaps
{
    /// <summary>
    /// A region's caps by the number of Badges held: index 0 is the cap with none (Roark's), the last the cap with every
    /// Badge (Cynthia's). Sinnoh's is plan 02's: Roark 14, Gardenia 22, Fantina 26, Maylene 32, Crasher Wake 37, Byron 41,
    /// Candice 44, Volkner 50, then the League's 62.
    /// </summary>
    public static IReadOnlyList<int>? TableOf(string regionId) => regionId switch
    {
        RegionDatabase.Sinnoh => Sinnoh,
        _ => null
    };

    private static readonly int[] Sinnoh = { 14, 22, 26, 32, 37, 41, 44, 50, 62 };

    /// <summary>
    /// The level no Pokémon of the run grows past now, or null for none: the run isn't capped, the region has no table,
    /// or its story is finished. The Badges are the region's own (<see cref="StoryState.BadgeCount"/>).
    /// </summary>
    public static int? For(RunRules? run, string regionId, StoryState story)
    {
        if (run is not { LevelCap: true }) return null;
        if (TableOf(regionId) is not { } table) return null;
        if (story.IsRegionComplete(regionId)) return null;
        return table[Math.Clamp(story.BadgeCount(regionId), 0, table.Count - 1)];
    }

    /// <summary>
    /// The EXP total a Pokémon of this growth rate stops at under <paramref name="cap"/>: the very start of the cap's
    /// level, so it reaches the level and none of the next one's way.
    /// </summary>
    public static int ExpAt(int cap, GrowthRate growth) => Pokemon.GetExpForLevel(cap, growth);

    /// <summary>
    /// The EXP a Pokémon may still be given under <paramref name="cap"/> (null for none) out of <paramref name="wanted"/>:
    /// all of it without a cap, none of it when it has reached the cap's threshold already.
    /// </summary>
    public static int Allowed(Pokemon p, int wanted, int? cap) =>
        cap is not { } c ? wanted : Math.Clamp(ExpAt(c, p.Species.GrowthRate) - p.CurrentExp, 0, Math.Max(wanted, 0));

    /// <summary>Whether the cap stops this Pokémon growing at all now (it is on the cap's level or past it).</summary>
    public static bool Holds(Pokemon p, int? cap) => cap is { } c && p.Level >= c;
}
