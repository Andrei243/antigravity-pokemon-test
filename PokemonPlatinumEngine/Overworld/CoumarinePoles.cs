using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Ramos's Gym in Coumarine City (plan 23, Kalos's Gyms; docs/mechanics/rulings.md, "Kalos's Gyms, its water and the
/// Mamoswine ride"): X and Y's giant vine tree, as it is publicly described (a tree climbed by its poles to the Leader),
/// in a form of our own. The hall rises in three tiers, at the floor, two tiles up and four (the map file's heights),
/// with a face between each; a pole stands on a tile of the face, and a step onto it is a hop over it to the tier beyond:
/// a climbing pole is gone up (north) only, a sliding pole down (south) only. Ramos's platform on the top tier is
/// parted from the tier's west end by a hedge and is reached by one pole, which hangs short of the tier below until a
/// switch of vine at the top tier's west end lets it down; it stays down while the player is in the room. Laid out
/// afresh (hung up) as the player comes in. So the way to Ramos climbs to the west end, pulls the vine, slides back down
/// and climbs the pole it let down; sliding poles lead from every tier back down to the door. GPU-free.
/// </summary>
public sealed class CoumarinePoles : GymPuzzle
{
    public const string PuzzleName = "CoumarinePoles";

    public override string Name => PuzzleName;

    public enum Kind { Climb, Slide, Hanging }

    /// <summary>A pole: the tile of the face it stands on, and what it is.</summary>
    public readonly record struct Pole(int X, int Y, Kind Kind);

    /// <summary>The poles: two on the lower face (row 12) and four on the upper (row 7), the hanging one among them.</summary>
    public static readonly Pole[] Poles =
    {
        new(2, 12, Kind.Climb), new(10, 12, Kind.Slide),
        new(2, 7, Kind.Climb), new(3, 7, Kind.Slide), new(7, 7, Kind.Hanging), new(10, 7, Kind.Slide),
    };

    /// <summary>The switch of vine that lets the hanging pole down, at the top tier's west end.</summary>
    public static readonly (int X, int Y) Switch = (1, 4);

    /// <summary>The heights of the tiers, from the door's up.</summary>
    public static readonly float[] Tiers = { 0f, 2f, 4f };

    /// <summary>Whether the hanging pole has been let down.</summary>
    public bool Down { get; set; }

    /// <summary>How many times the pole has been let down or hung up again (the renderer follows it).</summary>
    public int Changes { get; private set; }

    public static int PoleAt(int x, int y) => Array.FindIndex(Poles, p => p.X == x && p.Y == y);

    public void Reset()
    {
        Down = false;
        Changes++;
    }

    /// <summary>Lets the hanging pole down (once: pulling the vine again does nothing more).</summary>
    public void LetDown()
    {
        if (Down) return;
        Down = true;
        Changes++;
    }

    /// <summary>The tiers are the room's own heights and the faces solid in its file; nothing of its grid changes.</summary>
    public override void Apply(Map map, StoryState story) { }

    public override void Arrive(Map map, StoryState story, Random rng) => Reset();

    /// <summary>A pole is a hop over its face: a climbing one north, a sliding one south, the hanging one north once it is down.</summary>
    public override bool HopsOver(int x, int y, Direction dir)
    {
        int i = PoleAt(x, y);
        if (i < 0) return false;
        return Poles[i].Kind switch
        {
            Kind.Climb => dir == Direction.Up,
            Kind.Slide => dir == Direction.Down,
            _ => Down && dir == Direction.Up
        };
    }

    /// <summary>Whether a pole is ever gone over that way (the walk of the whole game, which pulls no vine).</summary>
    public static bool MayHopOver(int x, int y, Direction dir)
    {
        int i = PoleAt(x, y);
        return i >= 0 && dir == (Poles[i].Kind == Kind.Slide ? Direction.Down : Direction.Up);
    }

    /// <summary>A step onto the switch lets the hanging pole down.</summary>
    public override void Crossed(Map map, int x, int y, Direction dir)
    {
        if ((x, y) == Switch) LetDown();
    }
}
