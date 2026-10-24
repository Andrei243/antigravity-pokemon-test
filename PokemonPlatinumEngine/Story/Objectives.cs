using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// One thing the story asks of the player next (plan 12 · Q14): a name (the key of its line in <c>ui.json</c>, under
/// <c>objectives</c>), the region whose story it belongs to, a number that is what <c>VAR_OBJECTIVE</c> holds when a
/// script sets it, and, for a region whose chapters leave flags to follow, what says it is done.
/// </summary>
/// <param name="Done">
/// Whether the story has got past it; null for an objective a script sets by <c>objective</c> and nothing else.
/// Only ever a flag, a variable or a badge, so it never goes back to false once the story has moved on.
/// </param>
public sealed record Objective(int Id, string Name, string Region, Func<StoryState, bool>? Done)
{
    /// <summary>The key of its line in the table of words.</summary>
    public string Key => $"objectives.{Name}";
}

/// <summary>
/// "What to do next" (plan 12 · Q14; GPU-free). Each region's story is a list of objectives in the order it is played.
/// <para>
/// Sinnoh's are <i>derived</i>: the current one is the first whose <see cref="Objective.Done"/> the story has not
/// yet satisfied, worked out from the flags, variables and badges the chapters already set (S4 to S14 and the time
/// after the Hall of Fame), so the save gains no field and an older save needs no migration, as for the requests
/// of Q21. A region whose chapters set no such flags writes its objective with the script command
/// <c>objective &lt;name&gt;</c>, which stores its number in <see cref="Variable"/>; that is saved as any variable is.
/// A stored objective further along than the derived one wins, so a script can always move things on.
/// </para>
/// A new chapter adds its objectives here, in order, with their lines in <c>ui.json</c> (<c>objectives.*</c>), and
/// either gives each a <see cref="Objective.Done"/> or ends its scenes with <c>objective</c>.
/// </summary>
public static class Objectives
{
    /// <summary>The story variable a script's <c>objective</c> sets: the number of the objective (0 for none).</summary>
    public const string Variable = "VAR_OBJECTIVE";

    private static Objective S(int id, string name, Func<StoryState, bool>? done) => new(id, name, RegionDatabase.Sinnoh, done);

    private static Func<StoryState, bool> F(string flag) => s => s.Has(flag);

    private static Func<StoryState, bool> Badge(string name) => s => s.HasBadge(RegionDatabase.Sinnoh, name);

