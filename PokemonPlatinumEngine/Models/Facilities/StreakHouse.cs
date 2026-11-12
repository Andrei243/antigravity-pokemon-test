using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>A host of a later battle place: the trainer of <c>Data/our-trainers.json</c> they battle as in a normal and in a hard room, and the look they wear.</summary>
public sealed record FacilityHost(string Name, string Look, string Normal, string Hard);

/// <summary>
/// A room of a later battle place as values (plan 37 · FC2; <c>docs/mechanics/rulings.md</c>, "The later games' battle
/// places"): how many Pokémon go in, the format, the level, the tiers of the later sets its trainers bring by the streak,
/// the IVs by the streak, the Battle Points a win earns, and the host who stands in the way at <see cref="HostAt"/>. Each
/// place builds its rooms (<see cref="StreakHouse.Room"/>); <see cref="StreakChallenge"/> plays any of them.
/// </summary>
public sealed class StreakRoom
{
    public required string Place { get; init; }
    public required string Way { get; init; }
    public bool Hard { get; init; }

    /// <summary>How many Pokémon each side brings.</summary>
    public required int Entered { get; init; }
    public required BattleFormat Format { get; init; }
    public int Level { get; init; } = Frontier.FrontierRules.Level;

    /// <summary>The battle (counted from 1 in the streak) at which the host comes; 0 for a room with none.</summary>
    public int HostAt { get; init; }

    /// <summary>The host's trainer of <c>Data/our-trainers.json</c> for this room, or null for none.</summary>
    public string? HostTrainer { get; init; }
    public FacilityHost? Host { get; init; }

    /// <summary>The tiers (1 the weakest, 4 the strongest) the trainer of the battle after <c>streak</c> wins brings sets of.</summary>
    public required Func<int, (int Lowest, int Highest)> Tiers { get; init; }

    /// <summary>Every IV of the trainers' Pokémon at the battle after <c>streak</c> wins.</summary>
    public required Func<int, int> Ivs { get; init; }

    /// <summary>The Battle Points the win that makes the streak <c>streak</c> earns (the host's win included).</summary>
    public required Func<int, int> Points { get; init; }

    /// <summary>The story variable the trophy of this hard room is kept in (1 once its host is first beaten); null for a normal room.</summary>
    public string? TrophyVariable { get; init; }

    /// <summary>
    /// A room played with a partner of the place's own beside the player and two trainers against them (plan 23, the Battle
    /// Tree's Multi room): <see cref="Entered"/> is how many go in for the player alone, the partner brings as many, and each
    /// of the two trainers too. Such a room has no host and no hard room.
    /// </summary>
    public bool Multi { get; init; }

    /// <summary>Only the later sets this lets come (plan 37 · FC7: the sparring room's type); null for any.</summary>
    public Func<Data.FrontierSetRecord, bool>? Only { get; init; }

    /// <summary>The type every Pokémon entered must be of (plan 37 · FC7: the sparring room); null for any.</summary>
    public Data.PokemonType? Type { get; init; }

    /// <summary>The key its record is kept under.</summary>
    public string Key => FacilityRecord.KeyOf(Place, Way, Hard);
}

/// <summary>
/// The streak house (plan 37 · FC2; after X and Y's Battle Maison, every person and word ours), which stands in Kiloude
/// City as plan 23 drew it (<c>KiloudeCity_BattleMaison</c> and its hall, opened by Kalos's Hall of Fame): four rooms of
/// single (three entered), double (four), triple (six) and rotation (four) battles, the original's counts, an endless
/// streak a battle at a time against trainers of the later sets (<see cref="FacilityTrainers"/>) at level 50; a host of
/// ours for each at the 20th battle, whose defeat opens the room's hard room, whose own host comes at its 50th and leaves
/// a trophy the first time. The numbers here are ours. No drawing or input.
/// </summary>
public static class StreakHouse
{
    /// <summary>The house's word in a script (<c>facility house ...</c>) and its records' keys.</summary>
    public const string Place = "house";

    /// <summary>The house's rooms, in the order the desk and the board give them.</summary>
    public static readonly string[] Ways = { "single", "double", "triple", "rotation" };

    /// <summary>X and Y's battles: the normal room's host at the 20th, the hard room's at the 50th.</summary>
    public const int HostAtNormal = 20, HostAtHard = 50;

