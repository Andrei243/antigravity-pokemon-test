using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Sunyshore Gym's gears (plan 01 · M9, part 2c), ported from the original's <c>gym_features.c</c>. The Gym is three
/// rooms, each with gears whose arms are walkways over the dark: a gear turns on its hub, one an L of two arms, one a T of
/// three, one a dead end of one (<c>sSunyshoreRoom1Gears</c> to <c>sSunyshoreRoom3Gears</c>), and a gear that stands on
/// edge carries a bar five tiles long that is a bridge while it lies flat and a wall while it stands up. Every gear of a
/// room turns together, each its own way, and the room is in one of four states (<c>rotationState</c>), a quarter turn
/// apart. Which arms are there in each state is the original's own table of the regions each state closes
/// (<c>sSunyshoreCollisionLists</c>), and a step onto a closed one is refused
/// (<c>SunyshoreGym_DynamicMapFeaturesCheckCollision</c>): it is the player's alone, as the original's is, since it asks
/// the walker's step and no one else's.
/// <para>
/// A button on a gear's hub turns them all (<c>SunyshoreGym_PressButton</c>, the room's coordinate events running
/// <c>PressSunyshoreGymButton</c>): on a quarter, back a quarter, or a half turn. The state changes as the button is
/// pressed, and the gears turn to it at the original's 5.625 degrees a frame at thirty frames a second
/// (<c>FieldTask_SunyshoreGym_RotateGears</c>), the field waiting. Nobody is carried: the button is on the hub, which turns
/// in place. As the player comes in by a room's door from the room before, its gears are laid out in their first state;
/// coming back into it from the room beyond, in the state that leads back (<c>PersistedMapFeatures_InitForSunyshoreGym</c>,
/// by the room's own script).
/// </para>
/// </summary>
public sealed class SunyshoreGears : GymPuzzle
{
    public const string Room1Name = "SunyshoreGears1", Room2Name = "SunyshoreGears2", Room3Name = "SunyshoreGears3";

    public override string Name => Room switch { 0 => Room1Name, 1 => Room2Name, _ => Room3Name };

    /// <summary>The room, from nought (the entrance, matrix 226) to two (Volkner's, matrix 228).</summary>
    public int Room { get; }

    /// <summary>The original's gear models: the alternatives are the same walkways in another colour.</summary>
    public enum Shape { L, LAlt, T, TAlt, DeadEnd, Vertical, VerticalAlt }

    /// <summary>
    /// The original's three buttons (<c>sunyshore_gym_buttons.h</c>): a quarter turn on (<c>SUNYSHORE_GYM_BUTTON_NORMAL</c>),
    /// a quarter back (<c>REVERSE</c>) and a half turn on (<c>DOUBLE</c>).
    /// </summary>
    public enum Button { Normal, Reverse, Double }

    /// <summary>
    /// A gear as the original sets it up (<c>SunyshoreGymGearSetup</c>): its hub's tile and height (the original's y, in
    /// tiles), its turn in the first state in quarters (<c>initialRotation</c>), and which way it turns. A flat gear turns
    /// on the vertical through its hub, counter-clockwise as seen from above unless it turns clockwise; a gear on edge on
    /// the east-west line through its hub.
    /// </summary>
    public sealed record Gear(Shape Shape, int X, int Height, int Y, int InitialTurn, bool Clockwise)
    {
        public bool OnEdge => Shape is Shape.Vertical or Shape.VerticalAlt;

        /// <summary>The height its walkway is walked at: the original's plate under it, a tile over the gear's own height.</summary>
        public int Walkway => Height + 1;

        /// <summary>The gear's turn in a state, in quarters (<c>SunyshoreGym_SetGearRotation</c>), nought to three.</summary>
        public int TurnIn(int state) => ((InitialTurn + (Clockwise ? -state : state)) % 4 + 4) % 4;
    }

