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

    // ------------------------------------------------------------------ plan 14 · B12: the other events

    /// <summary>Events whose entrants (always-in and pool) join this one's pool, so a world event names no trainer twice.</summary>
    public List<string> From { get; set; } = new();

    /// <summary>
    /// Who may be seated (<see cref="Models.Tournament.OfKind"/>): <c>leaders</c> (a class that begins "Leader"),
    /// <c>champions</c> (a Champion or one of the Elite Four), <c>league</c> (any of them), or null for anyone the event names.
    /// </summary>
    public string? Kind { get; set; }

    /// <summary>A type bracket's types: every entrant brings three of them, the player's three each of one of them; empty for any.</summary>
    public List<string> Types { get; set; } = new();

    /// <summary>The battle's format: <c>single</c> (the default), <c>double</c>, <c>triple</c> or <c>rotation</c>.</summary>
    public string? Format { get; set; }

    /// <summary>The rental event: everyone, the player too, fights with three drawn from the Battle Factory's rentals.</summary>
    public bool Rental { get; set; }

    /// <summary>The mixed event: before each match the two in it hand each other one of their three.</summary>
    public bool Mix { get; set; }
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

    /// <summary>The section of the events of no one region (the World Leaders, the type brackets): read whatever data there is.</summary>
    public const string World = "World";

    /// <summary>Every event the file writes, region by region in the file's order, whether its trainers' tables are there or not.</summary>
    public static IReadOnlyList<TournamentEventRecord> All => Data.Regions.SelectMany(r => r.Events).ToList();

    private static readonly Lazy<IReadOnlyList<TournamentEventRecord>> Offered = new(() =>
        Models.Tournament.Offered(Data, id => TrainerDatabase.Get(id) != null, HasData));

    /// <summary>
    /// The events offered (plan 14 · B12): a region's section only once its trainers' table exists (Sinnoh's is the
    /// game's own; the others are <c>Data/world/&lt;region&gt;/trainers.json</c>), and an event only when it can
    /// seat seven of its kind among the trainers there are.
    /// </summary>
    public static IReadOnlyList<TournamentEventRecord> Events => Offered.Value;

    /// <summary>Whether a section's region has trainers of its own to seat: Sinnoh and the world's always.</summary>
    public static bool HasData(string region) =>
        region.Equals("Sinnoh", StringComparison.OrdinalIgnoreCase) || region.Equals(World, StringComparison.OrdinalIgnoreCase)
        || TrainerDatabase.OfRegion(region.ToLowerInvariant()).Count > 0;

    /// <summary>The event's entrants, its always-in and its pool (with those of the events it draws <see cref="TournamentEventRecord.From"/>), none twice.</summary>
    public static (List<TournamentEntrantRecord> Always, List<TournamentEntrantRecord> Pool) EntrantsOf(TournamentEventRecord ev, TournamentData? data = null)
    {
        var events = (data ?? Data).Regions.SelectMany(r => r.Events).ToList();
        var always = ev.Always.ToList();
        var pool = new List<TournamentEntrantRecord>();
        foreach (var entrant in ev.Pool.Concat(ev.From.SelectMany(id => events.Where(e => e.Id == id).SelectMany(e => e.Always.Concat(e.Pool)))))
            if (always.Concat(pool).All(a => !a.Trainer.Equals(entrant.Trainer, StringComparison.OrdinalIgnoreCase))) pool.Add(entrant);
        return (always, pool);
    }

    public static TournamentEventRecord? Get(string? id) =>
        id == null ? null : All.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
}
