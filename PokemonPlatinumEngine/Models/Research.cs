using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What a research task counts (plan 15 · E1): a species met, caught, beaten, a move of its own used, a level it gained in battle, an evolution out of it, one of it made a friend.</summary>
public enum ResearchKind { Met, Caught, Defeated, MoveUsed, Leveled, Evolved, Befriended }

/// <summary>
/// One task of a species' research (plan 15 · E1): what it counts, its key in the record (<c>met</c>, <c>move:Tackle</c>),
/// the move for a <see cref="ResearchKind.MoveUsed"/>, and its ladder of targets, a point a rung reached.
/// </summary>
public sealed record ResearchTask(ResearchKind Kind, string Key, string? Move, IReadOnlyList<int> Rungs)
{
    /// <summary>How many rungs a count has reached.</summary>
    public int PointsFor(int count) => Rungs.Count(target => count >= target);

    /// <summary>The next target past a count, or null once the last rung is reached.</summary>
    public int? NextFor(int count) => Rungs.Cast<int?>().FirstOrDefault(target => count < target);
}

/// <summary>
/// The tasks of each species (plan 15 · E1), made from its data and never by hand: met, caught and beaten for every
/// species; two moves of its own learnset used (one where it has one); levels gained in battle; evolutions out of it
/// where it evolves; one made a friend (friendship to <see cref="FriendshipRules.EvolveAt"/>). Every ladder is 1, 2, 5,
/// 10 and 25, but what a game gives a legendary or a mythical only once (meeting, catching, beating, evolving, a friend)
/// has a ladder of 1 alone. Each rung is a point; a species' level is its points over all its rungs, out of 10.
/// </summary>
public static class ResearchTasks
{
    /// <summary>The ladder every task climbs.</summary>
    public static readonly IReadOnlyList<int> Ladder = new[] { 1, 2, 5, 10, 25 };

    /// <summary>The ladder of what a legendary or a mythical gives only once.</summary>
    public static readonly IReadOnlyList<int> Once = new[] { 1 };

    private static readonly Dictionary<string, IReadOnlyList<ResearchTask>> cache = new(StringComparer.Ordinal);
    private static readonly object gate = new();

    /// <summary>A species' tasks, in the order the page lists them.</summary>
    public static IReadOnlyList<ResearchTask> For(PokemonSpecies species)
    {
        // The moves learned follow the rules in force (Ruleset.Use swaps them in), so the cache keeps the first two moves too
        var moves = TwoMoves(species);
        string key = species.Name + "|" + string.Join(",", moves);
        lock (gate)
        {
            if (cache.TryGetValue(key, out var known)) return known;
            var made = Make(species, moves);
            cache[key] = made;
            return made;
        }
    }

    private static IReadOnlyList<ResearchTask> Make(PokemonSpecies species, IReadOnlyList<string> moves)
    {
        bool rare = species.Legendary || species.Mythical;
        var once = rare ? Once : Ladder;
        var tasks = new List<ResearchTask>
        {
            new(ResearchKind.Met, "met", null, once),
            new(ResearchKind.Caught, "caught", null, once),
            new(ResearchKind.Defeated, "defeated", null, once),
        };
        foreach (string move in moves) tasks.Add(new ResearchTask(ResearchKind.MoveUsed, "move:" + move, move, Ladder));
        tasks.Add(new ResearchTask(ResearchKind.Leveled, "leveled", null, Ladder));
        if (species.Evolutions is { Count: > 0 }) tasks.Add(new ResearchTask(ResearchKind.Evolved, "evolved", null, once));
        tasks.Add(new ResearchTask(ResearchKind.Befriended, "befriended", null, once));
        return tasks;
    }

    /// <summary>
    /// The two moves a species' tasks ask for: the first it learns by level, and the one halfway down its list, each a
    /// move that works (<see cref="MoveDatabase.Runs"/>); one where its list has only one.
    /// </summary>
    public static IReadOnlyList<string> TwoMoves(PokemonSpecies species)
    {
        var names = species.Learnset.Where(m => m.Level >= 1).OrderBy(m => m.Level)
            .Select(m => m.MoveName).Where(n => MoveDatabase.Get(n) != null && MoveDatabase.Runs(n)).Distinct().ToList();
        if (names.Count == 0) return Array.Empty<string>();
        string first = names[0], middle = names[names.Count / 2];
        return middle == first ? new[] { first } : new[] { first, middle };
    }
}

/// <summary>One species' counts (plan 15 · E1), by task key, like a Pokémon's <c>EvolutionProgress</c>.</summary>
public sealed class ResearchRecord
{
    public Dictionary<string, int> Counts { get; } = new(StringComparer.Ordinal);

    public int CountOf(string key) => Counts.GetValueOrDefault(key);

