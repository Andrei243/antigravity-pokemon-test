using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A timed power (plan 36 · Powers, "The rule"; <c>docs/mechanics/rulings.md</c>, "Powers"). The first eight are plan 36's
/// and their readers are its session TR7's; plan 39 · CR6 added <see cref="TypeEncounter"/> and <see cref="Materials"/> at
/// the end. A new kind is a value at the end, a row of <c>Data/powers.json</c> and its one reader.
/// </summary>
public enum PowerKind { Exp, Effort, Money, Bargain, Catch, Encounter, Stealth, Hatch, TypeEncounter, Materials }

/// <summary>One kind's row of <c>Data/powers.json</c>: what it comes to at each of its three levels (its reader says in what).</summary>
public sealed class PowerRow
{
    [DataRequired] public PowerKind Kind { get; set; }
    [DataRequired] public List<int> Strength { get; set; } = new();
}

/// <summary>
/// The file <c>Data/powers.json</c> (ours, read strictly): how long a power lasts unless granted otherwise, each kind's
/// strengths, the charge (plan 36 · TR7) and what each region's teacher teaches (TR8).
/// </summary>
public sealed class PowersFile
{
    [DataRequired] public int Seconds { get; set; } = 600;
    public List<PowerRow> Powers { get; set; } = new();
    /// <summary>The most charge the watch holds.</summary>
    public int ChargeMost { get; set; } = 10;
    /// <summary>The steps a point of charge takes.</summary>
    public int StepsPerCharge { get; set; } = 200;
    /// <summary>The kinds each region's training hall's teacher teaches, by the region's id (plan 36's decision 5).</summary>
    public Dictionary<string, List<PowerKind>> Teachers { get; set; } = new();
}

/// <summary>
/// The four powers a battle reads, as plain strengths in hundredths (plan 36 · TR7): fixed as the battle begins
/// (<see cref="Battle.Sim.BattleConditions.Powers"/>), lasting to its end and kept by its record's file, so a replay gives
/// the same log. Nought for a kind not in force.
/// </summary>
public sealed class PowerSet
{
    /// <summary>EXP added, in hundredths of what comes after affection.</summary>
    public int Exp { get; set; }
    /// <summary>Effort added, in hundredths of each foe's yield.</summary>
    public int Effort { get; set; }
    /// <summary>Prize money added, in hundredths.</summary>
    public int Money { get; set; }
    /// <summary>The species' catch rate raised, in hundredths.</summary>
    public int Catch { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmpty => Exp == 0 && Effort == 0 && Money == 0 && Catch == 0;

    /// <summary>An amount with a power's hundredths added, rounded down; the amount itself with none.</summary>
    public static int Raise(int amount, int hundredths) => hundredths <= 0 ? amount : amount + (int)((long)amount * hundredths / 100);
}

/// <summary>Why a power can't be used from the watch (plan 36 · TR7), in the order it is asked.</summary>
public enum PowerRefusal
{
    None = 0,
    /// <summary>The run chosen bars powers (plan 13's NO POWERS).</summary>
    NoPowers,
    /// <summary>The kind isn't learned to that level.</summary>
    NotLearned,
    /// <summary>Less charge than the level costs.</summary>
    TooLittleCharge
}

/// <summary>A power in force: its kind, its level (one to three), the seconds of play left, and for a type's encounters the type.</summary>
public sealed record ActivePower(PowerKind Kind, int Level, float SecondsLeft, PokemonType? Type = null);

/// <summary>A power as a save keeps it.</summary>
public sealed class SavedPower
{
    public PowerKind Kind { get; set; }
    public int Level { get; set; }
    public float Seconds { get; set; }
    public PokemonType? Type { get; set; }
}

/// <summary>What the game keeps of its powers: those in force, and (plan 36 · TR7) the levels learned, the charge and the day's free one.</summary>
public sealed class PowersSave
{
    public List<SavedPower>? InForce { get; set; }
    /// <summary>The levels learned, by the kind's name.</summary>
    public Dictionary<string, int>? Learned { get; set; }
    public int Charge { get; set; }
    /// <summary>The steps walked toward the next point of charge.</summary>
    public int Steps { get; set; }
    /// <summary>The day's number (<see cref="SpecialEncounters.DailyNumber"/>) whose free power was taken; null for none yet.</summary>
    public uint? FreeTaken { get; set; }
}

/// <summary>
/// The powers' table (<c>Data/powers.json</c>) and the kinds whose readers are built. No drawing or input.
/// </summary>
public static class PowerTable
{
    /// <summary>The most a power's level comes to.</summary>
    public const int TopLevel = 3;