    /// <summary>A rectangle of tiles a state can close (<c>SunyshoreGymCollisionRegion</c>).</summary>
    public readonly record struct Region(int X, int Y, int Width, int Depth)
    {
        public bool Contains(int x, int y) => x >= X && x < X + Width && y >= Y && y < Y + Depth;
    }

    /// <summary>The original's gears, a room each (<c>sSunyshoreRoomGears</c>).</summary>
    public static readonly Gear[][] Gears =
    {
        new Gear[]
        {
            new(Shape.L, 3, 0, 8, 1, false),
            new(Shape.LAlt, 8, 0, 8, 2, true),
            new(Shape.L, 13, 0, 8, 0, false)
        },
        new Gear[]
        {
            new(Shape.L, 6, 0, 8, 1, false),
            new(Shape.DeadEnd, 11, 0, 8, 3, true),
            new(Shape.Vertical, 15, 3, 8, 1, true),
            new(Shape.Vertical, 2, 3, 13, 1, false),
            new(Shape.DeadEnd, 6, 0, 13, 2, true),
            new(Shape.T, 11, 0, 13, 2, false)
        },
        new Gear[]
        {
            new(Shape.T, 6, 6, 8, 1, false),
            new(Shape.LAlt, 11, 6, 8, 0, true),
            new(Shape.TAlt, 16, 6, 8, 0, false),
            new(Shape.Vertical, 2, 3, 13, 0, true),
            new(Shape.LAlt, 6, 6, 13, 1, true),
            new(Shape.T, 11, 6, 13, 3, false),
            new(Shape.TAlt, 16, 6, 13, 3, true),
            new(Shape.Vertical, 20, 3, 13, 1, false),
            new(Shape.VerticalAlt, 2, 3, 18, 0, false),
            new(Shape.L, 6, 0, 18, 1, true),
            new(Shape.TAlt, 11, 0, 18, 3, false),
            new(Shape.T, 16, 0, 18, 0, true),
            new(Shape.VerticalAlt, 20, 3, 18, 1, true)
        }
    };

    /// <summary>The regions a state can close, a room each (<c>sSunyshoreRoom1CollisionRegions</c> to <c>3</c>).</summary>
    public static readonly Region[][] Regions =
    {
        new Region[]
        {
            new(1, 8, 2, 1), new(3, 6, 1, 2), new(4, 8, 2, 1), new(3, 9, 1, 2),
            new(6, 8, 2, 1), new(8, 6, 1, 2), new(9, 8, 2, 1), new(8, 9, 1, 2),
            new(11, 8, 2, 1), new(13, 6, 1, 2), new(14, 8, 2, 1), new(13, 9, 1, 2)
        },
        new Region[]
        {
            new(4, 8, 2, 1), new(6, 6, 1, 2), new(7, 8, 2, 1), new(6, 9, 1, 2),
            new(9, 8, 2, 1), new(11, 6, 1, 2), new(12, 8, 2, 1), new(11, 9, 1, 2),
            new(15, 6, 1, 5), new(2, 11, 1, 5),
            new(4, 13, 2, 1), new(6, 11, 1, 2), new(7, 13, 2, 1), new(6, 14, 1, 2),
            new(9, 13, 2, 1), new(11, 11, 1, 2), new(12, 13, 2, 1), new(11, 14, 1, 2)
        },
        new Region[]
        {
            new(4, 8, 2, 1), new(6, 6, 1, 2), new(7, 8, 2, 1), new(6, 9, 1, 2),
            new(9, 8, 2, 1), new(11, 6, 1, 2), new(12, 8, 2, 1), new(11, 9, 1, 2),
            new(14, 8, 2, 1), new(16, 6, 1, 2), new(17, 8, 2, 1), new(16, 9, 1, 2),
            new(2, 11, 1, 5),
            new(4, 13, 2, 1), new(6, 11, 1, 2), new(7, 13, 2, 1), new(6, 14, 1, 2),
            new(9, 13, 2, 1), new(11, 11, 1, 2), new(12, 13, 2, 1), new(11, 14, 1, 2),
            new(14, 13, 2, 1), new(16, 11, 1, 2), new(17, 13, 2, 1), new(16, 14, 1, 2),
            new(20, 11, 1, 5),
            new(2, 16, 1, 5),
            new(4, 18, 2, 1), new(6, 16, 1, 2), new(7, 18, 2, 1), new(6, 19, 1, 2),
            new(9, 18, 2, 1), new(11, 16, 1, 2), new(12, 18, 2, 1), new(11, 19, 1, 2),
            new(14, 18, 2, 1), new(16, 16, 1, 2), new(17, 18, 2, 1), new(16, 19, 1, 2),
            new(20, 16, 1, 5)
        }
    };

