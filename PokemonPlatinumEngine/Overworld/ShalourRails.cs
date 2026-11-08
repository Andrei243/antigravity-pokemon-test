using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Korrina's Gym in Shalour City (plan 23, Kalos's Gyms; docs/mechanics/rulings.md, "Kalos's Gyms, its water and the
/// Mamoswine ride"): X and Y's roller rink, as it is publicly described (rails and ramps skated to the Leader), in a form
/// of our own. The hall between the door's floor and Korrina's is a pit, and over it run rails of the floor broken by
/// gaps a tile wide; at each side of a gap a ramp ends its rail, and a step onto the gap is a hop over it to the rail
/// beyond when the ramp the step is taken from faces that way. A lever (on a rail, on the door's floor and on
/// Korrina's) throws the points: every ramp turns round, so every gap is hopped the other way. Laid out afresh as the
/// player comes in. The first gap is hopped north as the hall is laid out, the second only once the points are thrown,
/// and the third only once they are thrown back, so the way to Korrina throws them twice. GPU-free.
/// </summary>
public sealed class ShalourRails : GymPuzzle
{
    public const string PuzzleName = "ShalourRails";

    public override string Name => PuzzleName;

    /// <summary>A gap over the pit and the way it is hopped as the hall is laid out (the other way once the points are thrown).</summary>
    public readonly record struct Gap(int X, int Y, Direction Way);

    /// <summary>The door's floor to the first rail, the first rail to the second, the second to Korrina's floor.</summary>
    public static readonly Gap[] Gaps =
    {
        new(3, 13, Direction.Up),
        new(6, 10, Direction.Left),
        new(9, 6, Direction.Up),
    };

    /// <summary>The levers: one on each rail's stub, one on the door's floor and one on Korrina's.</summary>
    public static readonly (int X, int Y)[] Levers = { (2, 11), (10, 8), (10, 14), (10, 3) };

    /// <summary>Whether the points have been thrown (every ramp turned round) since the hall was laid out.</summary>
    public bool Thrown { get; set; }

    /// <summary>How many times the points have been thrown or laid out (the renderer follows it).</summary>
    public int Throws { get; private set; }

    public static int GapAt(int x, int y) => Array.FindIndex(Gaps, g => g.X == x && g.Y == y);

    public static bool IsLever(int x, int y) => Array.IndexOf(Levers, (x, y)) >= 0;

    /// <summary>The way a gap is hopped now.</summary>
    public Direction WayOver(int gap) => Thrown ? FieldMovement.Opposite(Gaps[gap].Way) : Gaps[gap].Way;

    /// <summary>The rails' tiles either side of a gap, where its two ramps stand: before it the way it is hopped as the hall is laid out, and beyond.</summary>
    public static ((int X, int Y) Before, (int X, int Y) Beyond) EndsOf(int gap)
    {
        var g = Gaps[gap];
        var (dx, dy) = FieldMovement.Delta(g.Way);
        return ((g.X - dx, g.Y - dy), (g.X + dx, g.Y + dy));
    }

    public void Reset()
    {
        Thrown = false;
        Throws++;
    }

    public void Throw()
    {
        Thrown = !Thrown;
        Throws++;
    }

    /// <summary>The rails are floor and the gaps the dark in the room's file; nothing of its grid changes.</summary>
    public override void Apply(Map map, StoryState story) { }

    public override void Arrive(Map map, StoryState story, Random rng) => Reset();

    /// <summary>A step onto a gap from the ramp that faces it is a hop over it.</summary>
    public override bool HopsOver(int x, int y, Direction dir) => GapAt(x, y) is int g and >= 0 && WayOver(g) == dir;

    /// <summary>Whether a gap is hopped that way in either state (the walk of the whole game, which throws no lever).</summary>
    public static bool MayHopOver(int x, int y, Direction dir) =>
        GapAt(x, y) is int g and >= 0 && (Gaps[g].Way == dir || FieldMovement.Opposite(Gaps[g].Way) == dir);

    /// <summary>A step onto a lever throws the points as it is taken.</summary>
    public override void Crossed(Map map, int x, int y, Direction dir)
    {
        if (IsLever(x, y)) Throw();
    }
}
