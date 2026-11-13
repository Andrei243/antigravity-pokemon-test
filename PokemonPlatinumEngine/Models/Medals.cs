using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>The categories the medals are kept in (plan 13 · V9), the medals' page's tabs in this order.</summary>
public enum MedalCategory { Journey, Battles, Catching, Growth, Pokedex, League }

/// <summary>
/// A medal (plan 13 · V9): won once what it counts (<see cref="Source"/>, a counter of <see cref="Statistics"/> or
/// one of the facts of <see cref="MedalFacts"/>) reaches <see cref="Goal"/>. Its id is the two together
/// (<c>stepsFoot.1000</c>), which is what the save keeps.
/// </summary>
public sealed record Medal(MedalCategory Category, string Source, long Goal)
{
    public string Id => $"{Source}.{Goal}";

    /// <summary>How far the facts have come toward it, at most its goal.</summary>
    public long Progress(MedalFacts facts) => Math.Min(Goal, Math.Max(0, facts.Value(Source)));

    public bool IsMet(MedalFacts facts) => facts.Value(Source) >= Goal;
}

/// <summary>
/// What the medals are judged by (plan 13 · V9): the game's counters (V8) and four facts the game keeps elsewhere,
/// the Pokédex's seen and caught counts, the badges of every region and the regions whose Hall of Fame is entered.
/// Plain values: a test makes one of its own.
/// </summary>
public sealed class MedalFacts
{
    public const string DexSeen = "dexSeen", DexCaught = "dexCaught", Badges = "badges", Champions = "champions", Circuits = "circuits",
        HeartsReclaimed = "heartsReclaimed", HeartsOpened = "heartsOpened", TourFourStars = "tourFourStars";

    /// <summary>The delve's hollows, each cleared once (plan 44 · SD9): a fact each, its source <c>hollow</c> and the hollow's key in camel case.</summary>
    public const string HollowBrinecave = "hollowBrinecave", HollowRootwood = "hollowRootwood", HollowHeartpeak = "hollowHeartpeak",
        HollowOldTower = "hollowOldTower";

    /// <summary>Each hollow's key (<c>Data/hollows.json</c>) by its medal's source.</summary>
    public static IReadOnlyDictionary<string, string> HollowKeys { get; } = new Dictionary<string, string>
    {
        [HollowBrinecave] = "brinecave", [HollowRootwood] = "rootwood", [HollowHeartpeak] = "heartpeak", [HollowOldTower] = "old_tower"
    };

    /// <summary>The facts that are no counter, in the order the medals list them.</summary>
    public static IReadOnlyList<string> Others { get; } = new[] { DexSeen, DexCaught, Badges, Champions, Circuits, HeartsReclaimed, HeartsOpened, TourFourStars,
        HollowBrinecave, HollowRootwood, HollowHeartpeak, HollowOldTower };

    public Statistics Counts { get; init; } = new();
    public int Seen { get; init; }
    public int Caught { get; init; }
    public int BadgeCount { get; init; }
    public int ChampionCount { get; init; }

    /// <summary>The Laurel Circuits won (plan 41 · EG4).</summary>
    public int CircuitCount { get; init; }

    /// <summary>Lockwork's locked Pokémon reclaimed and hearts opened (plan 44 · SD5).</summary>
    public int HeartsReclaimedCount { get; init; }
    public int HeartsOpenedCount { get; init; }

    /// <summary>The photo tour's four-star photographs the report keeps (plan 44 · SD6).</summary>
    public int TourFourStarCount { get; init; }

    /// <summary>The delve's hollows cleared, by key (plan 44 · SD9); empty while none is.</summary>
    public IReadOnlySet<string> HollowsCleared { get; init; } = new HashSet<string>();

    public long Value(string source) => HollowKeys.TryGetValue(source, out var hollow) ? (HollowsCleared.Contains(hollow) ? 1 : 0) : source switch
    {
        DexSeen => Seen,
        DexCaught => Caught,
        Badges => BadgeCount,
        Champions => ChampionCount,
        Circuits => CircuitCount,
        HeartsReclaimed => HeartsReclaimedCount,
        HeartsOpened => HeartsOpenedCount,
        TourFourStars => TourFourStarCount,
        _ => Counts.Get(source)
    };
}

/// <summary>
/// The medals (plan 13 · V9): a fixed list of 123 in six categories, each a goal of one counter or fact, and the
/// ones won, kept by id in <c>SaveData.Medals</c> (null while none is won, so no save version was raised). A medal
/// is won once and kept: <see cref="Check"/> gives back only those won now that weren't before. Plain data, no window.
/// </summary>
public sealed class Medals
{
    /// <summary>The most medals a category holds: what the card's page has room for (three columns of eight).</summary>
    public const int MostInACategory = 24;

    /// <summary>Every medal there is, by category, each source's goals rising.</summary>
    public static IReadOnlyList<Medal> All { get; } = Build();

