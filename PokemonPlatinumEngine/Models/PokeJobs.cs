using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What a Pokémon is sent to do (plan 15 · E3; Sun and Moon's islands and Sword and Shield's jobs, in our own words):
/// an errand about town, an orchard's harvest, a soak in a hot spring; and since plan 35 · BN6 the pasture's nursery, which
/// keeps one Egg warm for hours and hatches it as it is collected, and its cave, where finds turn up by the hour.
/// </summary>
public enum JobKind { Errand, Orchard, HotSpring, Nursery, Cave }

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

    /// <summary>What else it brought back (plan 35 · BN6): the orchard's treat, the cave's finds.</summary>
    public IReadOnlyList<(string Item, int Count)> Finds { get; init; } = Array.Empty<(string, int)>();

    /// <summary>The nursery's Egg had no cycles left and hatches as it is collected (plan 35 · BN6).</summary>
    public bool Hatches { get; init; }

    /// <summary>The Egg cycles the nursery's hours counted down (plan 35 · BN6).</summary>
    public int EggCycles { get; init; }
    /// <summary>The effort gained, and in which stat.</summary>
    public StatType EffortStat { get; init; }
    public int Effort { get; init; }
    public int Friendship { get; init; }

    /// <summary>Puts what was found into the bag.</summary>
    public void GiveTo(Inventory bag)
    {
        foreach (var (item, count) in Berries.Concat(Finds))
            if (ItemDatabase.Get(item) is { } data) bag.AddItem(data, count);
    }
}

