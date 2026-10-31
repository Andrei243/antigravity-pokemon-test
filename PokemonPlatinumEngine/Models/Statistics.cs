using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Models;

/// <summary>The groups the statistics page lists its counters in (plan 13 · V8), its tabs in this order.</summary>
public enum StatGroup { Journey, Battles, Catching, Growth }

/// <summary>
/// What the game has counted (plan 13 · V8): a number to each of a fixed list of keys, kept whole in
/// <c>SaveData.Counters</c> (null while nothing is counted, so no save version was raised). Every count is one line,
/// <see cref="Count"/>, at the point of the engine that knows it; a key not named in <see cref="All"/> throws, so the
/// page and the file can never part. Medals (V9) are predicates over these. Plain data, no window.
/// </summary>
public sealed class Statistics
{
    // Steps, by how the player travels
    public const string StepsFoot = "stepsFoot", StepsSurf = "stepsSurf", StepsCycle = "stepsCycle", StepsDive = "stepsDive";
    // Battles begun in the field, by who and how they ended; the Frontier's and a watched replay are not counted
    public const string WildWon = "wildWon", WildLost = "wildLost", WildRan = "wildRan", WildFled = "wildFled";
    public const string TrainerWon = "trainerWon", TrainerLost = "trainerLost", Whiteouts = "whiteouts";
    // Wild Pokémon met, caught, and caught for the first time of their species
    public const string Encounters = "encounters", Catches = "caught", NewSpecies = "newSpecies";
    // Pokémon that grew a level in a battle, and evolutions that happened
    public const string Levelled = "levelled", Evolutions = "evolutions";

    /// <summary>Every key there is, in the order the page lists them.</summary>
    public static readonly IReadOnlyList<(string Key, StatGroup Group)> All = new (string, StatGroup)[]
    {
        (StepsFoot, StatGroup.Journey), (StepsSurf, StatGroup.Journey), (StepsCycle, StatGroup.Journey), (StepsDive, StatGroup.Journey),
        (WildWon, StatGroup.Battles), (WildLost, StatGroup.Battles), (WildRan, StatGroup.Battles), (WildFled, StatGroup.Battles),
        (TrainerWon, StatGroup.Battles), (TrainerLost, StatGroup.Battles), (Whiteouts, StatGroup.Battles),
        (Encounters, StatGroup.Catching), (Catches, StatGroup.Catching), (NewSpecies, StatGroup.Catching),
        (Levelled, StatGroup.Growth), (Evolutions, StatGroup.Growth),
    };

    public static IEnumerable<string> KeysOf(StatGroup group) => All.Where(k => k.Group == group).Select(k => k.Key);

    private readonly Dictionary<string, long> counts = new(StringComparer.Ordinal);

    public bool IsEmpty => counts.Count == 0;

    /// <summary>Forgets every count (a tool puts things back as it found them).</summary>
    public void Clear() => counts.Clear();

    public long Get(string key) => counts.TryGetValue(key, out long n) ? n : 0;

    /// <summary>Adds <paramref name="n"/> (1 unless said) to a key; a key the list doesn't name is a mistake.</summary>
    public void Count(string key, long n = 1)
    {
        if (All.All(k => k.Key != key)) throw new ArgumentException($"'{key}' is no statistic", nameof(key));
        if (n <= 0) return;
        counts[key] = Get(key) + n;
    }

    /// <summary>The steps counter for how the player travels.</summary>
    public static string StepKey(Overworld.TravelMode mode) => mode switch
    {
        Overworld.TravelMode.Surfing => StepsSurf,
        Overworld.TravelMode.Cycling => StepsCycle,
        Overworld.TravelMode.Diving => StepsDive,
        _ => StepsFoot
    };

    /// <summary>
    /// Counts a battle of the field once it is over: a wild or a trainer's, by its result. A catch is counted
    /// by <see cref="Catches"/> and nothing else; a defeat is a whiteout too.
    /// </summary>
    public void CountBattle(BattleResult result, bool trainer, bool caughtNewSpecies = false)
    {
        string? key = (trainer, result) switch
        {
            (true, BattleResult.PlayerVictory) => TrainerWon,
            (true, BattleResult.PlayerDefeat) => TrainerLost,
            (false, BattleResult.PlayerVictory) => WildWon,
            (false, BattleResult.PlayerDefeat) => WildLost,
            (false, BattleResult.PlayerRan) => WildRan,
            (false, BattleResult.EnemyFled) => WildFled,
            (false, BattleResult.EnemyCaught) => Catches,
            _ => null
        };
        if (key == null) return;
        Count(key);
        if (key == Catches && caughtNewSpecies) Count(NewSpecies);
        if (result == BattleResult.PlayerDefeat) Count(Whiteouts);
    }

    /// <summary>The counters as the save keeps them: null while nothing is counted.</summary>
    public Dictionary<string, long>? ToSaved() => IsEmpty ? null : new Dictionary<string, long>(counts);

    /// <summary>Counters read from a save; a key this game doesn't name (a newer game's) is passed over.</summary>
    public static Statistics From(IReadOnlyDictionary<string, long>? saved)
    {
        var s = new Statistics();
        if (saved == null) return s;
        foreach (var (key, n) in saved)
            if (All.Any(k => k.Key == key) && n > 0) s.counts[key] = n;
        return s;
    }
}
