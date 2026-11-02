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

    public override string Name => name;

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
    public static IReadOnlyList<Arrow> Arrows { get; } = MakeArrows(Loops);

    private static List<Arrow> MakeArrows((int X, int Y, Colour Colour)[] loops)
    {
        var arrows = new List<Arrow>();
        foreach (var (x, y, colour) in loops)
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

    /// <summary>
    /// The Trick House's seventh puzzle (plan 21 · H12, part j): Emerald's <c>initrotatingtilepuzzle TRUE</c>, the same
    /// rule on the puzzle's own tileset (<c>METATILE_TrickHousePuzzle_Arrow_YellowOnWhite_Right</c> and on), whose loops
    /// and switches are read from <c>LAYOUT_ROUTE110_TRICK_HOUSE_PUZZLE7</c>'s blocks: a yellow, a blue, a green and a
    /// purple loop moving its statues and trainers.
    /// </summary>
    public const string TrickHouseName = "TrickHouseTiles";

    private static readonly (int X, int Y, Colour Colour)[] TrickHouseLoops =
    {
        (8, 17, Colour.Yellow), (8, 20, Colour.Yellow), (3, 17, Colour.Blue), (3, 6, Colour.Green), (10, 2, Colour.Purple),
    };

    /// <summary>The Trick House's floor switches (the seventh puzzle's coordinate events).</summary>
    public static readonly (int X, int Y, Colour Colour)[] TrickHouseSwitches =
    {
        (8, 19, Colour.Yellow), (0, 14, Colour.Blue), (6, 6, Colour.Green), (9, 7, Colour.Purple),
    };

    private readonly string name = PuzzleName;
    private readonly List<Arrow> roomArrows;
    private readonly (int X, int Y, Colour Colour)[] roomSwitches;

    /// <summary>The Mossdeep Gym's floor.</summary>
    public MossdeepTiles() : this(PuzzleName) { }

    /// <summary>A floor of turning arrows by its room's name (<see cref="PuzzleName"/> or <see cref="TrickHouseName"/>).</summary>
    public MossdeepTiles(string room)
    {
        name = room;
        roomArrows = room == TrickHouseName ? MakeArrows(TrickHouseLoops) : (List<Arrow>)Arrows;
        roomSwitches = room == TrickHouseName ? TrickHouseSwitches : Switches;
    }

    /// <summary>This room's arrow tiles.</summary>
    public IReadOnlyList<Arrow> RoomArrows => roomArrows;

    /// <summary>This room's floor switches.</summary>
    public IReadOnlyList<(int X, int Y, Colour Colour)> RoomSwitches => roomSwitches;

    /// <summary>What a switch of a colour does in this room (<see cref="Turns"/>, on this room's arrows).</summary>
    public List<(NPC Who, Direction Step, Direction Facing)> TurnsHere(Colour colour, Func<int, int, NPC?> at)
    {
        var turns = new List<(NPC, Direction, Direction)>();
        foreach (var a in roomArrows)
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

    // Where each one moved stood and faced as the room was come into: the trainers and the statues keep still, so the
    // field's own laying out of those who wander doesn't put them back
    private readonly Dictionary<NPC, (int X, int Y, Direction Facing)> homes = new();

    /// <summary>Notes where someone stands before a switch first moves them, to put them back as the room is come into.</summary>
    public void Remember(NPC who) => homes.TryAdd(who, (who.GridX, who.GridY, who.Facing));

    /// <summary>The room is come into: everyone a switch moved stands and faces as at first, as Emerald reads its objects afresh.</summary>
    public override void Arrive(Map map, StoryState story, Random rng)
    {
        foreach (var (who, (x, y, facing)) in homes)
        {
            who.GridX = x;
            who.GridY = y;
            who.Facing = facing;
            who.StepOffsetX = who.StepOffsetY = 0f;
        }
        homes.Clear();
    }
}
