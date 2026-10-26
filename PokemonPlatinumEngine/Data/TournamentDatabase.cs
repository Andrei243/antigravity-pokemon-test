using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>One who may be drawn into a World Tournament's event: a trainer of the trainer table and the look they stand in.</summary>
public sealed class TournamentEntrantRecord
{
    /// <summary>The trainer's id in <c>trainers.json</c> (or the other tables <see cref="TrainerDatabase.Get(string?)"/> finds): their team is that one's, never written twice.</summary>
    public string Trainer { get; set; } = "";

    /// <summary>The look of <c>characters.json</c> they stand on the stage and in battle as.</summary>
    public string Look { get; set; } = "";
}

/// <summary>
/// An event of the World Tournament (plan 14 · B11): who is always in its bracket of eight, who the rest are drawn from,
/// the event that must be won before it opens, and the Battle Points its title earns. Its name on screen is
/// <c>tournament.event.&lt;id&gt;</c> of the text table.
/// </summary>
public sealed class TournamentEventRecord
{
    public string Id { get; set; } = "";
    public int Points { get; set; }

    /// <summary>The event whose title opens this one; null for one open from the start.</summary>
    public string? After { get; set; }

    /// <summary>Seated in every bracket of the event.</summary>
    public List<TournamentEntrantRecord> Always { get; set; } = new();

    /// <summary>The rest of the seven are drawn from these, none twice and none of <see cref="Always"/>.</summary>
    public List<TournamentEntrantRecord> Pool { get; set; } = new();
}

/// <summary>A region's events, a section of the file (plan 14 · B12 adds the other regions' as their tables land).</summary>
public sealed class TournamentRegionRecord
{
    public string Region { get; set; } = "";
    public List<TournamentEventRecord> Events { get; set; } = new();
}

public sealed class TournamentData
{
    public List<TournamentRegionRecord> Regions { get; set; } = new();
}

/// <summary>
/// The World Tournament's events (plan 14 · B11; <c>Data/tournaments.json</c>, written by hand): Sinnoh's Leaders and the
/// Champion's event, whose entrants are trainers of the trainer table by id, so no team is written twice.
/// </summary>
public static class TournamentDatabase
{
    public const string FileName = "tournaments.json";

    private static readonly TournamentData Data = GameDataFiles.Load<TournamentData>(FileName);

    public static IReadOnlyList<TournamentRegionRecord> Regions => Data.Regions;

    /// <summary>Every event, region by region in the file's order.</summary>
    public static IReadOnlyList<TournamentEventRecord> Events => Data.Regions.SelectMany(r => r.Events).ToList();

    public static TournamentEventRecord? Get(string? id) =>
        id == null ? null : Events.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
}
