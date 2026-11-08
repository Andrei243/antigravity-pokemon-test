using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Grant's Gym in Cyllage City (plan 23, Kalos's Gyms; docs/mechanics/rulings.md, "Kalos's Gyms, its water and the
/// Mamoswine ride"): X and Y's climbing wall, as it is publicly described (a wall climbed by its grips to the Leader at
/// the top), in a form of our own. A wall runs across the hall in two pitches, a ledge of floor between them: on a
/// pitch's tiles a climber stands only on a grip (bare rock refuses the step), and climbs up and sideways, never down (a
/// step south from one tile of the wall onto another is barred; stepping off the foot of a pitch onto the floor below is
/// not). At each side a rope (a column of plain floor) lets the climber down, and only down. Every grip leads on, up to
/// the next floor or sideways to a rope, so nobody is left on the wall. Nothing moves: the wall is the same however it
/// is come to. GPU-free.
/// </summary>
public sealed class CyllageWall : GymPuzzle
{
    public const string PuzzleName = "CyllageWall";

    public override string Name => PuzzleName;

    /// <summary>The first row of the wall and the layout from there down: <c>R</c> a rope, <c>G</c> a grip, <c>x</c> bare rock, <c>.</c> floor.</summary>
    public const int Top = 6;

    /// <summary>
    /// Rows <see cref="Top"/> to 13, columns 0 to 12: the upper pitch (rows 6 to 8), the ledge (9 and 10) and the lower
    /// pitch (11 to 13), the ropes in columns 1 and 11, the wall's tiles in columns 2 to 10.
    /// </summary>
    public static readonly string[] Layout =
    {
        "#RxxxxGxxxxR#",   // 6: one grip under Grant's floor, before him
        "#RGGGGGxGGGR#",   // 7: west of the gap the way on and the way to the west rope, east of it the way to the east rope
        "#RxxGxxxGxxR#",   // 8: the pitch's two feet
        "#...........#",   // 9: the ledge
        "#...........#",   // 10
        "#RxxxxxxGxxR#",   // 11: the lower pitch's one way up onto the ledge
        "#RGxxxGGGxGR#",   // 12
        "#RGxxxGxxxGR#",   // 13: its three feet: the west and east ones lead to the ropes, the middle one on up
    };

    private static char At(int x, int y) =>
        y >= Top && y < Top + Layout.Length && x >= 0 && x < Layout[y - Top].Length ? Layout[y - Top][x] : '.';

    /// <summary>Whether a tile is one of the wall's (a grip or bare rock).</summary>
    public static bool IsWall(int x, int y) => At(x, y) is 'G' or 'x';

    public static bool IsGrip(int x, int y) => At(x, y) == 'G';

    public static bool IsRope(int x, int y) => At(x, y) == 'R';

    /// <summary>The wall's tiles are open in the room's file; bare rock is closed by <see cref="Refuses"/>.</summary>
    public override void Apply(Map map, StoryState story) { }

    /// <summary>Bare rock has nothing to hold: only a grip is stood on.</summary>
    public override bool Refuses(Map map, int x, int y, float from, bool afloat) => IsWall(x, y) && !IsGrip(x, y);

    /// <summary>
    /// A climber goes up and sideways: a step south from the wall onto the wall is barred, and a rope is gone down,
    /// never up (a step north onto a rope's tile is barred).
    /// </summary>
    public override bool Bars(Map map, int x, int y, Direction dir) => dir switch
    {
        Direction.Down => IsWall(x, y) && IsWall(x, y - 1),
        Direction.Up => IsRope(x, y),
        _ => false
    };
}