    /// <summary>Every objective of every region, in the order each region's story is played. Sinnoh's ids are 1 to 99.</summary>
    public static IReadOnlyList<Objective> All { get; } = new[]
    {
        // Plan 02 · S4: the opening
        S(1, "downstairs", F("FLAG_TALKED_TO_MOM")),
        S(2, "rivalHome", F("FLAG_RIVAL_LEFT_HOME")),
        S(3, "route201", s => s.PlayerStarter != null),
        S(4, "shoes", F(StoryState.RunningShoesFlag)),
        S(5, "lake", s => s.Var("VAR_VISITED_LAKE_VERITY_WITH_RIVAL") >= 1),
        S(6, "lab", F(StoryState.PokedexFlag)),
        S(7, "parcel", F("FLAG_RECEIVED_PARCEL")),
        // S5: Jubilife and Roark
        S(8, "school", F("FLAG_TALKED_TO_TRAINERS_SCHOOL_RIVAL")),
        S(9, "poketch", F("FLAG_RECEIVED_POKETCH")),
        S(10, "roark", Badge("Coal")),
        // S6: the Windworks and Eterna
        S(11, "worksKey", F("FLAG_OBTAINED_FLOAROMA_MEADOW_WORKS_KEY")),
        S(12, "forest", F("FLAG_TRAVELED_WITH_CHERYL")),
        S(13, "gardenia", Badge("Forest")),
        S(14, "galacticBuilding", F("FLAG_TEAM_GALACTIC_LEFT_ETERNA_BUILDING")),
        S(15, "bicycle", F("FLAG_RECEIVED_BICYCLE")),
        // S7 to S8: Hearthome, Veilstone and Pastoria
        S(16, "fantina", Badge("Relic")),
        S(17, "maylene", Badge("Cobble")),
        S(18, "wake", Badge("Fen")),
        // S9 to S10: Celestic Town, Canalave, Iron Island and the lakes
        S(19, "oldCharm", F("FLAG_DELIVERED_OLD_CHARM")),
        S(20, "ironIsland", F("FLAG_RECEIVED_RIOLU_EGG_FROM_RILEY")),
        S(21, "byron", Badge("Mine")),
        S(22, "lakes", F("FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN")),
        // S11: Snowpoint City and Team Galactic's headquarters
        S(23, "candice", Badge("Icicle")),
        S(24, "storageKey", F("FLAG_USED_STORAGE_KEY")),
        S(25, "headquarters", F("FLAG_DEFEATED_GALACTIC_HQ_CONTROL_ROOM_SATURN")),
        // S12: Mt. Coronet, Spear Pillar and the Distortion World
        S(26, "coronet", F("FLAG_RECEIVED_MT_CORONET_2F_BLACK_FLUTE")),
        S(27, "spearPillar", F("FLAG_GALACTIC_LEFT_LAKE_VALOR")),
        // S13: Sunyshore City
        S(28, "volkner", Badge("Beacon")),
        S(29, "beach", F("FLAG_RECEIVED_SUNYSHORE_CITY_HM07")),
        // S14: the Pokémon League
        S(30, "victoryRoad", F("FLAG_FIRST_ARRIVAL_POKEMON_LEAGUE")),
        S(31, "leagueGuard", F("FLAG_POKEMON_LEAGUE_DOOR_GUARD_MOVED_AWAY")),
        S(32, "champion", F(StoryMigration.GameCompletedFlag)),
        // S15: after the Hall of Fame
        S(33, "afterwards", F("FLAG_ARRESTED_CHARON_STARK_MOUNTAIN")),
        // Nothing presses any more
        S(34, "free", null),
    };

    private static readonly Dictionary<string, Objective> ByName = All.ToDictionary(o => o.Name, StringComparer.Ordinal);

    /// <summary>An objective by its name, as <c>objective &lt;name&gt;</c> writes it; null for a name there is none of.</summary>
    public static Objective? Find(string name) => ByName.GetValueOrDefault(name);

    /// <summary>An objective by the number a story variable holds; null for none.</summary>
    public static Objective? ById(int id) => id > 0 ? All.FirstOrDefault(o => o.Id == id) : null;

    /// <summary>
    /// The objective the story is at in a region: the first of the region's derived ones not yet done, or the one a
    /// script stored if that is further along; null in a region with neither.
    /// </summary>
    public static Objective? Current(StoryState story, string region)
    {
        Objective? derived = null;
        foreach (var o in All)
        {
            if (!string.Equals(o.Region, region, StringComparison.OrdinalIgnoreCase)) continue;
            if (o.Done == null || !o.Done(story)) { derived = o; break; }
        }
        var stored = ById(story.Var(Variable));
        if (stored != null && !string.Equals(stored.Region, region, StringComparison.OrdinalIgnoreCase)) stored = null;
        if (derived == null) return stored;
        return stored != null && stored.Id > derived.Id ? stored : derived;
    }

    /// <summary>
    /// The line that says what to do next, with the player's and the rival's names in, in the language chosen; empty
    /// where the region has none yet.
    /// </summary>
    public static string Line(StoryState story, string region) =>
        Current(story, region) is { } o ? PlayerIdentity.Fill(Text.Of(o.Key)) : "";

    /// <summary>The same for a save, in the region it was made in.</summary>
    public static string Line(SaveData save)
    {
        var story = new StoryState();
        story.Restore(save.ToStory());
        return Line(story, save.RegionId);
    }
}