    /// <summary>
    /// The regions each state closes, a room each (<c>sSunyshoreCollisionLists</c>), as the original lists them: the
    /// second room's lists for the states a quarter turn from the first end in a repeat of their first region, which the
    /// original's count of eleven reads and which closes nothing more.
    /// </summary>
    public static readonly int[][][] Closing =
    {
        new[]
        {
            new[] { 1, 2, 4, 5, 10, 11 },
            new[] { 0, 1, 5, 6, 9, 10 },
            new[] { 0, 3, 6, 7, 8, 9 },
            new[] { 2, 3, 4, 7, 8, 11 }
        },
        new[]
        {
            new[] { 1, 2, 4, 5, 7, 8, 9, 10, 11, 12, 15 },
            new[] { 0, 1, 4, 5, 6, 11, 12, 13, 14, 0, 0 },
            new[] { 0, 3, 5, 6, 7, 8, 9, 10, 12, 13, 17 },
            new[] { 2, 3, 4, 6, 7, 10, 11, 13, 16, 2, 2 }
        },
        new[]
        {
            new[] { 2, 6, 7, 11, 14, 15, 17, 21, 25, 28, 29, 31, 38, 39 },
            new[] { 1, 4, 7, 10, 12, 15, 16, 20, 22, 26, 29, 30, 34, 35 },
            new[] { 0, 4, 5, 9, 13, 16, 19, 23, 25, 27, 30, 33, 36, 39 },
            new[] { 3, 5, 6, 8, 12, 13, 14, 18, 24, 26, 27, 28, 32, 37 }
        }
    };

    /// <summary>
    /// The buttons, a room each: the room's coordinate events, on the hubs of the gears, and the kind each one presses
    /// (the scripts they run: <c>SunyshoreGymRoom1_Button</c>, <c>Room2_BottomButton</c> and <c>_TopButtons</c>,
    /// <c>Room3_TopButtons</c> and <c>_BottomButtons</c>).
    /// </summary>
    public static readonly (int X, int Y, Button Kind)[][] Buttons =
    {
        new[] { (3, 8, Button.Normal), (13, 8, Button.Normal) },
        new[] { (6, 13, Button.Normal), (6, 8, Button.Reverse), (11, 8, Button.Reverse) },
        new[] { (6, 8, Button.Normal), (16, 13, Button.Normal), (6, 18, Button.Double), (16, 18, Button.Double) }
    };

    /// <summary>
    /// The row of each room's door from the room before (the city's for the first), and the state its gears are laid out
    /// in when the player comes in anywhere else, from the room beyond (<c>PersistedMapFeatures_InitForSunyshoreGym</c>:
    /// <c>entranceZ</c> 14, 21 and 25; states 2, 1 and 0).
    /// </summary>
    public static readonly int[] EntranceRow = { 14, 21, 25 }, BackState = { 2, 1, 0 };

    /// <summary>The number of states, a quarter turn apart (<c>SUNYSHORE_NUM_ROTATION_STATES</c>).</summary>
    public const int States = 4;

    /// <summary>How fast the gears turn: 5.625 degrees a frame (<c>SUNYSHORE_ROTATION_STEP</c>) at thirty frames a second.</summary>
    public const float DegreesPerSecond = 5.625f * 30f;