    private static readonly Lazy<PowersFile> file = new(() => GameDataFiles.Load<PowersFile>("powers.json", strict: true));

    public static PowersFile File => file.Value;

    /// <summary>The seconds of play a power lasts when its giver names no length.</summary>
    public static int Seconds => File.Seconds;

    public static PowerRow? Row(PowerKind kind) => File.Powers.FirstOrDefault(r => r.Kind == kind);

    /// <summary>
    /// The kinds something reads, each in one place: <see cref="PowerKind.Exp"/> and <see cref="PowerKind.Effort"/> in
    /// <c>BattleCore.Give</c>, <see cref="PowerKind.Money"/> in <c>BattleCore.PrizeMoney</c>, <see cref="PowerKind.Catch"/> on
    /// the species' rate in <c>CatchCalculator.Shakes</c> (the four through the battle's <see cref="PowerSet"/>, plan 36 · TR7),
    /// <see cref="PowerKind.Bargain"/> in <c>ShopScreen.PriceOf</c>, <see cref="PowerKind.Encounter"/> and
    /// <see cref="PowerKind.Stealth"/> by a step's odds (<c>Map.RollWildEncounter</c>), <see cref="PowerKind.TypeEncounter"/> by
    /// the grass's slots (<c>EncounterSlots.Grass</c>), <see cref="PowerKind.Hatch"/> by the Eggs' cycles
    /// (<see cref="Breeding.CountDown"/>) and <see cref="PowerKind.Materials"/> by what a wild Pokémon leaves
    /// (<see cref="MaterialDrops.For(Pokemon, Random, int)"/>). Every kind is read since TR7.
    /// </summary>
    public static readonly IReadOnlySet<PowerKind> Read = new HashSet<PowerKind>(Enum.GetValues<PowerKind>());

    /// <summary>The kinds the watch can use and a teacher teaches: plan 36's eight (the camp's two come only from its dishes).</summary>
    public static IReadOnlyList<PowerKind> Taught { get; } = new[]
    {
        PowerKind.Exp, PowerKind.Effort, PowerKind.Money, PowerKind.Bargain, PowerKind.Catch, PowerKind.Encounter, PowerKind.Stealth, PowerKind.Hatch
    };

    /// <summary>The most charge the watch holds.</summary>
    public static int ChargeMost => File.ChargeMost;

    /// <summary>The steps a point of charge takes.</summary>
    public static int StepsPerCharge => File.StepsPerCharge;

    /// <summary>What a level costs from the charge: the level itself (plan 36: a power costs its level).</summary>
    public static int CostOf(int level) => Math.Clamp(level, 1, TopLevel);

    /// <summary>The kinds a region's teacher teaches, in the order taught; none for a region with no row.</summary>
    public static IReadOnlyList<PowerKind> TaughtIn(string region) =>
        File.Teachers.TryGetValue(region, out var kinds) ? kinds : (IReadOnlyList<PowerKind>)Array.Empty<PowerKind>();

    /// <summary>The level a teacher teaches at, by the Halls of Fame entered (ours): one with one, two with two, three with four or more; nought with none.</summary>
    public static int TeachingLevel(int hallsOfFame) => hallsOfFame >= 4 ? 3 : hallsOfFame >= 2 ? 2 : hallsOfFame >= 1 ? 1 : 0;

    /// <summary>What a kind comes to at a level (one to three); nought for level nought or a kind with no row.</summary>
    public static int Strength(PowerKind kind, int level) =>
        level < 1 || Row(kind) is not { } row || row.Strength.Count == 0 ? 0 : row.Strength[Math.Min(level, row.Strength.Count) - 1];

    /// <summary>The name a kind is shown by (<c>powers.kind.*</c>).</summary>
    public static string NameOf(PowerKind kind)
    {
        string key = kind.ToString();
        key = char.ToLowerInvariant(key[0]) + key[1..];
        return Text.Of($"timedPowers.kind.{key}");
    }
}

/// <summary>
/// The powers in force (plan 36 · Powers, the one door every source grants through; plan 39 · CR6 is its first source,
/// the camp's dishes): one of each kind at once, a new one replacing the old, and <see cref="PowerKind.Encounter"/> and
/// <see cref="PowerKind.Stealth"/> putting each other out. Lengths count down from the play time summed from each frame's
/// <c>dt</c>, never the wall clock, so a paused game loses nothing and the harness repeats. Nothing here is drawn by
/// chance. Plan 36 · TR7 adds the levels learned, the charge and the day's free one beside it. No drawing or input.
/// </summary>
public sealed class PowerState
{
    private readonly Dictionary<PowerKind, ActivePower> inForce = new();

