using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Canalave Gym's lifts (plan 01 · M9), ported from the original's <c>gym_features.c</c>. The Gym is four floors,
/// ten tiles apart (<c>CANALAVE_DIST_BETWEEN_FLOORS</c>), each with its own map of the tiles that can be walked
/// (<c>sCanalaveGymCollisionMaps</c>, chosen by the walker's height: <c>CanalaveGym_DynamicMapFeaturesCheckCollision</c>).
/// Twenty-four platforms join them (<c>sCanalavePlatformPaths</c>): lifts between two floors, two red shafts from the
/// ground to the top, and platforms that run east and west or north and south along a floor. Each stands at one of its
/// two ends; a step that ends on a platform (<c>Field_ProcessStep</c>, <c>CanalaveGym_CheckIfPlayerOnPlatform</c>)
/// carries the player to its other end, at a tile every two frames at thirty a second (<c>FX32_CONST(8)</c> a frame
/// up or down, <c>WALK_FASTER</c> along). Which end each stands at is laid out afresh as the player comes in
/// (<c>PersistedMapFeatures_InitForCanalaveGym</c>, by the room's own script).
/// <para>
/// One rule is ours: a platform's place on a floor above the ground is open only while the platform stands there, so
/// nobody walks on the air of an empty slot (the original's maps leave those tiles open, under its floors' models).
/// </para>
/// </summary>
public sealed class CanalaveLifts : GymPuzzle
{
    public const string PuzzleName = "CanalaveLifts";
    public override string Name => PuzzleName;

    /// <summary>The floors' heights, in tiles, and how many there are.</summary>
    public const int FloorSpacing = 10, Floors = 4;

    /// <summary>How fast a platform carries the player, in tiles a second.</summary>
    public const float TilesPerSecond = 15f;

    /// <summary>How a platform moves: up and down between two floors (the red shafts from the ground to the top), or along a floor.</summary>
    public enum Kind { Lift, Shaft, EastWest, NorthSouth }

    /// <summary>A platform's two ends, as tiles (x, height, y), and whether it stands at the second when the player comes in.</summary>
    public sealed record Platform(Kind Kind, (int X, int H, int Y) A, (int X, int H, int Y) B, bool StartsAtB)
    {
        public bool Vertical => Kind is Kind.Lift or Kind.Shaft;
    }

    /// <summary>The original's platforms, in its order (<c>sCanalavePlatformPaths</c>).</summary>
    public static readonly Platform[] Platforms =
    {
        new(Kind.Shaft, (16, 0, 9), (16, 30, 9), false),
        new(Kind.Lift, (11, 0, 13), (11, 10, 13), false),
        new(Kind.Lift, (15, 0, 13), (15, 10, 13), false),
        new(Kind.Lift, (19, 0, 13), (19, 10, 13), false),
        new(Kind.Lift, (24, 0, 13), (24, 10, 13), false),
        new(Kind.Lift, (21, 0, 22), (21, 10, 22), false),
        new(Kind.Lift, (25, 0, 9), (25, 10, 9), true),
        new(Kind.Lift, (25, 0, 22), (25, 10, 22), true),
        new(Kind.Lift, (29, 0, 22), (29, 10, 22), true),
        new(Kind.Lift, (5, 10, 26), (5, 20, 26), false),
        new(Kind.EastWest, (11, 10, 22), (18, 10, 22), true),
        new(Kind.Lift, (29, 10, 9), (29, 20, 9), true),
        new(Kind.EastWest, (10, 20, 4), (14, 20, 4), false),
        new(Kind.EastWest, (19, 20, 4), (22, 20, 4), false),
        new(Kind.EastWest, (7, 20, 12), (22, 20, 12), false),
        new(Kind.EastWest, (9, 20, 26), (21, 20, 26), false),
        new(Kind.NorthSouth, (2, 20, 19), (2, 20, 22), true),
        new(Kind.NorthSouth, (26, 20, 16), (26, 20, 22), false),
        new(Kind.NorthSouth, (29, 20, 16), (29, 20, 22), true),
        new(Kind.Shaft, (7, 0, 9), (7, 30, 9), true),
        new(Kind.EastWest, (19, 30, 4), (26, 30, 4), false),
        new(Kind.EastWest, (5, 30, 26), (26, 30, 26), true),
        new(Kind.NorthSouth, (29, 30, 7), (29, 30, 23), false),
        new(Kind.NorthSouth, (2, 30, 12), (2, 30, 23), true)
    };

