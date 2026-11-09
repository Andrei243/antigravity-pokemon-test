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

/// <summary>The file <c>Data/powers.json</c> (ours, read strictly): how long a power lasts unless granted otherwise, and each kind's strengths.</summary>
public sealed class PowersFile
{
    [DataRequired] public int Seconds { get; set; } = 600;
    public List<PowerRow> Powers { get; set; } = new();
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

/// <summary>What the game keeps of its powers: those in force. Plan 36 · TR7 adds the levels learned, the charge and the day's free one.</summary>
public sealed class PowersSave
{
    public List<SavedPower>? InForce { get; set; }
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
    /// The kinds something reads today (plan 39 · CR6): <see cref="PowerKind.Encounter"/> and <see cref="PowerKind.Stealth"/>
    /// by a step's odds (<c>Map.RollWildEncounter</c>), <see cref="PowerKind.TypeEncounter"/> by the grass's slots
    /// (<c>EncounterSlots.Grass</c>), <see cref="PowerKind.Hatch"/> by the Eggs' cycles (<see cref="Breeding.CountDown"/>)
    /// and <see cref="PowerKind.Materials"/> by what a wild Pokémon leaves (<see cref="MaterialDrops.For(Pokemon, Random, int)"/>).
    /// The others wait for plan 36 · TR7's readers in the battle (EXP, effort, money, catching) and the shops (prices), and
    /// nothing grants them before it.
    /// </summary>
    public static readonly IReadOnlySet<PowerKind> Read = new HashSet<PowerKind>
    {
        PowerKind.Encounter, PowerKind.Stealth, PowerKind.TypeEncounter, PowerKind.Hatch, PowerKind.Materials
    };

    /// <summary>What a kind comes to at a level (one to three); nought for level nought or a kind with no row.</summary>
    public static int Strength(PowerKind kind, int level) =>
        level < 1 || Row(kind) is not { } row || row.Strength.Count == 0 ? 0 : row.Strength[Math.Min(level, row.Strength.Count) - 1];

    /// <summary>The name a kind is shown by (<c>powers.kind.*</c>).</summary>
    public static string NameOf(PowerKind kind)
    {
        string key = kind.ToString();
        key = char.ToLowerInvariant(key[0]) + key[1..];
        return Text.Of($"powers.kind.{key}");
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

    public void Clear() => Load(null);

    public void Load(PowersSave? save)
    {
        inForce.Clear();
        foreach (var p in save?.InForce ?? new())
            if (p.Seconds > 0 && p.Level >= 1 && (p.Kind == PowerKind.TypeEncounter) == (p.Type != null))
                inForce[p.Kind] = new ActivePower(p.Kind, Math.Min(p.Level, PowerTable.TopLevel), p.Seconds, p.Type);
        Revision++;
    }

    /// <summary>The save's record; null while nothing is in force, so no version was raised.</summary>
    public PowersSave? ToSave() => inForce.Count == 0 ? null : new PowersSave
    {
        InForce = InForce.Select(p => new SavedPower { Kind = p.Kind, Level = p.Level, Seconds = p.SecondsLeft, Type = p.Type }).ToList()
    };
}
