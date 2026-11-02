using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What a Pokémon is sent to do (plan 15 · E3; Sun and Moon's islands and Sword and Shield's jobs, in our own words):
/// an errand about town, an orchard's harvest, a soak in a hot spring. The nursery that hatches Eggs is a kind still to come.
/// </summary>
public enum JobKind { Errand, Orchard, HotSpring }

/// <summary>
/// A Pokémon away on a job (plan 15 · E3): the Pokémon itself, held whole as a save holds it, so while it is away it is
/// in no box and no team and needs no id to be found by; what it does, when it set off and when it is due back (UTC),
/// and the hours asked for.
/// </summary>
public sealed class Job
{
    public SavedPokemonData Pokemon { get; set; } = new();
    public JobKind Kind { get; set; }
    public DateTime Started { get; set; }
    public DateTime Ends { get; set; }
    public int Hours { get; set; }

    /// <summary>The whole hours that have passed since it set off, never more than asked for; none for a clock set back.</summary>
    public int HoursDone(DateTime now) => now < Started ? 0 : (int)Math.Min(Hours, Math.Floor((now - Started).TotalHours));

    /// <summary>Whether the hours asked for have all passed.</summary>
    public bool IsDone(DateTime now) => now >= Ends;

    /// <summary>The time left until it is due back; nothing once it is.</summary>
    public TimeSpan Left(DateTime now) => now >= Ends ? TimeSpan.Zero : now < Started ? Ends - Started : Ends - now;
}

/// <summary>What a job paid as it was collected (plan 15 · E3).</summary>
public sealed class JobResult
{
    public required Pokemon Pokemon { get; init; }
    public required JobKind Kind { get; init; }
    /// <summary>The whole hours paid for.</summary>
    public int Hours { get; init; }
    public int Exp { get; init; }
    public int LevelsGained { get; init; }
    public IReadOnlyList<string> MovesLearned { get; init; } = Array.Empty<string>();
    public IReadOnlyList<(string Item, int Count)> Berries { get; init; } = Array.Empty<(string, int)>();
    /// <summary>The effort gained, and in which stat.</summary>
    public StatType EffortStat { get; init; }
    public int Effort { get; init; }
    public int Friendship { get; init; }

    /// <summary>Puts what was found into the bag.</summary>
    public void GiveTo(Inventory bag)
    {
        foreach (var (item, count) in Berries)
            if (ItemDatabase.Get(item) is { } data) bag.AddItem(data, count);
    }
}

/// <summary>Why a Pokémon can't be sent on a job, or a job can't be collected.</summary>
public enum JobRefusal { None, Full, Egg, NoHours, ClockBack }

/// <summary>
/// The jobs board (plan 15 · E3; GPU-free): up to <see cref="Slots"/> Pokémon away at once, each paid when it is
/// collected for the whole hours that passed on the computer's clock (<see cref="WallClock.UtcNow"/>, handed in), never
/// more than asked for and nothing for a clock set back before it set off. The rates are one table here, ours, and a
/// session can turn them down: EXP by the hour as a share of the level it is at (a level's worth in eight hours, and
/// never more than a level's worth for one job), berries by kind, effort and friendship by the hour. Every draw is on
/// the <see cref="Random"/> handed over. Saved as <c>SaveData.Jobs</c> (null with none, so no save version was raised).
/// </summary>
public sealed class JobBoard
{
    /// <summary>How many Pokémon may be away at once.</summary>
    public const int Slots = 3;

    /// <summary>The fewest and most hours a job can be given.</summary>
    public const int MinHours = 1, MaxHours = 24;

    /// <summary>The hours a job takes to pay a level's worth, the most a job ever pays.</summary>
    public const int HoursToALevel = 8;

    private readonly List<Job> jobs = new();

    public IReadOnlyList<Job> Jobs => jobs;
    public bool IsFull => jobs.Count >= Slots;
    public bool IsEmpty => jobs.Count == 0;

    // ------------------------------------------------------------------ the rates

    /// <summary>EXP as a share of a level, by kind: an errand a level in eight hours, an orchard and a hot spring half that.</summary>
    public static int ExpFor(JobKind kind, int hours, int levelSpan)
    {
        if (hours <= 0 || levelSpan <= 0) return 0;
        int perLevel = kind == JobKind.Errand ? HoursToALevel : HoursToALevel * 2;
        return Math.Min(levelSpan, (int)((long)levelSpan * hours / perLevel));
    }

    /// <summary>Which stat a job's effort goes into: running errands Speed, the orchard's lifting HP, the hot spring Sp. Def.</summary>
    public static StatType EffortStatOf(JobKind kind) => kind switch { JobKind.Orchard => StatType.HP, JobKind.HotSpring => StatType.SpDefense, _ => StatType.Speed };

    /// <summary>A point of effort an hour.</summary>
    public const int EffortPerHour = 1;

    /// <summary>Friendship an hour: two in the hot spring, one elsewhere.</summary>
    public static int FriendshipPerHour(JobKind kind) => kind == JobKind.HotSpring ? 2 : 1;

    /// <summary>How many hours bring one berry: two in the orchard, six on an errand, none in the hot spring.</summary>
    public static int HoursPerBerry(JobKind kind) => kind switch { JobKind.Orchard => 2, JobKind.Errand => 6, _ => 0 };