    // The original's collision maps, a floor each: '#' closed, '.' open (sCanalaveGymCollisionMaps)
    private static readonly string[][] Collision =
    {
        // Floor 0
        new[]
        {
            "################################",
            "################################",
            "################################",
            "################################",
            "################################",
            "################################",
            "################################",
            "################################",
            "######...#.................#####",
            "######...#.................#####",
            "######...#.................#####",
            "######...#######################",
            "######...........#.........#####",
            "######...........#.........#####",
            "######...........#.........#####",
            "######...........#.........#####",
            "######.....................#####",
            "######.....................#####",
            "######...###########.......#####",
            "######.............#.......#####",
            "######.............#####...#####",
            "######.............#...#...#...#",
            "######.............#...#...#...#",
            "######.............#...#...#...#",
            "######.............#...#####...#",
            "######........#...##...........#",
            "######.............#...........#",
            "######.............#...........#",
            "################################",
            "################################",
            "################################",
            "################################"
        },
        // Floor 1
        new[]
        {
            "################################",
            "################################",
            "################################",
            "######...#............#........#",
            "######...#............#........#",
            "######...#............#........#",
            "######...#...#######..#.####...#",
            "######...#...#.....#..#....#...#",
            "##########...#.###.#..#....#...#",
            "##########...#.###.#..#....#...#",
            "##########...#.###.#..#....#...#",
            "######...#...###################",
            "##########...#...#....#........#",
            "##########...#...#....#........#",
            "##########...#...#....#........#",
            "####################..#........#",
            "####################..#######..#",
            "####################.......#...#",
            "####################.......#...#",
            "####################.......#...#",
            "#.........##############...#...#",
            "#.........##########...#...#...#",
            "#..#####....######.....#...#...#",
            "#......#..##########...#...#...#",
            "#......#..######################",
            "#......#..######################",
            "#......#..######################",
            "#......#..######################",
            "################################",
            "################################",
            "################################",
            "################################"
        },
        // Floor 2
        new[]
        {
            "################################",
            "################################",
            "################################",
            "#..#.....#######..######.......#",
            "#..#.......###......##.........#",
            "#..#.....#######..######.......#",
            "#..#.....#####......####.......#",
            "#..#..########......######.#####",
            "#..#..########.###..####.#.#...#",
            "#..#..########.###..####.#.#...#",
            "#..#..########.###..####.#.#...#",
            "####..##################.#.#...#",
            "#.......##############...#.#...#",
            "#.....##################.#.#...#",
            "#.....##################.#.#...#",
            "#.....####################.##.##",
            "#.....####################.##.##",
            "#.....##########################",
            "##.#############################",
            "##.#############################",
            "################################",
            "################################",
            "##.#######################.##.##",
            "##.#######################.##.##",
            "#.......###############.#......#",
            "#.......###############.#......#",
            "#.........###########...#......#",
            "#.......###############.#......#",
            "################################",
            "################################",
            "################################",
            "################################"
        },
        // Floor 3
        new[]
        {
            "################################",
            "################################",
            "################################",
            "################.###########...#",
            "###############.....######.....#",
            "###############...##########...#",
            "##############.....##########.##",
            "##############.....##########.##",
            "#........#####.....#############",
            "#........#####.....#############",
            "#........#####.....#############",
            "##.#############################",
            "##.#############################",
            "################################",
            "################################",
            "################################",
            "################################",
            "################################",
            "################################",
            "################################",
            "################################",
            "################################",
            "################################",
            "##.##########################.##",
            "##.##########################.##",
            "#...########################...#",
            "#.....####################.....#",
            "#...########################...#",
            "################################",
            "################################",
            "################################",
            "################################"
        },
    };

    /// <summary>Which end each platform stands at now: true at its second (the original's <c>platformStates</c> bits).</summary>
    private readonly bool[] atB = new bool[Platforms.Length];

    public CanalaveLifts() => Reset();

    /// <summary>A platform carrying the player from one end to the other.</summary>
    public sealed class Ride
    {
        public int Index { get; }
        public Platform Platform => Platforms[Index];
        public (int X, int H, int Y) From { get; }
        public (int X, int H, int Y) To { get; }
        public float Time { get; private set; }
        public float Duration { get; }

        public Ride(int index, (int X, int H, int Y) from, (int X, int H, int Y) to)
        {
            Index = index;
            From = from;
            To = to;
            Duration = (Math.Abs(to.X - from.X) + Math.Abs(to.H - from.H) + Math.Abs(to.Y - from.Y)) / TilesPerSecond;
        }

        /// <summary>Where the platform and whoever rides it are now, in tiles: across, height, down.</summary>
        public (float X, float H, float Y) Now
        {
            get
            {
                float t = Duration <= 0f ? 1f : Math.Clamp(Time / Duration, 0f, 1f);
                return (From.X + (To.X - From.X) * t, From.H + (To.H - From.H) * t, From.Y + (To.Y - From.Y) * t);
            }
        }

        public bool Done => Time >= Duration;

        public void Update(float dt) => Time = MathF.Min(Duration, Time + dt);

        /// <summary>Whether the ride passes over a tile, at its own floor (a run along a floor) or its own place (a lift).</summary>
        public bool Covers(int x, int y) =>
            x >= Math.Min(From.X, To.X) && x <= Math.Max(From.X, To.X) && y >= Math.Min(From.Y, To.Y) && y <= Math.Max(From.Y, To.Y);
    }

    /// <summary>The ride under way, or null.</summary>
    public Ride? Moving { get; private set; }

    /// <summary>The floor a height is on: the nearest, nought to three.</summary>
    public static int FloorOf(float height) => Math.Clamp((int)MathF.Round(height / FloorSpacing), 0, Floors - 1);