    public void Add(string key, int n = 1)
    {
        if (n <= 0) return;
        Counts[key] = CountOf(key) + n;
    }
}

/// <summary>
/// The Pokédex's research (plan 15 · E1; GPU-free): a record of counts for each species, by National number, fed by
/// what the player does (the battle's screen tells it what its lines show, the field what happens outside), and the
/// levels it comes to. A level that rises is noted for the field to tell (<see cref="TakeRisen"/>). Saved whole as
/// <c>SaveData.Research</c> (null while empty, so no save version was raised) with <c>SaveData.ResearchComplete</c>.
/// </summary>
public sealed class Research
{
    /// <summary>The highest level a species' research reaches.</summary>
    public const int MaxLevel = 10;

    /// <summary>What the Pokédex's diploma screen is told to show the research certificate (beside a Pokédex's own identifier).</summary>
    public const string CertificateMode = "research";

    private readonly Dictionary<int, ResearchRecord> records = new();
    private readonly List<int> risen = new();

    /// <summary>Whether the research certificate has been given (the Sinnoh Pokédex's 210 at level 10).</summary>
    public bool Complete { get; set; }

    public bool IsEmpty => records.Count == 0;

    /// <summary>A species' record; an empty one for a species nothing has been counted of.</summary>
    public ResearchRecord RecordOf(int dexNumber) => records.TryGetValue(dexNumber, out var r) ? r : new ResearchRecord();

    /// <summary>How many points a species has, of how many.</summary>
    public (int Points, int Of) Points(PokemonSpecies species)
    {
        var record = RecordOf(species.DexNumber);
        var tasks = ResearchTasks.For(species);
        return (tasks.Sum(t => t.PointsFor(record.CountOf(t.Key))), tasks.Sum(t => t.Rungs.Count));
    }

    /// <summary>A species' research level, 0 to 10: its points over all its rungs, out of ten, rounded down.</summary>
    public int LevelOf(PokemonSpecies species)
    {
        var (points, of) = Points(species);
        return of == 0 ? 0 : points * MaxLevel / of;
    }

    /// <summary>
    /// Counts something done with a species: <paramref name="kind"/>'s task (the move named, for a move used); a kind
    /// or a move the species' tasks don't ask for is not counted. A level that rises is noted.
    /// </summary>
    public void Tell(PokemonSpecies species, ResearchKind kind, string? move = null, int n = 1)
    {
        if (species.IsOurs || n <= 0) return;
        var task = ResearchTasks.For(species).FirstOrDefault(t => t.Kind == kind && (kind != ResearchKind.MoveUsed || t.Move == move));
        if (task == null) return;
        int before = LevelOf(species);
        if (!records.TryGetValue(species.DexNumber, out var record)) records[species.DexNumber] = record = new ResearchRecord();
        record.Add(task.Key, n);
        if (LevelOf(species) > before && !risen.Contains(species.DexNumber)) risen.Add(species.DexNumber);
    }

    /// <summary>The species whose level has risen since this was last asked, in the order they rose.</summary>
    public IReadOnlyList<int> TakeRisen()
    {
        var taken = risen.ToList();
        risen.Clear();
        return taken;
    }

    /// <summary>The species of the Sinnoh Pokédex at level 10, of its 210.</summary>
    public (int Done, int Of) SinnohProgress()
    {
        var species = Pokedex.Entries(Pokedex.Sinnoh).Select(e => e.Species).ToList();
        return (species.Count(s => LevelOf(s) >= MaxLevel), species.Count);
    }

    /// <summary>Whether the certificate is due now and not yet given; gives it (once, ever) when it is.</summary>
    public bool AwardCertificate()
    {
        if (Complete) return false;
        var (done, of) = SinnohProgress();
        if (of == 0 || done < of) return false;
        Complete = true;
        return true;
    }

    public void Clear()
    {
        records.Clear();
        risen.Clear();
        Complete = false;
    }

    /// <summary>What a save keeps: every species' counts by number; null with nothing counted.</summary>
    public Dictionary<int, Dictionary<string, int>>? ToSaved() => records.Count == 0 ? null
        : records.Where(kv => kv.Value.Counts.Count > 0).OrderBy(kv => kv.Key)
            .ToDictionary(kv => kv.Key, kv => kv.Value.Counts.OrderBy(c => c.Key, StringComparer.Ordinal).ToDictionary(c => c.Key, c => c.Value, StringComparer.Ordinal));

    /// <summary>Restores what a save kept (null reads as nothing counted).</summary>
    public void Restore(Dictionary<int, Dictionary<string, int>>? saved, bool complete)
    {
        Clear();
        Complete = complete;
        if (saved == null) return;
        foreach (var (dex, counts) in saved)
        {
            var record = new ResearchRecord();
            foreach (var (key, n) in counts) record.Add(key, n);
            records[dex] = record;
        }
    }
}