    /// <summary>The berries a job may bring back, Platinum's own.</summary>
    public static readonly IReadOnlyList<string> BerryTable = new[]
    {
        "Oran Berry", "Oran Berry", "Pecha Berry", "Cheri Berry", "Chesto Berry", "Rawst Berry",
        "Aspear Berry", "Persim Berry", "Leppa Berry", "Sitrus Berry"
    };

    // ------------------------------------------------------------------ posting and collecting

    /// <summary>Why this Pokémon can't be sent for these hours now; <see cref="JobRefusal.None"/> when it can.</summary>
    public JobRefusal WhyNot(Pokemon pokemon, int hours)
    {
        if (IsFull) return JobRefusal.Full;
        if (pokemon.IsEgg) return JobRefusal.Egg;
        if (hours < MinHours || hours > MaxHours) return JobRefusal.NoHours;
        return JobRefusal.None;
    }

    /// <summary>Sends a Pokémon on a job (the caller has taken it out of its box); null, with nothing changed, when it can't go.</summary>
    public Job? Post(Pokemon pokemon, JobKind kind, int hours, DateTime now)
    {
        if (WhyNot(pokemon, hours) != JobRefusal.None) return null;
        var job = new Job
        {
            Pokemon = SavedPokemonData.FromPokemon(pokemon),
            Kind = kind,
            Started = now,
            Ends = now.AddHours(hours),
            Hours = hours
        };
        jobs.Add(job);
        return job;
    }

    /// <summary>
    /// Brings a Pokémon back and pays for the whole hours that passed, never more than asked for. A clock set back before
    /// the job began pays nothing and leaves the job where it is (<paramref name="refusal"/> says so); null then.
    /// </summary>
    public JobResult? Collect(Job job, DateTime now, Random random, out JobRefusal refusal)
    {
        refusal = JobRefusal.None;
        if (!jobs.Contains(job)) { refusal = JobRefusal.NoHours; return null; }
        if (now < job.Started) { refusal = JobRefusal.ClockBack; return null; }
        jobs.Remove(job);
        return Pay(job, job.HoursDone(now), random);
    }

    /// <summary>What a job's hours pay, on its Pokémon as it was held.</summary>
    public static JobResult Pay(Job job, int hours, Random random)
    {
        var pokemon = job.Pokemon.ToPokemon();
        int levelBefore = pokemon.Level;
        int span = Math.Max(0, pokemon.ExpForNextLevel - pokemon.ExpForCurrentLevel);
        int exp = pokemon.Level >= Pokemon.Cap ? 0 : ExpFor(job.Kind, hours, span);
        var learned = new List<string>();
        if (exp > 0)
        {
            pokemon.GainExp(exp, out var moves, out _, Pokemon.Cap);
            learned.AddRange(moves);
        }

        var stat = EffortStatOf(job.Kind);
        int effortBefore = EffortRules.Get(pokemon, stat);
        if (hours > 0) EffortRules.Gain(pokemon, SpreadOf(stat, hours * EffortPerHour));
        int effort = EffortRules.Get(pokemon, stat) - effortBefore;

        int friendshipBefore = pokemon.Friendship;
        if (hours > 0) FriendshipRules.ChangeByScript(pokemon, hours * FriendshipPerHour(job.Kind));

        var berries = new Dictionary<string, int>(StringComparer.Ordinal);
        int per = HoursPerBerry(job.Kind);
        if (per > 0)
            for (int i = 0; i < hours / per; i++)
            {
                string berry = BerryTable[random.Next(BerryTable.Count)];
                berries[berry] = berries.GetValueOrDefault(berry) + 1;
            }

        return new JobResult
        {
            Pokemon = pokemon,
            Kind = job.Kind,
            Hours = hours,
            Exp = exp,
            LevelsGained = pokemon.Level - levelBefore,
            MovesLearned = learned,
            Berries = berries.OrderBy(b => b.Key, StringComparer.Ordinal).Select(b => (b.Key, b.Value)).ToList(),
            EffortStat = stat,
            Effort = effort,
            Friendship = pokemon.Friendship - friendshipBefore
        };
    }

    private static StatSpread SpreadOf(StatType stat, int n) => stat switch
    {
        StatType.HP => new StatSpread { HP = n },
        StatType.SpDefense => new StatSpread { SpDefense = n },
        _ => new StatSpread { Speed = n }
    };

    // ------------------------------------------------------------------ the save

    public void Clear() => jobs.Clear();

    /// <summary>What a save keeps: the jobs, or null with none.</summary>
    public List<Job>? ToSaved() => jobs.Count == 0 ? null : jobs.ToList();

    /// <summary>Restores a save's jobs (null reads as none).</summary>
    public void Restore(IEnumerable<Job>? saved)
    {
        jobs.Clear();
        if (saved != null) jobs.AddRange(saved.Take(Slots));
    }

    /// <summary>The Pokémon away, as they are held (a save's id sweep and the Pokédex's counts never see them).</summary>
    public IEnumerable<SavedPokemonData> Away => jobs.Select(j => j.Pokemon);
}