    private static IReadOnlyList<Medal> Build()
    {
        var all = new List<Medal>();
        void Add(MedalCategory category, string source, params long[] goals)
        {
            foreach (long goal in goals) all.Add(new Medal(category, source, goal));
        }
        Add(MedalCategory.Journey, Statistics.StepsFoot, 100, 500, 1000, 2500, 5000, 10000, 25000, 50000, 100000, 250000);
        Add(MedalCategory.Journey, Statistics.StepsSurf, 100, 1000, 5000, 10000, 25000, 50000);
        Add(MedalCategory.Journey, Statistics.StepsCycle, 500, 5000, 20000, 50000);
        Add(MedalCategory.Journey, Statistics.StepsDive, 100, 1000, 5000);
        // A four-star photograph on the photo tour, a rare action caught (plan 44 · SD6)
        Add(MedalCategory.Journey, MedalFacts.TourFourStars, 1);

        Add(MedalCategory.Battles, Statistics.WildWon, 1, 10, 50, 100, 250, 500, 1000);
        Add(MedalCategory.Battles, Statistics.TrainerWon, 1, 10, 25, 50, 100, 250, 500, 1000);
        Add(MedalCategory.Battles, Statistics.WildRan, 1, 25, 100);
        Add(MedalCategory.Battles, Statistics.WildFled, 1, 25);
        Add(MedalCategory.Battles, Statistics.TrainerLost, 1);
        Add(MedalCategory.Battles, Statistics.Whiteouts, 1, 10);

        Add(MedalCategory.Catching, Statistics.Catches, 1, 5, 10, 25, 50, 100, 250, 500, 1000);
        Add(MedalCategory.Catching, Statistics.NewSpecies, 5, 10, 25, 50, 100, 150, 200, 300, 400);
        Add(MedalCategory.Catching, Statistics.Encounters, 1, 50, 100, 500, 1000, 2500);

        Add(MedalCategory.Growth, Statistics.Levelled, 1, 10, 50, 100, 250, 500, 1000, 2500);
        Add(MedalCategory.Growth, Statistics.Evolutions, 1, 5, 10, 25, 50, 100, 200, 300);

        Add(MedalCategory.Pokedex, MedalFacts.DexSeen, 10, 25, 50, 100, 151, 210, 300, 493, 700, 1025);
        Add(MedalCategory.Pokedex, MedalFacts.DexCaught, 10, 25, 50, 100, 151, 210, 300, 493, 700, 1025);

        Add(MedalCategory.League, MedalFacts.Badges, 1, 2, 4, 8, 16, 24, 32, 40, 48);
        Add(MedalCategory.League, MedalFacts.Champions, 1, 2, 3, 4, 5);
        // The Laurel Circuit's finals won (plan 41 · EG4)
        Add(MedalCategory.League, MedalFacts.Circuits, 1, 3, 5);
        // Lockwork's locked hearts: the first reclaimed, the thirtieth opened (plan 44 · SD5)
        Add(MedalCategory.League, MedalFacts.HeartsReclaimed, 1);
        Add(MedalCategory.League, MedalFacts.HeartsOpened, 30);
        // Each of the delve's four hollows cleared (plan 44 · SD9; the Journey tab is full)
        Add(MedalCategory.League, MedalFacts.HollowBrinecave, 1);
        Add(MedalCategory.League, MedalFacts.HollowRootwood, 1);
        Add(MedalCategory.League, MedalFacts.HollowHeartpeak, 1);
        Add(MedalCategory.League, MedalFacts.HollowOldTower, 1);
        return all;
    }

    public static IEnumerable<Medal> In(MedalCategory category) => All.Where(m => m.Category == category);

    public static Medal? Find(string id) => All.FirstOrDefault(m => m.Id == id);

    private readonly HashSet<string> won = new(StringComparer.Ordinal);

    public int Count => won.Count;
    public bool IsEmpty => won.Count == 0;
    public bool Has(Medal medal) => won.Contains(medal.Id);
    public bool Has(string id) => won.Contains(id);

    /// <summary>Forgets every medal (a tool puts things back as it found them).</summary>
    public void Clear() => won.Clear();

    /// <summary>
    /// Wins every medal the facts reach that isn't won yet and gives those back, in the list's order: each medal is
    /// given back once in a game's whole life, whatever is asked after.
    /// </summary>
    public IReadOnlyList<Medal> Check(MedalFacts facts)
    {
        List<Medal>? now = null;
        foreach (var medal in All)
            if (!won.Contains(medal.Id) && medal.IsMet(facts) && won.Add(medal.Id))
                (now ??= new List<Medal>()).Add(medal);
        return (IReadOnlyList<Medal>?)now ?? Array.Empty<Medal>();
    }

    /// <summary>The medals as the save keeps them: their ids in the list's order, or null while none is won.</summary>
    public List<string>? ToSaved() => IsEmpty ? null : All.Where(m => won.Contains(m.Id)).Select(m => m.Id).ToList();

    /// <summary>Medals read from a save; an id this game doesn't name (a newer game's) is passed over.</summary>
    public static Medals From(IEnumerable<string>? saved)
    {
        var m = new Medals();
        if (saved == null) return m;
        foreach (string id in saved)
            if (Find(id) != null) m.won.Add(id);
        return m;
    }
}
