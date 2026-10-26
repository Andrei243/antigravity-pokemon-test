using System;
using System.Collections.Generic;
using System.Linq;
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
    /// A region's caps by the number of Badges held, its file's <c>caps</c> (plan 18 · W4b-1): index 0 is the cap with
    /// none (Roark's), the last the cap with every Badge (Cynthia's). Sinnoh's is plan 02's: Roark 14, Gardenia 22,
    /// Fantina 26, Maylene 32, Crasher Wake 37, Byron 41, Candice 44, Volkner 50, then the League's 62. Each later
    /// region's is its own game's levels; <paramref name="crossed"/> (a region the game didn't begin in) gives them as
    /// they are met there, through the region's <c>levels</c>. Null for a region with no table.
    /// </summary>
    public static IReadOnlyList<int>? TableOf(string regionId, bool crossed = false)
    {
        var region = RegionDatabase.Get(regionId);
        if (region?.File?.Caps is not { Count: > 0 } own) return null;
        if (!crossed || region.Levels is not { } levels) return own;
        return own.Select(levels.Map).ToArray();
    }

    /// <summary>
    /// The level no Pokémon of the run grows past now, or null for none: the run isn't capped, the region has no table,
    /// or its story is finished. The Badges are the region's own (<see cref="StoryState.BadgeCount"/>); in a region the
    /// game crossed into (<paramref name="crossed"/>) the table is the mapped one.
    /// </summary>
    public static int? For(RunRules? run, string regionId, StoryState story, bool crossed = false)
    {
        if (run is not { LevelCap: true }) return null;
        if (TableOf(regionId, crossed) is not { } table) return null;
        if (story.IsRegionComplete(regionId)) return null;
        return table[Math.Clamp(story.BadgeCount(regionId), 0, table.Count - 1)];
    }

    /// <summary>The share over a crossed region's next milestone the ceiling stands at (plan 18 · W4b-3): 110%.</summary>
    public const int CeilingPercent = 110;

    /// <summary>What a crossed region's foes give, in halves (plan 18 · W4b-3): three halves, as a Lucky Egg does.</summary>
    public const int CrossedExpHalves = 3;

    /// <summary>
    /// The milestone a crossed region's ceiling is measured from (plan 18 · W4b-3): the mapped level of the next Leader
    /// whose Badge isn't held, the Champion's with all eight, and once the region's Hall of Fame is entered its post-game
    /// top (the last pair of its <c>levels</c>, as met). Null for a region with no table or no mapping (Sinnoh).
    /// </summary>
    public static int? MilestoneOf(string regionId, StoryState story)
    {
        if (RegionDatabase.Get(regionId) is not { Levels: { } levels } || TableOf(regionId, crossed: true) is not { } table) return null;
        if (story.IsRegionComplete(regionId)) return levels.Points[^1].To;
        return table[Math.Clamp(story.BadgeCount(regionId), 0, table.Count - 1)];
    }

    /// <summary>
    /// A crossed region's soft ceiling (plan 18 · W4b-3, Design C; <c>docs/mechanics/rulings.md</c>, "A crossed region's
    /// EXP"): no Pokémon gains EXP past 110% of the next milestone's level (<see cref="MilestoneOf"/>, rounded down), held
    /// under <see cref="Ruleset.HighestLevel"/>. It never lowers a level: a Pokémon over it keeps its level and fights at
    /// it, and only stops growing until the region catches up. In every difficulty; null where there is no milestone.
    /// </summary>
    public static int? Ceiling(string regionId, StoryState story) =>
        MilestoneOf(regionId, story) is { } m ? Math.Min(Ruleset.HighestLevel, m * CeilingPercent / 100) : null;

    /// <summary>The lower of two caps, either of which may be none: a run's own cap wins where it is lower (W4b-3's L5).</summary>
    public static int? Lower(int? a, int? b) => a is { } x ? (b is { } y ? Math.Min(x, y) : x) : b;

    /// <summary>A crossed region's EXP (plan 18 · W4b-3): half as much again, after the formula, rounded down.</summary>
    public static int CrossedExp(int exp) => exp * CrossedExpHalves / 2;

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
