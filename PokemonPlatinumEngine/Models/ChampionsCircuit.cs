using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Laurel Circuit as one game keeps it (plan 41 · EG4; saved as <c>SaveData.Circuit</c>, null until first begun): the
/// circuits won, whether one is under way, the regions it has stamped, and the teams that won its finals, kept for the
/// Laurel Gallery's circuit plinth. Plain data, which the save writes as it stands.
/// </summary>
public sealed class CircuitState
{
    /// <summary>How many circuits have been won: each raises the next one's level by <see cref="ChampionsCircuit.StepPerCircuit"/>.</summary>
    public int Won { get; set; }

    /// <summary>Whether a circuit is under way, from the desk's word until its final is won.</summary>
    public bool Running { get; set; }

    /// <summary>The regions the circuit under way has stamped, by id, in the order their Halls of Fame were entered.</summary>
    public List<string> Stamps { get; set; } = new();

    /// <summary>The teams that won a final, oldest first, each with the region "Circuit".</summary>
    public List<Core.SavedHallOfFameEntry> Finals { get; set; } = new();

    /// <summary>Whether there is nothing to keep: no circuit ever begun.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmpty => Won == 0 && !Running && Stamps.Count == 0 && Finals.Count == 0;
}

/// <summary>A row of <c>Data/circuit.json</c>: a region's Champion in the final, the team they bring, the look they stand in and who stands for them in the court.</summary>
public sealed class CircuitRow
{
    [DataRequired] public string Region { get; set; } = "";
    [DataRequired] public string Team { get; set; } = "";
    [DataRequired] public string Look { get; set; } = "";
    /// <summary>The person of the arena who stands for them (their id in <c>Hub_Arena</c>).</summary>
    [DataRequired] public string Person { get; set; } = "";
}

/// <summary>The file <c>Data/circuit.json</c>: the final's Champions in the chain's order.</summary>
public sealed class CircuitFile
{
    [DataRequired] public List<CircuitRow> Rows { get; set; } = new();
}

/// <summary>
/// The Champions' circuit (plan 41 · EG4; <c>docs/mechanics/rulings.md</c>, "The Laurel Circuit"): every finished
/// region's League fought again in its own rooms at one level, stamped by its Hall of Fame, and a final of the finished
/// regions' Champions in the hub's Laurel Court. GPU-free.
/// </summary>
public static class ChampionsCircuit
{
    public const string FileName = "circuit.json";

    /// <summary>The levels each circuit won adds to the next (decision 4).</summary>
    public const int StepPerCircuit = 5;

    /// <summary>The level where the furthest region finished has no post-game pair of its own (the region a game began in, Sinnoh's levels being Platinum's).</summary>
    public const int BaseLevel = 80;

    /// <summary>How many regions must be finished before a circuit may be begun.</summary>
    public const int RegionsNeeded = 2;

    /// <summary>The region a final's teams are kept under.</summary>
    public const string CircuitRegion = "Circuit";

    private static readonly Lazy<CircuitFile> file = new(() => GameDataFiles.Load<CircuitFile>(FileName, strict: true));

    /// <summary>The final's rows, as the file lists them (the chain's order).</summary>
    public static IReadOnlyList<CircuitRow> Rows => file.Value.Rows;

    /// <summary>The regions of the chain whose Hall of Fame is entered, in the chain's order (no side region).</summary>
    public static IReadOnlyList<Region> Finished(StoryState story) =>
        RegionDatabase.All.Where(r => !r.Side && story.Has(r.StoryCompleteFlag)).ToList();

    /// <summary>
    /// The one level every circuit team is met at (decision 4): the furthest finished region's post-game level (its
    /// <c>levels</c>' last pair), five more for each circuit won, never past the game's cap.
    /// </summary>
    public static int Level(CircuitState state, StoryState story, Ruleset? rules = null)
    {
        int top = RematchLevels.Furthest(story)?.Levels is { Points.Count: > 0 } levels ? levels.Points[^1].To : BaseLevel;
        return Math.Min((rules ?? Ruleset.Current).LevelCap, Math.Max(1, top + StepPerCircuit * state.Won));
    }