    /// <summary>Whether the original's map of a floor closes a tile.</summary>
    public static bool Closed(int floor, int x, int y) =>
        x < 0 || y < 0 || y >= Collision[floor].Length || x >= Collision[floor][y].Length || Collision[floor][y][x] == '#';

    /// <summary>Whether any floor can be walked on a tile: the room's own grid is open there, and each floor's map closes the rest.</summary>
    public static bool OpenOnAnyFloor(int x, int y)
    {
        for (int f = 0; f < Floors; f++)
            if (!Closed(f, x, y)) return true;
        return false;
    }

    /// <summary>Where a platform stands now.</summary>
    public (int X, int H, int Y) Where(int index) => atB[index] ? Platforms[index].B : Platforms[index].A;

    /// <summary>Whether a platform stands at its second end.</summary>
    public bool AtB(int index) => atB[index];

    /// <summary>Every platform at the end it starts at (<c>sCanalaveGymPlatformsStartInPositionB</c>).</summary>
    public void Reset()
    {
        for (int i = 0; i < Platforms.Length; i++) atB[i] = Platforms[i].StartsAtB;
        Moving = null;
    }

    /// <summary>Puts a platform at one end at once (a test, the harness).</summary>
    public void Set(int index, bool second) => atB[index] = second;

    /// <summary>The platform standing on a tile at a floor's height, or null (<c>CanalaveGym_GetPlaformPlayerIsOn</c>).</summary>
    public int? PlatformAt(int x, int y, float height)
    {
        int floorHeight = FloorOf(height) * FloorSpacing;
        for (int i = 0; i < Platforms.Length; i++)
        {
            var at = Where(i);
            if (at.X == x && at.Y == y && at.H == floorHeight) return i;
        }
        return null;
    }

    /// <summary>
    /// Whether a tile is a platform's place on a floor above the ground, and none stands there now: our rule that
    /// nobody walks on an empty slot.
    /// </summary>
    public bool EmptySlot(int floor, int x, int y)
    {
        if (floor == 0) return false;
        bool slot = false;
        for (int i = 0; i < Platforms.Length; i++)
        {
            var p = Platforms[i];
            foreach (var end in new[] { p.A, p.B })
                if (end.X == x && end.Y == y && end.H == floor * FloorSpacing)
                {
                    if (Where(i) == end) return false;
                    slot = true;
                }
        }
        return slot;
    }

    /// <summary>
    /// The player has stepped onto a platform: it sets off for its other end with them on it
    /// (<c>CanalaveGym_MovePlatform</c>, which turns its bit over as it starts).
    /// </summary>
    public Ride Board(int index)
    {
        var from = Where(index);
        atB[index] = !atB[index];
        Moving = new Ride(index, from, Where(index));
        return Moving;
    }

    /// <summary>Moves the ride on; it is over once the platform has come to its end.</summary>
    public void Update(float dt)
    {
        if (Moving == null) return;
        Moving.Update(dt);
        if (Moving.Done) Moving = null;
    }

    /// <summary>Takes a ride to its end at once (a test, the harness).</summary>
    public void Finish() => Moving = null;

    public override void Apply(Map map, StoryState story) { }

    /// <summary>The room's own script lays the platforms out as the player comes in (<c>InitPersistedMapFeaturesForCanalaveGym</c>).</summary>
    public override void Arrive(Map map, StoryState story, Random rng) => Reset();

    /// <summary>
    /// The floor someone at a height stands at on a tile, above the ground: the floor nearest them where its map lets
    /// them, and wherever a ride passes, the ride's own height.
    /// </summary>
    public override float? FloorAt(int x, int y, float near)
    {
        if (Moving is { } ride && ride.Covers(x, y) && MathF.Abs(ride.Now.H - near) < FloorSpacing / 2f) return ride.Now.H;
        int floor = FloorOf(near);
        if (floor == 0 || Closed(floor, x, y) || EmptySlot(floor, x, y)) return null;
        return floor * FloorSpacing;
    }

    /// <summary>The floor's map, at the height stepped from, and an empty slot (<c>CanalaveGym_DynamicMapFeaturesCheckCollision</c>).</summary>
    public override bool Refuses(Map map, int x, int y, float from, bool afloat)
    {
        int floor = FloorOf(from);
        return Closed(floor, x, y) || EmptySlot(floor, x, y);
    }

    /// <summary>A trainer looks along their own floor only, and not past what it closes.</summary>
    public override bool BlocksSight(int x, int y, float level) => Refuses(null!, x, y, level, false);

    public override bool Apart(float one, float other) => FloorOf(one) != FloorOf(other);

    /// <summary>
    /// What can be seen from a height (<c>CanalaveGym_UpdateVisibleProps</c>): the floors up to the one the viewer is
    /// on, each showing once they have risen a tile toward it, and nothing above.
    /// </summary>
    public static bool ShownFrom(int floor, float viewer) => floor == 0 || viewer >= (floor - 1) * FloorSpacing + 1;

    public override bool Hides(float height, float viewer) => !ShownFrom(FloorOf(height), viewer);
}