    /// <summary>The powers in force, in the kinds' order.</summary>
    public IReadOnlyList<ActivePower> InForce => inForce.Values.OrderBy(p => p.Kind).ToList();

    /// <summary>Counts every change, so a screen knows to look again. Not saved.</summary>
    public int Revision { get; private set; }

    public bool Has(PowerKind kind) => inForce.ContainsKey(kind);

    /// <summary>The level of a kind in force, nought when none is.</summary>
    public int LevelOf(PowerKind kind) => inForce.TryGetValue(kind, out var p) ? p.Level : 0;

    /// <summary>What a kind in force comes to (its row's strength at its level); nought when none is.</summary>
    public int Strength(PowerKind kind) => PowerTable.Strength(kind, LevelOf(kind));

    /// <summary>The type of the type's encounters in force; null when none is.</summary>
    public PokemonType? TypeOf(PowerKind kind) => inForce.TryGetValue(kind, out var p) ? p.Type : null;

    /// <summary>The seconds of play a kind in force has left, nought when none is.</summary>
    public float SecondsLeft(PowerKind kind) => inForce.TryGetValue(kind, out var p) ? p.SecondsLeft : 0f;

    /// <summary>
    /// The one door (plan 36): puts a power in force for <paramref name="seconds"/> of play at a level of one to three,
    /// replacing one of its kind; the encounters' and stealth's put each other out. A type's encounters needs its type.
    /// False and nothing changes for no time, no level or a type's encounters with no type.
    /// </summary>
    public bool Grant(PowerKind kind, int level, float seconds, PokemonType? type = null)
    {
        if (seconds <= 0 || level < 1 || (kind == PowerKind.TypeEncounter) != (type != null)) return false;
        level = Math.Min(level, PowerTable.TopLevel);
        if (kind == PowerKind.Encounter) inForce.Remove(PowerKind.Stealth);
        if (kind == PowerKind.Stealth) inForce.Remove(PowerKind.Encounter);
        inForce[kind] = new ActivePower(kind, level, seconds, type);
        Revision++;
        return true;
    }

    /// <summary>The time on by <paramref name="dt"/> seconds of play: the kinds that wear off now, in order (empty most frames).</summary>
    public List<PowerKind> Tick(float dt)
    {
        var worn = new List<PowerKind>();
        if (dt <= 0 || inForce.Count == 0) return worn;
        foreach (var kind in inForce.Keys.OrderBy(k => k).ToList())
        {
            var p = inForce[kind];
            float left = p.SecondsLeft - dt;
            if (left <= 0)
            {
                inForce.Remove(kind);
                worn.Add(kind);
            }
            else inForce[kind] = p with { SecondsLeft = left };
        }
        if (worn.Count > 0) Revision++;
        return worn;
    }

    // ---------------------------------------------------------------- the watch (plan 36 · TR7)

    private readonly Dictionary<PowerKind, int> learned = new();

    /// <summary>The charge, nought to <see cref="PowerTable.ChargeMost"/>, which walking fills.</summary>
    public int Charge { get; private set; }

    /// <summary>The steps walked toward the next point of charge.</summary>
    public int Steps { get; private set; }

    /// <summary>The day's number whose free power was taken; null for none yet.</summary>
    public uint? FreeTaken { get; private set; }

    /// <summary>The level learned of a kind, nought for none.</summary>
    public int Learned(PowerKind kind) => learned.GetValueOrDefault(kind);

    /// <summary>The kinds learned, in the kinds' order.</summary>
    public IReadOnlyList<PowerKind> LearnedKinds => learned.Keys.OrderBy(k => k).ToList();

    /// <summary>
    /// A kind learned to a level (a teacher's <c>power learn</c>): kept where it is higher than what was. Returns whether
    /// anything changed. Only the watch's eight are learned.
    /// </summary>
    public bool Learn(PowerKind kind, int level)
    {
        level = Math.Min(level, PowerTable.TopLevel);
        if (level < 1 || !PowerTable.Taught.Contains(kind) || Learned(kind) >= level) return false;
        learned[kind] = level;
        Revision++;
        return true;
    }

    /// <summary>A step walked: a point of charge every <see cref="PowerTable.StepsPerCharge"/>, up to the most. Returns whether a point came.</summary>
    public bool Walk()
    {
        if (Charge >= PowerTable.ChargeMost) return false;
        if (++Steps < PowerTable.StepsPerCharge) return false;
        Steps = 0;
        Charge++;
        Revision++;
        return true;
    }