    /// <summary>The four hosts of ours, one to a room, each with a normal and a hard team that Mega Evolves.</summary>
    public static readonly IReadOnlyDictionary<string, FacilityHost> Hosts = new Dictionary<string, FacilityHost>(StringComparer.Ordinal)
    {
        ["single"] = new("Corvin", "house_corvin", "house_host_corvin", "house_host_corvin_hard"),
        ["double"] = new("Isaura", "house_isaura", "house_host_isaura", "house_host_isaura_hard"),
        ["triple"] = new("Tamsin", "house_tamsin", "house_host_tamsin", "house_host_tamsin_hard"),
        ["rotation"] = new("Ludovic", "house_ludovic", "house_host_ludovic", "house_host_ludovic_hard")
    };

    /// <summary>How many each side brings in a room: the original's counts.</summary>
    public static int EnteredIn(string way) => way switch
    {
        "single" => 3,
        "triple" => 6,
        "double" or "rotation" => 4,
        _ => throw new ArgumentException($"The streak house has no {way} room", nameof(way))
    };

    public static BattleFormat FormatOf(string way) => way switch
    {
        "double" => BattleFormat.Double,
        "triple" => BattleFormat.Triple,
        "rotation" => BattleFormat.Rotation,
        _ => BattleFormat.Single
    };

    /// <summary>
    /// The tiers a trainer's sets are of by the streak (a table of ours, the Maison's first build's by its rounds of
    /// seven): the normal rooms climb from the weakest two to the strongest two by the 50th battle; the hard rooms begin
    /// a step up and reach the strongest alone at the 50th.
    /// </summary>
    public static (int Lowest, int Highest) TiersFor(int streak, bool hard)
    {
        if (hard)
            return streak switch
            {
                < 20 => (2, 4),
                < 49 => (3, 4),
                _ => (4, 4)
            };
        return (streak / 7) switch
        {
            0 => (1, 2),
            1 => (1, 3),
            2 => (2, 3),
            3 or 4 or 5 or 6 => (2, 4),
            _ => (3, 4)
        };
    }

    /// <summary>The IVs by the streak: <see cref="FacilityTrainers.IvsFor"/> by its seventh, three ranks higher in a hard room.</summary>
    public static int IvsFor(int streak, bool hard) => FacilityTrainers.IvsFor(streak / 7 + (hard ? 3 : 0));

    /// <summary>
    /// The Battle Points of a win that makes the streak <paramref name="streak"/> (a table of ours): one for each of the
    /// first ten, a point more each ten battles to five, one more in a hard room, and the host's win twenty (forty in a
    /// hard room) in place of the battle's own.
    /// </summary>
    public static int PointsFor(int streak, bool hard)
    {
        if (streak == (hard ? HostAtHard : HostAtNormal)) return hard ? 40 : 20;
        int points = Math.Min(5, 1 + (streak - 1) / 10);
        return points + (hard ? 1 : 0);
    }

    /// <summary>The story variable a hard room's trophy is kept in: <c>VAR_KALOS_HOUSE_TROPHY_SINGLE</c> and its three fellows.</summary>
    public static string TrophyVariable(string way) => "VAR_KALOS_HOUSE_TROPHY_" + way.ToUpperInvariant();

    /// <summary>A room of the house.</summary>
    public static StreakRoom Room(string way, bool hard)
    {
        var host = Hosts[way];
        return new StreakRoom
        {
            Place = Place, Way = way, Hard = hard, Entered = EnteredIn(way), Format = FormatOf(way),
            HostAt = hard ? HostAtHard : HostAtNormal, HostTrainer = hard ? host.Hard : host.Normal, Host = host,
            Tiers = streak => TiersFor(streak, hard), Ivs = streak => IvsFor(streak, hard), Points = streak => PointsFor(streak, hard),
            TrophyVariable = hard ? TrophyVariable(way) : null
        };
    }

    /// <summary>Every room, normal and hard, in the board's order.</summary>
    public static IEnumerable<StreakRoom> Rooms => Ways.SelectMany(w => new[] { Room(w, false), Room(w, true) });

    /// <summary>How many hard rooms' trophies the story holds.</summary>
    public static int Trophies(Story.StoryState story) => Ways.Count(w => story.Var(TrophyVariable(w)) > 0);
}
