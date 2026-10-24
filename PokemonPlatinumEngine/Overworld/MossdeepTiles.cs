using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Mossdeep Gym's floor of turning arrows (plan 21 · H8; Emerald's <c>src/rotating_tile_puzzle.c</c>, its Mossdeep
/// puzzle): loops of four arrow tiles in five colours, each loop's arrows pointing round it anticlockwise as seen from
/// above (the north-west tile down, the south-west right, the south-east up, the north-east left). A floor switch of a
/// colour (the room's own triggers, whose scripts say <c>rotatetiles</c>) moves everyone standing on that colour's
/// arrows one step the way their arrow points, trainers and stone statues alike, and turns each of them a quarter
/// anticlockwise (<c>TurnRotatingTileObjects</c>: east to north, south to east, west to south, north to west), so the
/// statues stop or free a corridor and a trainer looks another way. Where they stand lasts while the player stays: the
/// original moves the map's copies of its objects, which it reads again from the map whenever it is come to, and the
/// field puts everyone back where they first stood as the room is come into. GPU-free: the hosts ask
/// <see cref="Turns"/> who moves where, and the room's pieces draw the arrows from <see cref="Arrows"/>.
/// </summary>
public sealed class MossdeepTiles : GymPuzzle
{
    public const string PuzzleName = "MossdeepTiles";

    public override string Name => PuzzleName;

    /// <summary>The five colours, in the original's order of its arrows' metatiles (<c>METATILE_MossdeepGym_YellowArrow_Right</c> and on).</summary>
    public enum Colour { Yellow, Blue, Green, Purple, Red }

    /// <summary>One arrow tile: where it is, its colour and the way it points.</summary>
    public readonly record struct Arrow(int X, int Y, Colour Colour, Direction Way);

    /// <summary>The floor switches, each on its tile, by colour: the room's coordinate events.</summary>
    public static readonly (int X, int Y, Colour Colour)[] Switches =
    {
        (2, 21, Colour.Yellow), (3, 30, Colour.Yellow), (8, 10, Colour.Blue), (6, 7, Colour.Blue),
        (15, 34, Colour.Green), (23, 24, Colour.Purple), (23, 21, Colour.Purple), (8, 6, Colour.Red),
    };

    /// <summary>The north-west tile of each loop of four arrows, by colour, as the room's layout has them.</summary>
    private static readonly (int X, int Y, Colour Colour)[] Loops =
    {
        (6, 5, Colour.Red), (11, 5, Colour.Red),
        (6, 8, Colour.Blue), (9, 8, Colour.Blue),
        (4, 20, Colour.Yellow), (1, 29, Colour.Yellow), (4, 29, Colour.Yellow),
        (21, 22, Colour.Purple), (24, 22, Colour.Purple), (22, 25, Colour.Purple),
        (11, 33, Colour.Green),
    };

    /// <summary>Every arrow tile of the room.</summary>
    public static IReadOnlyList<Arrow> Arrows { get; } = MakeArrows();

    private static List<Arrow> MakeArrows()
    {
        var arrows = new List<Arrow>();
        foreach (var (x, y, colour) in Loops)
        {
            arrows.Add(new Arrow(x, y, colour, Direction.Down));
            arrows.Add(new Arrow(x + 1, y, colour, Direction.Left));
            arrows.Add(new Arrow(x, y + 1, colour, Direction.Right));
            arrows.Add(new Arrow(x + 1, y + 1, colour, Direction.Up));
        }
        return arrows;
    }

    /// <summary>The arrow on a tile, if there is one.</summary>
    public static Arrow? ArrowAt(int x, int y)
    {
        foreach (var a in Arrows)
            if (a.X == x && a.Y == y) return a;
        return null;
    }

    /// <summary>A quarter turn anticlockwise as seen from above (the original's <c>ROTATE_COUNTERCLOCKWISE</c>).</summary>
    public static Direction Anticlockwise(Direction facing) => facing switch
    {
        Direction.Right => Direction.Up,
        Direction.Down => Direction.Right,
        Direction.Left => Direction.Down,
        _ => Direction.Left
    };

    /// <summary>
    /// What a switch of a colour does (<c>MoveRotatingTileObjects</c>, then <c>TurnRotatingTileObjects</c>): everyone
    /// standing on an arrow of that colour, as <paramref name="at"/> finds them, with the way they step and the way
    /// they face once turned. All of them step at once, so a loop's people go round it together.
    /// </summary>
    public static List<(NPC Who, Direction Step, Direction Facing)> Turns(Colour colour, Func<int, int, NPC?> at)
    {
        var turns = new List<(NPC, Direction, Direction)>();
        foreach (var a in Arrows)
        {
            if (a.Colour != colour || at(a.X, a.Y) is not { } who) continue;
            turns.Add((who, a.Way, Anticlockwise(who.Facing)));
        }
        return turns;
    }

    /// <summary>A colour by its number in a script (0 yellow to 4 red, the original's puzzle numbers).</summary>
    public static Colour ColourOf(int number) => (Colour)Math.Clamp(number, 0, 4);

    // Nothing closes or opens: the statues and the trainers are the walls that move
    public override void Apply(Map map, StoryState story) { }
}