    /// <summary>Whether a circuit may be begun: none under way, and two regions of the chain finished.</summary>
    public static bool CanBegin(CircuitState state, StoryState story) => !state.Running && Finished(story).Count >= RegionsNeeded;

    /// <summary>Begins a circuit at the desk; false where it may not be begun.</summary>
    public static bool Begin(CircuitState state, StoryState story)
    {
        if (!CanBegin(state, story)) return false;
        state.Running = true;
        state.Stamps.Clear();
        return true;
    }

    /// <summary>
    /// A region's Hall of Fame entered (both hosts' <c>EnterHallOfFame</c>): stamped while a circuit runs and the region is
    /// one of the chain's and finished, once. Whether it was stamped now.
    /// </summary>
    public static bool Stamp(CircuitState state, string? region, StoryState story)
    {
        if (!state.Running || region == null || RegionDatabase.Get(region) is not { Side: false } r || !story.Has(r.StoryCompleteFlag)) return false;
        if (state.Stamps.Contains(r.Id, StringComparer.OrdinalIgnoreCase)) return false;
        state.Stamps.Add(r.Id);
        return true;
    }

    /// <summary>Whether every finished region is stamped, which opens the final.</summary>
    public static bool Ready(CircuitState state, StoryState story) =>
        state.Running && Finished(story).All(r => state.Stamps.Contains(r.Id, StringComparer.OrdinalIgnoreCase));

    /// <summary>The final's Champions: the rows of the finished regions in the chain's order.</summary>
    public static IReadOnlyList<CircuitRow> FinalOrder(StoryState story)
    {
        var finished = Finished(story).Select(r => r.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Rows.Where(r => finished.Contains(r.Region)).ToList();
    }

    /// <summary>
    /// The final won: the circuit is over and counted, its team kept for the circuit's plinth. Gives back the circuits won.
    /// </summary>
    public static int WinFinal(CircuitState state, Party party, Func<Pokemon, (string Name, int Id)> trainerOf, DateTime now)
    {
        if (!state.Running) return state.Won;
        state.Running = false;
        state.Stamps.Clear();
        state.Won++;
        var entry = new HallOfFameEntry(now.Date, HallOfFame.TeamOf(party, trainerOf), Region: CircuitRegion, Number: state.Won);
        state.Finals.Add(SavedHallOfFameEntry.From(entry));
        return state.Won;
    }

    /// <summary>What a team of the gathering's id ends in (plan 41 · EG9: <c>cyrus_gathering</c>), each lifted to the circuit's level.</summary>
    public const string GatheringSuffix = "_gathering";

    /// <summary>Whether a team is one of Grayfall Keep's (plan 41 · EG9), met at the circuit's level whether a circuit runs or not.</summary>
    public static bool IsGatheringTeam(string teamId) => teamId.EndsWith(GatheringSuffix, StringComparison.Ordinal);

    /// <summary>Whether a team is a League's: one of the Elite Four or a Champion, by its class.</summary>
    public static bool IsLeagueTeam(string teamId) =>
        TrainerDatabase.Get(teamId) is { } record
        && (record.Class is "Elite Four" or "Champion" || record.Class.StartsWith("Elite Four ", StringComparison.Ordinal));

    /// <summary>
    /// Whether a circuit under way lifts a team: a League's team of a finished region of the chain (its region read from
    /// its id, <see cref="RematchLevels.RegionOfTeam"/>).
    /// </summary>
    public static bool Lifts(CircuitState state, string teamId, StoryState story) =>
        state.Running && IsLeagueTeam(teamId) && RematchLevels.RegionOfTeam(teamId) is { Side: false } region && story.Has(region.StoryCompleteFlag);

    /// <summary>
    /// A team lifted to a level on a copy (the trainer of the map keeps theirs): its highest Pokémon at <paramref name="level"/>,
    /// the others the same steps below it as they stood (the spread kept), each taking the stages its new level allows.
    /// </summary>
    public static Trainer Lift(Trainer trainer, int level)
    {
        int top = trainer.Party.Members.Count == 0 ? level : trainer.Party.Members.Max(p => p.Level);
        int by = level - top;
        return trainer.Relevelled(l => Math.Clamp(l + by, 1, Ruleset.HighestLevel), grow: true);
    }
}