    public SunyshoreGears(int room)
    {
        if (room is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(room), room, "The Sunyshore Gym has three rooms.");
        Room = room;
    }

    /// <summary>The state the room's gears are in (the original's persisted <c>rotationState</c>).</summary>
    public int State { get; private set; }

    /// <summary>The gears turning to the state of the button pressed last, or null.</summary>
    public Turn? Turning { get; private set; }

    /// <summary>The gears of this room.</summary>
    public IReadOnlyList<Gear> RoomGears => Gears[Room];

    /// <summary>The gears turning from one state to the next, as the field shows it.</summary>
    public sealed class Turn
    {
        public int From { get; }
        public Button Kind { get; }

        /// <summary>How far they turn: a quarter or a half (<c>rotationGoal</c>), in degrees.</summary>
        public float Goal => Kind == Button.Double ? 180f : 90f;

        /// <summary>How far they have turned (<c>rotationProgress</c>), in degrees.</summary>
        public float Progress { get; private set; }

        public float Duration => Goal / DegreesPerSecond;
        public bool Done => Progress >= Goal;

        public Turn(int from, Button kind)
        {
            From = from;
            Kind = kind;
        }

        public void Update(float dt) => Progress = MathF.Min(Goal, Progress + dt * DegreesPerSecond);
    }

    /// <summary>Puts the gears in a state at once (a test, the harness).</summary>
    public void Set(int state)
    {
        State = ((state % States) + States) % States;
        Turning = null;
    }

    /// <summary>
    /// A button is stepped on (<c>SunyshoreGym_PressButton</c>): the state moves on a quarter, back a quarter, or on a
    /// half, at once, and the gears set off to turn to it.
    /// </summary>
    public Turn Press(Button kind)
    {
        int from = State;
        State = After(State, kind);
        Turning = new Turn(from, kind);
        return Turning;
    }

    /// <summary>The state a button leaves the gears in: a quarter on, a quarter back (from nought, to three), a half on.</summary>
    public static int After(int state, Button kind) => (state + kind switch { Button.Normal => 1, Button.Reverse => States - 1, _ => 2 }) % States;

    /// <summary>Turns the gears on; the turn is over once they have come to the new state.</summary>
    public void Update(float dt)
    {
        if (Turning == null) return;
        Turning.Update(dt);
        if (Turning.Done) Turning = null;
    }

    /// <summary>Takes a turn to its end at once (a test, the harness, a game with no screen).</summary>
    public void Finish() => Turning = null;

    /// <summary>The kind of button on a tile of this room, or null where there is none.</summary>
    public Button? ButtonAt(int x, int y)
    {
        foreach (var (bx, by, kind) in Buttons[Room])
            if ((bx, by) == (x, y)) return kind;
        return null;
    }

    /// <summary>Whether a state closes a tile of a room: the original's regions, as its list for the state names them.</summary>
    public static bool Closes(int room, int state, int x, int y)
    {
        foreach (int region in Closing[room][state])
            if (Regions[room][region].Contains(x, y)) return true;
        return false;
    }

    /// <summary>Whether the gears close a tile now.</summary>
    public bool Closed(int x, int y) => Closes(Room, State, x, y);

    /// <summary>
    /// The way the gear turns its hub's walkways for a press, as seen from above for a flat gear (+1 counter-clockwise,
    /// -1 clockwise; <c>FieldTask_SunyshoreGym_RotateGears</c>): its own way, the other way for the reverse button.
    /// </summary>
    public static int Sense(Gear gear, Button kind) => (gear.Clockwise ? -1 : 1) * (kind == Button.Reverse ? -1 : 1);

