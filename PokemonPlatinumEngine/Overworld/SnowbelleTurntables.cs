using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Wulfric's Gym in Snowbelle City (plan 23, Kalos's Gyms; docs/mechanics/rulings.md, "Kalos's Gyms, its water and the
/// Mamoswine ride"): X and Y's hall of turning platforms, as it is publicly described (switches turn platforms so that
/// a way opens across the ice to the Leader), in a form of our own. Six turntables of ice stand over the dark between
/// the door's platform and Wulfric's, each a hub with two arms at a right angle, an L. Whoever steps onto a hub turns
/// it a quarter clockwise as seen from above (the hub is the switch), and the arms that lie where a neighbour's arm
/// meets them are the only way over the dark. An arm is one tile; two hubs three tiles apart are joined when both
/// point their arms at each other, and an arm that points at the door's platform or at the walkway under Wulfric's
/// meets them. Laid out afresh as the player comes in, as X and Y's room is. GPU-free.
/// </summary>
public sealed class SnowbelleTurntables : GymPuzzle
{
    public const string PuzzleName = "SnowbelleTurntables";

    public override string Name => PuzzleName;

    /// <summary>A turntable: its hub's tile and the state it is laid out in as the player comes in (nought to three).</summary>
    public readonly record struct Table(int X, int Y, int Start);

    /// <summary>
    /// The hall's five turntables: two over the door's platform (west and east), one above each of those, and one in the
    /// middle under the walkway to Wulfric's platform, which only the two beside it reach. Laid out so that the way is
    /// shut until hubs are turned: the shortest way across turns the east ones and the middle one twice
    /// (<c>SnowbelleGymTests</c> searches it).
    /// </summary>
    public static readonly Table[] Tables =
    {
        new(3, 12, 0), new(9, 12, 1),
        new(3, 9, 3), new(9, 9, 1),
        new(6, 9, 1),
    };

    /// <summary>The four ways an arm can point, as quarter turns from north clockwise: north, east, south, west.</summary>
    public static (int DX, int DY) Way(int quarter) => (quarter & 3) switch
    {
        0 => (0, -1),
        1 => (1, 0),
        2 => (0, 1),
        _ => (-1, 0)
    };

    /// <summary>The ways a turntable's arms point in a state: an L, the state's own way and the next one clockwise.</summary>
    public static IEnumerable<int> ArmsIn(int state) => new[] { state & 3, (state + 1) & 3 };

    private readonly int[] states = new int[Tables.Length];

    /// <summary>The state each turntable is in now, nought to three: its arms point <see cref="ArmsIn"/>.</summary>
    public IReadOnlyList<int> States => states;

    /// <summary>How many times the turntables have been turned since the player came in (the renderer eases toward it).</summary>
    public int Turns { get; private set; }

    /// <summary>Puts every turntable as the hall lays them out (<see cref="Tables"/>' starts).</summary>
    public void Reset()
    {
        for (int i = 0; i < Tables.Length; i++) states[i] = Tables[i].Start;
        Turns++;
    }

    public SnowbelleTurntables() => Reset();

    /// <summary>The turntable whose hub is a tile; -1 for none.</summary>
    public static int HubAt(int x, int y) => Array.FindIndex(Tables, t => t.X == x && t.Y == y);

    /// <summary>Whether a tile is a turntable's hub or one of its four arm tiles: the tiles the puzzle decides about.</summary>
    public static bool Covers(int x, int y) =>
        Tables.Any(t => (t.X == x && t.Y == y) || Math.Abs(t.X - x) + Math.Abs(t.Y - y) == 1);

    /// <summary>Whether a tile is open now: a hub always, an arm tile while its turntable points an arm at it.</summary>
    public bool Open(int x, int y)
    {
        for (int i = 0; i < Tables.Length; i++)
        {
            var t = Tables[i];
            if (t.X == x && t.Y == y) return true;
            foreach (int a in ArmsIn(states[i]))
            {
                var (dx, dy) = Way(a);
                if (t.X + dx == x && t.Y + dy == y) return true;
            }
        }
        return false;
    }

    /// <summary>Puts every turntable in a state of its own (a test's search of the hall).</summary>
    public void Set(IReadOnlyList<int> to)
    {
        for (int i = 0; i < Tables.Length; i++) states[i] = to[i] & 3;
        Turns++;
    }

    /// <summary>Turns a turntable a quarter clockwise (what a step onto its hub does).</summary>
    public void Turn(int table)
    {
        states[table] = (states[table] + 1) & 3;
        Turns++;
    }

    /// <summary>Nothing of the room's own grid changes: the tiles the turntables decide about are open in the room's file, and closed by <see cref="Refuses"/>.</summary>
    public override void Apply(Map map, StoryState story) { }

    public override void Arrive(Map map, StoryState story, Random rng) => Reset();

    /// <summary>An arm tile no arm lies on now is the dark, for anyone stepping onto it (as the Sunyshore Gym's gears refuse theirs).</summary>
    public override bool Refuses(Map map, int x, int y, float from, bool afloat) => Covers(x, y) && !Open(x, y);

    /// <summary>A step onto a hub turns its turntable as the step is taken.</summary>
    public override void Crossed(Map map, int x, int y, Direction dir)
    {
        int hub = HubAt(x, y);
        if (hub >= 0) Turn(hub);
    }
}