/// <summary>Why a Pokémon can't be sent on a job, or a job can't be collected.</summary>
public enum JobRefusal { None, Full, Egg, NoHours, ClockBack, Locked, NotAnEgg }

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
    /// <summary>How many Pokémon may be away at once: six since the pasture opened its fields (plan 35 · BN6, decision 5; three before).</summary>
    public const int Slots = 6;

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
    public JobRefusal WhyNot(Pokemon pokemon, int hours) => WhyNot(pokemon, hours, pokemon.IsEgg ? JobKind.Nursery : JobKind.Errand);

    /// <summary>Why this Pokémon can't be sent on this kind of job: the nursery takes an Egg and nothing else, every other kind no Egg.</summary>
    public JobRefusal WhyNot(Pokemon pokemon, int hours, JobKind kind)
    {
        if (IsFull) return JobRefusal.Full;
        if (kind == JobKind.Nursery && !pokemon.IsEgg) return JobRefusal.NotAnEgg;
        if (kind != JobKind.Nursery && pokemon.IsEgg) return JobRefusal.Egg;
        // A locked heart isn't sent (plan 44 · SD1)
        if (HeartLock.Refuses(pokemon, LockedUse.Jobs)) return JobRefusal.Locked;
        if (hours < MinHours || hours > MaxHours) return JobRefusal.NoHours;
        return JobRefusal.None;
    }

    /// <summary>Sends a Pokémon on a job (the caller has taken it out of its box); null, with nothing changed, when it can't go.</summary>
    public Job? Post(Pokemon pokemon, JobKind kind, int hours, DateTime now)
    {
        if (WhyNot(pokemon, hours, kind) != JobRefusal.None) return null;
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
    public JobResult? Collect(Job job, DateTime now, Random random, out JobRefusal refusal) =>
        Collect(job, now, random, RegionDatabase.Sinnoh, out refusal);

    /// <summary>The same, collected in a region: the cave's finds are its region's (plan 35 · BN6).</summary>
    public JobResult? Collect(Job job, DateTime now, Random random, string region, out JobRefusal refusal)
    {
        refusal = JobRefusal.None;
        if (!jobs.Contains(job)) { refusal = JobRefusal.NoHours; return null; }
        if (now < job.Started) { refusal = JobRefusal.ClockBack; return null; }
        jobs.Remove(job);
        return Pay(job, job.HoursDone(now), random, region);
    }

    /// <summary>What a job's hours pay, on its Pokémon as it was held.</summary>
    public static JobResult Pay(Job job, int hours, Random random, string region = RegionDatabase.Sinnoh)
    {
        var pokemon = job.Pokemon.ToPokemon();
        // The nursery keeps its Egg warm and pays nothing else: an Egg's friendship field is its cycles (plan 35 · BN6)
        if (job.Kind == JobKind.Nursery) return Warm(pokemon, hours);
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

        // The orchard's treat and the cave's finds (plan 35 · BN6), drawn after the berries so that a harvest's berries
        // are what they were
        var finds = new Dictionary<string, int>(StringComparer.Ordinal);
        if (job.Kind == JobKind.Orchard)
            for (int i = 0; i < hours / HoursPerTreat; i++)
            {
                string treat = OrchardTreats[random.Next(OrchardTreats.Count)];
                finds[treat] = finds.GetValueOrDefault(treat) + 1;
            }
        if (job.Kind == JobKind.Cave)
        {
            var table = CaveTable(region);
            for (int i = 0; i < CaveFinds(pokemon, hours); i++)
            {
                string found = table[random.Next(table.Count)];
                finds[found] = finds.GetValueOrDefault(found) + 1;
            }
        }

        return new JobResult
        {
            Pokemon = pokemon,
            Kind = job.Kind,
            Hours = hours,
            Finds = finds.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => (f.Key, f.Value)).ToList(),
            Exp = exp,
            LevelsGained = pokemon.Level - levelBefore,
            MovesLearned = learned,
            Berries = berries.OrderBy(b => b.Key, StringComparer.Ordinal).Select(b => (b.Key, b.Value)).ToList(),
            EffortStat = stat,
            Effort = effort,
            Friendship = pokemon.Friendship - friendshipBefore
        };
    }

    // ------------------------------------------------------------------ the pasture's fields (plan 35 · BN6)

    /// <summary>The Egg cycles the nursery counts down an hour (ours: a night of eight hours hatches an Egg of forty).</summary>
    public const int CyclesPerHour = 5;

    /// <summary>How many hours in the orchard bring one of the sold treats besides its berries.</summary>
    public const int HoursPerTreat = 8;

    /// <summary>How many hours in the cave bring a find; a Rock, Ground or Steel type finds one more every <see cref="HoursPerExtraFind"/>.</summary>
    public const int HoursPerFind = 3, HoursPerExtraFind = 6;

    /// <summary>The treats the orchard's pickers bring back now and then: the five sold ones (plan 35 · BN3).</summary>
    public static readonly IReadOnlyList<string> OrchardTreats = new[] { "Pepper Bite", "Crisp Wafer", "Honey Drop", "Cocoa Nib", "Tart Chew" };

    /// <summary>
    /// What the cave gives by region (ours): evolution stones, shards, treasures to sell and Heart Scales. Never a fossil
    /// or anything that gives a species, so no region's list of ways to a species moves.
    /// </summary>
    public static IReadOnlyList<string> CaveTable(string region) => region switch
    {
        RegionDatabase.Kanto => new[] { "Fire Stone", "Water Stone", "Thunder Stone", "Moon Stone", "Red Shard", "Blue Shard", "Nugget", "Stardust", "Heart Scale" },
        RegionDatabase.Johto => new[] { "Sun Stone", "Moon Stone", "Leaf Stone", "Yellow Shard", "Green Shard", "Pearl", "Star Piece", "Stardust", "Heart Scale" },
        RegionDatabase.Hoenn => new[] { "Fire Stone", "Water Stone", "Leaf Stone", "Red Shard", "Blue Shard", "Yellow Shard", "Green Shard", "Star Piece", "Heart Scale" },
        RegionDatabase.Unova => new[] { "Thunder Stone", "Dusk Stone", "Dawn Stone", "Shiny Stone", "Blue Shard", "Pearl", "Nugget", "Stardust", "Heart Scale" },
        _ => new[] { "Fire Stone", "Water Stone", "Thunder Stone", "Leaf Stone", "Dusk Stone", "Red Shard", "Blue Shard", "Star Piece", "Heart Scale" }
    };

    /// <summary>How many finds a Pokémon brings back from the cave for these hours.</summary>
    public static int CaveFinds(Pokemon pokemon, int hours)
    {
        if (hours <= 0) return 0;
        int finds = hours / HoursPerFind;
        if (pokemon.HasType(PokemonType.Rock) || pokemon.HasType(PokemonType.Ground) || pokemon.HasType(PokemonType.Steel))
            finds += hours / HoursPerExtraFind;
        return finds;
    }

    /// <summary>The nursery's hours on its Egg: its cycles counted down, and whether it hatches as it is collected.</summary>
    private static JobResult Warm(Pokemon egg, int hours)
    {
        int before = egg.EggCycles;
        if (hours > 0 && egg.IsEgg) egg.EggCycles = Math.Max(0, egg.EggCycles - hours * CyclesPerHour);
        return new JobResult
        {
            Pokemon = egg,
            Kind = JobKind.Nursery,
            Hours = hours,
            EggCycles = before - egg.EggCycles,
            Hatches = egg.IsEgg && egg.EggCycles == 0,
            EffortStat = StatType.HP
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
        // (never cut to the board's size: a save keeps every Pokémon it sent)
        if (saved != null) jobs.AddRange(saved);
    }

    /// <summary>The Pokémon away, as they are held (a save's id sweep and the Pokédex's counts never see them).</summary>
    public IEnumerable<SavedPokemonData> Away => jobs.Select(j => j.Pokemon);
}