    /// <summary>
    /// How far a gear is turned now, in degrees: its turn in the state it is in, or on its way from the last one to it.
    /// A flat gear's turn is about the vertical, counter-clockwise from above; a gear on edge's is about the east-west line.
    /// </summary>
    public float AngleOf(int gear)
    {
        var g = Gears[Room][gear];
        if (Turning is not { } turn) return 90f * g.TurnIn(State);
        return 90f * g.TurnIn(turn.From) + Sense(g, turn.Kind) * turn.Progress;
    }

    // ------------------------------------------------------------------ the walkways each gear has, for the picture and the tests

    /// <summary>The ways a flat gear's arms point with no turn: an L west and north, a T west, north and east, a dead end north.</summary>
    public static Direction[] ArmsOf(Shape shape) => shape switch
    {
        Shape.L or Shape.LAlt => new[] { Direction.Left, Direction.Up },
        Shape.T or Shape.TAlt => new[] { Direction.Left, Direction.Up, Direction.Right },
        Shape.DeadEnd => new[] { Direction.Up },
        _ => Array.Empty<Direction>()
    };

    /// <summary>A way turned a number of quarters counter-clockwise as seen from above: north to west, west to south.</summary>
    public static Direction Turned(Direction way, int quarters)
    {
        for (int i = 0; i < ((quarters % 4) + 4) % 4; i++)
            way = way switch { Direction.Up => Direction.Left, Direction.Left => Direction.Down, Direction.Down => Direction.Right, _ => Direction.Up };
        return way;
    }

    /// <summary>
    /// The tiles of a gear's walkways that are there in a state, by its shape and turn: a flat gear's arms, two tiles out
    /// from its hub each; a gear on edge's bar, the five tiles of its column, while it lies flat (a turn of nought or a half).
    /// The original's table says the same (<c>GymTests</c> holds the two together); the table is what the field asks.
    /// </summary>
    public static IEnumerable<(int X, int Y)> WalkwaysOf(Gear gear, int state)
    {
        int turn = gear.TurnIn(state);
        if (gear.OnEdge)
        {
            if (turn % 2 == 0)
                for (int dy = -2; dy <= 2; dy++) yield return (gear.X, gear.Y + dy);
            yield break;
        }
        foreach (var arm in ArmsOf(gear.Shape))
        {
            var (dx, dy) = FieldMovement.Delta(Turned(arm, turn));
            yield return (gear.X + dx, gear.Y + dy);
            yield return (gear.X + 2 * dx, gear.Y + 2 * dy);
        }
    }

    /// <summary>The tiles a gear's walkways can cover at any turn: its arms' four ways, or its bar's column.</summary>
    public static IEnumerable<(int X, int Y)> ReachOf(Gear gear)
    {
        if (gear.OnEdge)
        {
            for (int dy = -2; dy <= 2; dy++) yield return (gear.X, gear.Y + dy);
            yield break;
        }
        foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
        {
            var (dx, dy) = FieldMovement.Delta(way);
            yield return (gear.X + dx, gear.Y + dy);
            yield return (gear.X + 2 * dx, gear.Y + 2 * dy);
        }
    }

    // ------------------------------------------------------------------ the puzzle in the field

    public override void Apply(Map map, StoryState story) { }

    /// <summary>Laid out as the room's own script lays it out, as if the player came in by its door from the room before.</summary>
    public override void Arrive(Map map, StoryState story, Random rng) => Set(0);

    /// <summary>
    /// The room's own script as the player comes in (<c>InitPersistedMapFeaturesForSunyshoreGym</c>): by the door from the
    /// room before, the gears in their first state; anywhere else, in the state that leads back to it.
    /// </summary>
    public override void ArriveAt(Map map, StoryState story, Random rng, int x, int y) =>
        Set(y == EntranceRow[Room] ? 0 : BackState[Room]);

    /// <summary>A walkway that isn't there now (<c>SunyshoreGym_DynamicMapFeaturesCheckCollision</c>), whatever the height.</summary>
    public override bool Refuses(Map map, int x, int y, float from, bool afloat) => Closed(x, y);
}
