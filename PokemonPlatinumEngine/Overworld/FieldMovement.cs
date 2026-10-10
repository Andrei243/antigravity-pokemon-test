using System;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>How someone is getting about the field.</summary>
public enum TravelMode
{
    OnFoot,
    /// <summary>On a Pokémon's back, across water.</summary>
    Surfing,
    Cycling
}

/// <summary>Platinum's five speeds for a step, slowest first.</summary>
public enum Pace
{
    Slowest,
    Slow,
    Walk,
    Fast,
    Fastest
}

/// <summary>What the party's Pokémon can do in the field that changes where one can go.</summary>
[Flags]
public enum FieldMoves
{
    None = 0,
    Surf = 1,
    Waterfall = 2,
    RockClimb = 4,
    /// <summary>Knows Flash, which lights a dark cave once it is used (<see cref="Darkness"/>, <see cref="FieldMoveRules"/>).</summary>
    Flash = 8
}

public enum StepKind
{
    /// <summary>Nothing moves: a bump.</summary>
    Blocked,
    /// <summary>One tile on.</summary>
    Walk,
    /// <summary>Over a ledge: two tiles on, in an arc.</summary>
    Hop,
    /// <summary>From the water onto the shore: one tile on, and on foot again.</summary>
    Land,
    /// <summary>Up or down a waterfall or a rock face, to the first tile past it.</summary>
    Climb,
    /// <summary>Off a Bicycle's ramp, through the air to the tile it lands on.</summary>
    Jump
}

/// <summary>Why a step was refused, for what the game does about it (a bump, an offer to surf).</summary>
public enum Obstacle
{
    None,
    MapEdge,
    /// <summary>A blocked tile: a wall, a tree, a rock.</summary>
    Solid,
    Person,
    /// <summary>The ground there is too far above or below.</summary>
    Cliff,
    /// <summary>A side of the tile that is closed: a railing, a counter's edge.</summary>
    Rail,
    /// <summary>A ledge from the wrong side, or with nowhere to land beyond it.</summary>
    Ledge,
    /// <summary>Deep water, and nobody is surfing.</summary>
    Water,
    /// <summary>A rock face that needs Rock Climb, or is being taken sideways.</summary>
    RockFace,
    /// <summary>A waterfall that needs Waterfall, or is being taken sideways.</summary>
    Waterfall,
    /// <summary>A muddy slope only a Bicycle in its fast gear gets up.</summary>
    MudSlope,
    /// <summary>A plank only a Bicycle crosses.</summary>
    BicyclesOnly,
    /// <summary>Ground a Bicycle can't be ridden on: snow, mud, grass taller than the rider.</summary>
    NoBicycles,
    /// <summary>A Bicycle's ramp: a wall on foot, from the side and from its far end, or with nowhere to land.</summary>
    Ramp
}

/// <summary>
/// Whoever is taking a step: how they travel, the height they stand at, and what they can do. <see cref="Climbing"/>
/// is a climb they chose to make, by saying yes to Waterfall or Rock Climb (plan 02 · S2): walking into a rock face
/// or up a waterfall is a bump, as in the original.
/// </summary>
public readonly record struct Walker(
    TravelMode Mode = TravelMode.OnFoot, float Height = 0f, bool Running = false, bool FastGear = false, FieldMoves Moves = FieldMoves.None,
    bool Climbing = false);

/// <summary>What a step comes to: where it ends, at what height, how fast, and how the walker travels afterwards.</summary>
public readonly record struct FieldStep(StepKind Kind, int X, int Y, float Height, Pace Pace, TravelMode Mode, Obstacle Obstacle)
{
    public bool Moves => Kind != StepKind.Blocked;
}

/// <summary>
/// The rules of walking the field (plan 01 · M3): what each of Platinum's tile behaviours does to someone who
/// tries to step onto it, and the height rule that makes cliffs. No drawing and no input: tests walk maps with
/// it, and <see cref="Player"/> plays out what it decides. The rules follow the original's movement code
/// (<c>src/player_move.c</c> in the decompilation); where this game is simpler, <c>docs/tile-behaviours.md</c> says so.
/// </summary>
public static class FieldMovement
{
    /// <summary>Platinum's limit: ground this many tiles or more above or below can't be stepped onto.</summary>
    public const float StepLimit = 1.25f;