    /// <summary>Charge given outright (a tool's, a test's), held to the most.</summary>
    public void AddCharge(int points)
    {
        Charge = Math.Clamp(Charge + points, 0, PowerTable.ChargeMost);
        Revision++;
    }

    /// <summary>Why a kind can't be used from the watch at a level; <see cref="PowerRefusal.None"/> when it can.</summary>
    public PowerRefusal WhyNot(PowerKind kind, int level, bool noPowers = false)
    {
        if (noPowers) return PowerRefusal.NoPowers;
        if (level < 1 || Learned(kind) < level) return PowerRefusal.NotLearned;
        return Charge < PowerTable.CostOf(level) ? PowerRefusal.TooLittleCharge : PowerRefusal.None;
    }

    /// <summary>Uses a kind from the watch: its level paid from the charge and the power put in force for the table's length. False, and nothing paid, where it is refused.</summary>
    public bool Use(PowerKind kind, int level, bool noPowers = false)
    {
        if (WhyNot(kind, level, noPowers) != PowerRefusal.None) return false;
        Charge -= PowerTable.CostOf(level);
        return Grant(kind, level, PowerTable.Seconds);
    }

    /// <summary>
    /// Today's free power (plan 36 · TR7): one of the kinds learned, at level one or two (never past the level learned),
    /// worked out from the day's number by <see cref="SplitMix"/>, never drawn, so it is the same all day and moves no
    /// other draw. Null with nothing learned, or once today's was taken.
    /// </summary>
    public (PowerKind Kind, int Level)? FreeToday(uint dailyNumber)
    {
        if (learned.Count == 0 || FreeTaken == dailyNumber) return null;
        var kinds = LearnedKinds;
        uint hash = SplitMix.Hash(unchecked((int)dailyNumber), 0x9077E2u);
        var kind = kinds[(int)(hash % (uint)kinds.Count)];
        int level = Math.Min(Learned(kind), 1 + (int)((hash >> 16) & 1));
        return (kind, level);
    }

    /// <summary>Today's free power put in force at no charge, once a day. False where there is none to take (or the run bars powers).</summary>
    public bool TakeFree(uint dailyNumber, bool noPowers = false)
    {
        if (noPowers || FreeToday(dailyNumber) is not { } free) return false;
        FreeTaken = dailyNumber;
        return Grant(free.Kind, free.Level, PowerTable.Seconds);
    }

    /// <summary>The battle's powers as plain strengths, fixed as it begins (plan 36 · TR7).</summary>
    public PowerSet ForBattle() => new()
    {
        Exp = Strength(PowerKind.Exp),
        Effort = Strength(PowerKind.Effort),
        Money = Strength(PowerKind.Money),
        Catch = Strength(PowerKind.Catch)
    };

    public void Clear() => Load(null);

    public void Load(PowersSave? save)
    {
        inForce.Clear();
        learned.Clear();
        foreach (var p in save?.InForce ?? new())
            if (p.Seconds > 0 && p.Level >= 1 && (p.Kind == PowerKind.TypeEncounter) == (p.Type != null))
                inForce[p.Kind] = new ActivePower(p.Kind, Math.Min(p.Level, PowerTable.TopLevel), p.Seconds, p.Type);
        foreach (var (name, level) in save?.Learned ?? new())
            if (Enum.TryParse<PowerKind>(name, out var kind) && PowerTable.Taught.Contains(kind) && level >= 1)
                learned[kind] = Math.Min(level, PowerTable.TopLevel);
        Charge = Math.Clamp(save?.Charge ?? 0, 0, PowerTable.ChargeMost);
        Steps = Math.Clamp(save?.Steps ?? 0, 0, Math.Max(0, PowerTable.StepsPerCharge - 1));
        FreeTaken = save?.FreeTaken;
        Revision++;
    }

    /// <summary>The save's record; null while nothing is in force, learned or charged, so no version was raised.</summary>
    public PowersSave? ToSave() => inForce.Count == 0 && learned.Count == 0 && Charge == 0 && Steps == 0 && FreeTaken == null ? null : new PowersSave
    {
        InForce = inForce.Count == 0 ? null : InForce.Select(p => new SavedPower { Kind = p.Kind, Level = p.Level, Seconds = p.SecondsLeft, Type = p.Type }).ToList(),
        Learned = learned.Count == 0 ? null : LearnedKinds.ToDictionary(k => k.ToString(), k => learned[k]),
        Charge = Charge,
        Steps = Steps,
        FreeTaken = FreeTaken
    };
}
