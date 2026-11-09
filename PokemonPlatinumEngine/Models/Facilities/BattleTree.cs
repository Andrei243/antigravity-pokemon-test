using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>
/// The Battle Tree at the head of Poni Gauntlet (plan 23, the Battle Tree's inside; plan 37 · FC2's frame; rulings,
/// "The Battle Tree, the power plant, Zygarde's cells and the rest of the islands"): Sun and Moon's two rooms of our own
/// that need no partner, single battles (three entered) and double battles (four), an endless streak a battle at a time
/// against trainers of the later sets (<see cref="FacilityTrainers"/>) at level 50. Sun and Moon's Battle Legends stand
/// in the way at the 20th battle and their Super rooms at the 50th; here two Tree Masters of ours, one to a room, whose
/// defeat at the 20th opens the room's Super room, where they come again at the 50th and leave a trophy the first time.
/// Sun and Moon's Multi room (a partner beside the player) is not built: the frame plays one trainer to a side. The
/// numbers are ours. No drawing or input.
/// </summary>
public static class BattleTree
{
    /// <summary>The tree's word in a script (<c>facility tree ...</c>) and its records' keys.</summary>
    public const string Place = "tree";

    /// <summary>The tree's rooms, in the order the desk and the board give them.</summary>
    public static readonly string[] Ways = { "single", "double" };

    /// <summary>Sun and Moon's battles: the normal room's master at the 20th, the Super room's at the 50th.</summary>
    public const int HostAtNormal = 20, HostAtSuper = 50;

    /// <summary>The two masters of ours, one to a room, each with a normal and a Super team of Alola's Z-Moves.</summary>
    public static readonly IReadOnlyDictionary<string, FacilityHost> Hosts = new Dictionary<string, FacilityHost>(StringComparer.Ordinal)
    {
        ["single"] = new("Kaimana", "tree_kaimana", "tree_master_kaimana", "tree_master_kaimana_super"),
        ["double"] = new("Leilani", "tree_leilani", "tree_master_leilani", "tree_master_leilani_super"),
    };

    /// <summary>How many each side brings in a room: Sun and Moon's counts.</summary>
    public static int EnteredIn(string way) => way switch
    {
        "single" => 3,
        "double" => 4,
        _ => throw new ArgumentException($"The Battle Tree has no {way} room", nameof(way))
    };

    public static BattleFormat FormatOf(string way) => way == "double" ? BattleFormat.Double : BattleFormat.Single;

    /// <summary>
    /// The tiers a trainer's sets are of by the streak (a table of ours): a normal room from the weakest two to the strongest
    /// two by its master's 20th battle; a Super room a step up from the first, the strongest alone from the 40th.
    /// </summary>
    public static (int Lowest, int Highest) TiersFor(int streak, bool super)
    {
        if (super)
            return streak switch
            {
                < 20 => (2, 4),
                < 40 => (3, 4),
                _ => (4, 4)
            };
        return streak switch
        {
            < 5 => (1, 2),
            < 10 => (1, 3),
            < 15 => (2, 3),
            _ => (2, 4)
        };
    }

    /// <summary>The IVs by the streak: <see cref="FacilityTrainers.IvsFor"/> by its tenth, three ranks higher in a Super room.</summary>
    public static int IvsFor(int streak, bool super) => FacilityTrainers.IvsFor(streak / 10 + (super ? 3 : 0));

    /// <summary>
    /// The Battle Points of a win that makes the streak <paramref name="streak"/> (a table of ours, near Sun and Moon's):
    /// two for each of the first ten, three to the 20th, four after, one more in a Super room, and the master's win twenty
    /// (forty in a Super room) in place of the battle's own.
    /// </summary>
    public static int PointsFor(int streak, bool super)
    {
        if (streak == (super ? HostAtSuper : HostAtNormal)) return super ? 40 : 20;
        int points = streak <= 10 ? 2 : streak <= 20 ? 3 : 4;
        return points + (super ? 1 : 0);
    }

    /// <summary>The story variable a Super room's trophy is kept in: <c>VAR_ALOLA_TREE_TROPHY_SINGLE</c> and its fellow.</summary>
    public static string TrophyVariable(string way) => "VAR_ALOLA_TREE_TROPHY_" + way.ToUpperInvariant();

    /// <summary>A room of the tree.</summary>
    public static StreakRoom Room(string way, bool super)
    {
        var host = Hosts[way];
        return new StreakRoom
        {
            Place = Place, Way = way, Hard = super, Entered = EnteredIn(way), Format = FormatOf(way),
            HostAt = super ? HostAtSuper : HostAtNormal, HostTrainer = super ? host.Hard : host.Normal, Host = host,
            Tiers = streak => TiersFor(streak, super), Ivs = streak => IvsFor(streak, super), Points = streak => PointsFor(streak, super),
            TrophyVariable = super ? TrophyVariable(way) : null
        };
    }

    /// <summary>Every room, normal and Super, in the board's order.</summary>
    public static IEnumerable<StreakRoom> Rooms => Ways.SelectMany(w => new[] { Room(w, false), Room(w, true) });

    /// <summary>How many Super rooms' trophies the story holds.</summary>
    public static int Trophies(Story.StoryState story) => Ways.Count(w => story.Var(TrophyVariable(w)) > 0);
}