    /// <summary>How long a boulder takes to be pushed a tile with Strength: the original's slow walk, 16 frames at 30 a second.</summary>
    public const float BoulderPushSeconds = 16f / 30f;

    public static (int X, int Y) Delta(Direction dir) => dir switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        _ => (1, 0)
    };

    public static Direction Opposite(Direction dir) => dir switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        _ => Direction.Left
    };

    /// <summary>The way a ledge is hopped over; null for anything that isn't a ledge.</summary>
    public static Direction? LedgeDirection(TileBehavior behaviour) => behaviour switch
    {
        TileBehavior.LedgeSouth => Direction.Down,
        TileBehavior.LedgeEast => Direction.Right,
        TileBehavior.LedgeWest => Direction.Left,
        _ => null
    };

    /// <summary>
    /// The way one of the Distortion World's gaps is jumped (its "jump twice" tiles, <c>TILE_BEHAVIOR_JUMP_*_TWICE</c>);
    /// null for any other tile.
    /// </summary>
    public static Direction? LongJumpDirection(TileBehavior behaviour) => behaviour switch
    {
        TileBehavior.LongLedgeNorth => Direction.Up,
        TileBehavior.LongLedgeSouth => Direction.Down,
        TileBehavior.LongLedgeWest => Direction.Left,
        TileBehavior.LongLedgeEast => Direction.Right,
        _ => null
    };

    /// <summary>
    /// How far a jump across one of the Distortion World's gaps carries: from the tile before it to the third tile
    /// on (<c>MovementAction_JumpDistortionWorldNorth</c> and the rest: two units a frame for 24 frames, three tiles).
    /// </summary>
    public const int LongJumpTiles = 3;

    /// <summary>Whether a tile's side toward a direction is closed, so nobody steps across that edge either way.</summary>
    public static bool ClosedToward(TileBehavior behaviour, Direction side) => behaviour switch
    {
        TileBehavior.BlockEast => side == Direction.Right,
        TileBehavior.BlockWest => side == Direction.Left,
        TileBehavior.BlockNorthAndSouth => side is Direction.Up or Direction.Down,
        TileBehavior.BlockEastAndWest => side is Direction.Left or Direction.Right,
        _ => false
    };

    /// <summary>Whether a rock face is climbed along a direction (north and south, or east and west).</summary>
    public static bool IsRockFace(TileBehavior behaviour, Direction along) => behaviour switch
    {
        TileBehavior.RockClimbNorthSouth => along is Direction.Up or Direction.Down,
        TileBehavior.RockClimbEastWest => along is Direction.Left or Direction.Right,
        _ => false
    };

    /// <summary>A plank for Bicycles, and whether it runs north to south; null for anything else.</summary>
    /// <summary>The way a Bicycle's ramp is jumped, or null for any other tile.</summary>
    public static Direction? RampDirection(TileBehavior behaviour) => behaviour switch
    {
        TileBehavior.BikeRampEast => Direction.Right,
        TileBehavior.BikeRampWest => Direction.Left,
        _ => null
    };

    public static bool? BikePlankRunsNorthSouth(TileBehavior behaviour) => behaviour switch
    {
        TileBehavior.BikeBridgeNorthSouth or TileBehavior.BikeBridgeNorthSouthOverSand => true,
        TileBehavior.BikeBridgeEastWest or TileBehavior.BikeBridgeEastWestOverGrass or TileBehavior.BikeBridgeEastWestOverWater
            or TileBehavior.BikeBridgeEastWestOverSand => false,
        _ => null
    };

    /// <summary>Ground a Bicycle can't be ridden onto (deep water aside): snow, marsh mud and grass taller than the rider.</summary>
    public static bool StopsBicycles(TileBehavior behaviour) => behaviour is TileBehavior.DeepSnow or TileBehavior.DeeperSnow
        or TileBehavior.DeepestSnow or TileBehavior.ShallowSnow or TileBehavior.VeryTallGrass or TileBehavior.Mud or TileBehavior.DeepMud
        or TileBehavior.MarshGrass or TileBehavior.DeepMarshGrass;

    private static bool IsMudSlope(TileBehavior behaviour) => behaviour is TileBehavior.BikeSlopeTop or TileBehavior.BikeSlopeBottom;

    /// <summary>
    /// What happens when someone standing on (<paramref name="x"/>, <paramref name="y"/>) tries to move one tile
    /// in a direction.
    /// </summary>
    public static FieldStep Step(Map map, int x, int y, Direction dir, Walker walker)
    {
        var (dx, dy) = Delta(dir);
        int nx = x + dx, ny = y + dy;
        FieldStep No(Obstacle why) => new(StepKind.Blocked, x, y, walker.Height, Pace.Walk, walker.Mode, why);

        if (!map.InBounds(nx, ny)) return No(Obstacle.MapEdge);
        var here = map.BehaviourAt(x, y);
        var there = map.BehaviourAt(nx, ny);

        if (ClosedToward(here, dir) || ClosedToward(there, Opposite(dir))) return No(Obstacle.Rail);

        // A ledge is hopped the way it faces, by anyone not on the water, and is a wall from every other side
        if (LedgeDirection(there) is { } facing)
        {
            int lx = nx + dx, ly = ny + dy;
            if (facing != dir || walker.Mode == TravelMode.Surfing || !map.IsWalkable(lx, ly)) return No(Obstacle.Ledge);
            return new FieldStep(StepKind.Hop, lx, ly, map.SurfaceAt(lx, ly, walker.Height).Height, Pace.Walk, walker.Mode, Obstacle.None);
        }

        // The tip of the Eterna Gym's hour hand is hopped over along the hand, either way, like a ledge
        // (DynamicMapFeatures_WillPlayerJumpEternaGymClock, plan 01 · M9)
        if (map.Puzzle?.HopsOver(nx, ny, dir) == true)
        {
            int lx = nx + dx, ly = ny + dy;
            if (walker.Mode != TravelMode.OnFoot || !map.IsWalkable(lx, ly)) return No(Obstacle.Ledge);
            return new FieldStep(StepKind.Hop, lx, ly, map.SurfaceAt(lx, ly, walker.Height).Height, Pace.Walk, walker.Mode, Obstacle.None);
        }

        // A gap in the Distortion World is jumped the way its tile says, over it and the tile beyond, to the third
        // tile on (PlayerAvatar_WillJumpTwiceDistortion); from any other side it is the drop it is
        if (LongJumpDirection(there) is { } across)
        {
            int lx = x + dx * LongJumpTiles, ly = y + dy * LongJumpTiles;
            if (across != dir || walker.Mode != TravelMode.OnFoot || !map.IsWalkable(lx, ly)) return No(Obstacle.Ledge);
            return new FieldStep(StepKind.Jump, lx, ly, map.SurfaceAt(lx, ly, walker.Height).Height, Pace.Walk, walker.Mode, Obstacle.None);
        }

        // A waterfall is taken north or south only: up it once the player has said yes to Waterfall, down it with a
        // Pokémon that knows the move (ov5_021E04A8: the way down needs the move and no badge, the way up the question)
        if (there == TileBehavior.Waterfall)
        {
            if (walker.Mode != TravelMode.Surfing) return No(Obstacle.Water);
            if (dir is Direction.Left or Direction.Right) return No(Obstacle.Waterfall);
            if (!walker.Moves.HasFlag(FieldMoves.Waterfall)) return No(Obstacle.Waterfall);
            if (dir == Direction.Up && !walker.Climbing) return No(Obstacle.Waterfall);
            var (tx, ty) = PastRun(map, nx, ny, dx, dy, b => b == TileBehavior.Waterfall);
            if (!map.InBounds(tx, ty) || map.IsSolid(tx, ty) || !map.IsDeepWater(tx, ty)) return No(Obstacle.Waterfall);
            return new FieldStep(StepKind.Climb, tx, ty, map.HeightAt(tx, ty), Pace.Fast, TravelMode.Surfing, Obstacle.None);
        }

        // A rock face is climbed along its grain with Rock Climb, once the player has said yes to it, to the ground past it
        if (there is TileBehavior.RockClimbNorthSouth or TileBehavior.RockClimbEastWest)
        {
            if (!IsRockFace(there, dir) || walker.Mode != TravelMode.OnFoot || !walker.Moves.HasFlag(FieldMoves.RockClimb) || !walker.Climbing)
                return No(Obstacle.RockFace);
            var (tx, ty) = PastRun(map, nx, ny, dx, dy, b => b == there);
            if (!map.IsWalkable(tx, ty)) return No(Obstacle.RockFace);
            return new FieldStep(StepKind.Climb, tx, ty, map.HeightAt(tx, ty), Pace.Walk, TravelMode.OnFoot, Obstacle.None);
        }

        // A ramp is jumped the way it faces on a Bicycle, from the tile before it: onto it and three tiles on in top
        // gear, one in low (PlayerAvatar_TileMove_BikeRampEast and West: JUMP_FARTHER, JUMP_NEAR_SLOW)
        if (RampDirection(there) is { } launch)
        {
            if (walker.Mode != TravelMode.Cycling || dir != launch) return No(Obstacle.Ramp);
            int reach = walker.FastGear ? 3 : 1;
            int lx = nx + dx * reach, ly = ny + dy * reach;
            if (!map.IsWalkable(lx, ly) || map.NpcIn(lx, ly, map.SurfaceAt(lx, ly, walker.Height).Height) != null) return No(Obstacle.Ramp);
            return new FieldStep(StepKind.Jump, lx, ly, map.SurfaceAt(lx, ly, walker.Height).Height, PaceOn(here, walker), walker.Mode, Obstacle.None);
        }

        // A Gym's puzzle may decide a tile instead of its blocked flag (the Pastoria Gym's floors that are walked
        // onto only from one height: DynamicMapFeatures_CheckCollision)
        var gate = map.Puzzle?.Collides(map, nx, ny, walker.Height);
        if (gate == true || (gate == null && map.IsSolid(nx, ny))) return No(Obstacle.Solid);

        var (height, onDeck, onWater) = map.StandAt(nx, ny, walker.Height);
        if (map.NpcIn(nx, ny, height) is { } someone && someone != map.Follower) return No(Obstacle.Person);
        if (MathF.Abs(height - walker.Height) >= StepLimit) return No(Obstacle.Cliff);
        // Nobody stands on a puzzle's water where it has nothing to stand on: the pool round the Pastoria Gym's floats
        // (TILE_BEHAVIOR_DYNAMIC_HEIGHT_COLLISION, TerrainCollisionManager_WillPlayerCollide)
        if (onWater && gate == null && there == TileBehavior.MovingFloor) return No(Obstacle.Water);
        bool water = TileBehaviors.IsSurfable(there) && !onDeck;

        if (walker.Mode == TravelMode.Surfing)
        {
            // Water carries on; anything else open is the shore
            return water
                ? new FieldStep(StepKind.Walk, nx, ny, height, Pace.Fast, TravelMode.Surfing, Obstacle.None)
                : new FieldStep(StepKind.Land, nx, ny, height, Pace.Walk, TravelMode.OnFoot, Obstacle.None);
        }
        if (water) return No(Obstacle.Water);

        bool fastBike = walker.Mode == TravelMode.Cycling && walker.FastGear;
        if (dir == Direction.Up && (IsMudSlope(there) || IsMudSlope(here)) && !fastBike) return No(Obstacle.MudSlope);

        // A Bicycle's plank is the deck's: whoever walks on the ground under it (a road through the gorge beneath Route
        // 210's bridge) is not on it (plan 01 · M7)
        var plank = map.DeckAt(nx, ny) != null && !onDeck ? null : BikePlankRunsNorthSouth(there);
        if (walker.Mode == TravelMode.Cycling)
        {
            if (StopsBicycles(there)) return No(Obstacle.NoBicycles);
            // A plank is ridden along, never across
            if (plank is { } northSouth && northSouth != (dir is Direction.Up or Direction.Down)) return No(Obstacle.Rail);
        }
        else if (plank != null) return No(Obstacle.BicyclesOnly);

        return new FieldStep(StepKind.Walk, nx, ny, height, PaceOn(here, walker), walker.Mode, Obstacle.None);
    }

    // The first tile past a run of tiles of one kind, going one way
    private static (int X, int Y) PastRun(Map map, int x, int y, int dx, int dy, Func<TileBehavior, bool> inRun)
    {
        while (map.InBounds(x, y) && inRun(map.BehaviourAt(x, y)))
        {
            x += dx;
            y += dy;
        }
        return (x, y);
    }

    /// <summary>
    /// How fast a step is taken from a tile: deep snow slows a walker by its depth and nobody runs in it or in
    /// mud; a surfer goes at a run; a Bicycle is faster still.
    /// </summary>
    public static Pace PaceOn(TileBehavior underfoot, Walker walker) => walker.Mode switch
    {
        TravelMode.Surfing => Pace.Fast,
        TravelMode.Cycling => walker.FastGear ? Pace.Fastest : Pace.Fast,
        _ => underfoot switch
        {
            TileBehavior.DeepestSnow => Pace.Slowest,
            TileBehavior.DeeperSnow or TileBehavior.DeepMud or TileBehavior.DeepMarshGrass => Pace.Slow,
            TileBehavior.DeepSnow or TileBehavior.Mud or TileBehavior.MarshGrass => Pace.Walk,
            _ => walker.Running ? Pace.Fast : Pace.Walk
        }
    };

    /// <summary>Tiles a second at each pace: a walk is the game's 4.5, a run 8.</summary>
    public static float TilesPerSecond(Pace pace) => pace switch
    {
        Pace.Slowest => 1.125f,
        Pace.Slow => 2.25f,
        Pace.Walk => 4.5f,
        Pace.Fast => 8f,
        _ => 12f
    };

    /// <summary>
    /// Where the tile someone has just arrived on takes them next, whatever they press: ice keeps them sliding
    /// the way they came until something stops them, and a moving floor carries them its own way. Null lets
    /// them stand.
    /// </summary>
    public static Direction? Carries(TileBehavior underfoot, Direction arrivedMoving) => underfoot switch
    {
        TileBehavior.Ice => arrivedMoving,
        TileBehavior.SlideEast => Direction.Right,
        TileBehavior.SlideWest => Direction.Left,
        TileBehavior.SlideNorth => Direction.Up,
        TileBehavior.SlideSouth => Direction.Down,
        _ => null
    };

    /// <summary>
    /// Whether someone on foot who faces a direction stands at the edge of deep water they could surf out onto
    /// (still water or a river within a step's height; not a waterfall, and not from a bridge's deck).
    /// </summary>
    public static bool CanStartSurf(Map map, int x, int y, Direction dir, Walker walker)
    {
        if (walker.Mode != TravelMode.OnFoot) return false;
        var (dx, dy) = Delta(dir);
        int nx = x + dx, ny = y + dy;
        if (!map.InBounds(nx, ny) || map.IsSolid(nx, ny) || map.GetNpcAt(nx, ny) != null) return false;
        var there = map.BehaviourAt(nx, ny);
        if (!TileBehaviors.IsSurfable(there) || there == TileBehavior.Waterfall) return false;
        if (ClosedToward(map.BehaviourAt(x, y), dir) || ClosedToward(there, Opposite(dir))) return false;
        var (height, onDeck) = map.SurfaceAt(nx, ny, walker.Height);
        return !onDeck && MathF.Abs(height - walker.Height) < StepLimit;
    }

    /// <summary>
    /// Whether a boulder can be pushed a tile on with Strength: the tile beyond is open ground it could be walked
    /// onto at its own height, with nobody there (the original asks the boulder's own step for a collision,
    /// <c>sub_02063EBC</c>): no water, no ledge, no cliff, nothing solid.
    /// </summary>
    public static bool CanPush(Map map, NPC boulder, Direction dir)
    {
        var (dx, dy) = Delta(dir);
        var step = Step(map, boulder.GridX, boulder.GridY, dir, new Walker(TravelMode.OnFoot, map.HeightAt(boulder.GridX, boulder.GridY)));
        return step.Kind == StepKind.Walk && step.X == boulder.GridX + dx && step.Y == boulder.GridY + dy
            && map.GetWarpAt(step.X, step.Y) == null;
    }

    /// <summary>What a party can do in the field: the moves its Pokémon know that open the way.</summary>
    public static FieldMoves MovesOf(Models.Party party)
    {
        var moves = FieldMoves.None;
        // A Pokémon that has fainted can still carry its trainer, as in the original
        foreach (var pokemon in party.Members)
        {
            foreach (var move in pokemon.Moves)
            {
                moves |= move.Name switch
                {
                    "Surf" => FieldMoves.Surf,
                    "Waterfall" => FieldMoves.Waterfall,
                    "Rock Climb" => FieldMoves.RockClimb,
                    "Flash" => FieldMoves.Flash,
                    _ => FieldMoves.None
                };
            }
        }
        return moves;
    }
}
