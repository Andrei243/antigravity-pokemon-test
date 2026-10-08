using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// The rules of walking the field (plan 01 · M3), each on a small map made for it: what every tile behaviour
/// that changes a step does, the height rule, and the player playing the rules out.
/// </summary>
public class FieldMovementTests
{
    private const Direction North = Direction.Up, South = Direction.Down, West = Direction.Left, East = Direction.Right;

    /// <summary>An open lawn; tests put on it what they are about.</summary>
    private static Map Lawn(int width = 9, int height = 9) => new(width, height) { Name = "Lab" };

    private static FieldStep Step(Map map, int x, int y, Direction dir, TravelMode mode = TravelMode.OnFoot, FieldMoves moves = FieldMoves.None,
        bool running = false, bool fastGear = false, float? height = null, bool climbing = false) =>
        FieldMovement.Step(map, x, y, dir, new Walker(mode, height ?? map.HeightAt(x, y), running, fastGear, moves, climbing));

    private static void AssertBlocked(Obstacle why, FieldStep step)
    {
        Assert.Equal(StepKind.Blocked, step.Kind);
        Assert.False(step.Moves);
        Assert.Equal(why, step.Obstacle);
    }

    private static void AssertMoves(StepKind kind, int x, int y, FieldStep step)
    {
        Assert.Equal((kind, x, y, Obstacle.None), (step.Kind, step.X, step.Y, step.Obstacle));
        Assert.True(step.Moves);
    }

    /// <summary>Holds a direction until the player has stopped on a tile again (or a second has passed).</summary>
    private static void Walk(Player player, Map map, Direction dir, Action<WildEncounterEntry>? onWild = null)
    {
        player.Facing = dir;
        bool started = false;
        for (int frame = 0; frame < 600; frame++)
        {
            // Hold the direction only until the step starts, so one call is one step (plus whatever carries on by itself)
            player.Advance(1f / 60f, map, started ? null : dir, false, onWild ?? (_ => { }), _ => { });
            started |= player.IsMoving;
            if (started && !player.IsMoving && !player.IsSliding) return;
            if (!started && frame > 2) return;   // blocked
        }
    }

    // ------------------------------------------------------------------ plain ground

    [Fact]
    public void OpenGroundIsWalkedAndWhatBlocksSaysWhy()
    {
        var map = Lawn();
        AssertMoves(StepKind.Walk, 4, 3, Step(map, 4, 4, North));
        AssertMoves(StepKind.Walk, 5, 4, Step(map, 4, 4, East));
        Assert.Equal(Pace.Walk, Step(map, 4, 4, East).Pace);
        Assert.Equal(Pace.Fast, Step(map, 4, 4, East, running: true).Pace);

        AssertBlocked(Obstacle.MapEdge, Step(map, 0, 4, West));
        AssertBlocked(Obstacle.MapEdge, Step(map, 4, 8, South));

        map.SetGroundTile(4, 3, TileType.Tree, isSolid: true);
        AssertBlocked(Obstacle.Solid, Step(map, 4, 4, North));

        map.NPCs.Add(new NPC { Name = "Lass", GridX = 5, GridY = 4 });
        AssertBlocked(Obstacle.Person, Step(map, 4, 4, East));

        // A blocked step leaves the walker where and as they were
        var bump = Step(map, 4, 4, East, TravelMode.Cycling);
        Assert.Equal((4, 4, TravelMode.Cycling), (bump.X, bump.Y, bump.Mode));
    }

    [Fact]
    public void ATilesTypeSaysWhatItDoesUnlessTheMapSaysMore()
    {
        var map = Lawn();
        Assert.Equal(TileBehavior.None, map.BehaviourAt(1, 1));
        foreach (var (type, behaviour) in new[]
        {
            (TileType.TallGrass, TileBehavior.TallGrass), (TileType.Water, TileBehavior.Sea), (TileType.LedgeDown, TileBehavior.LedgeSouth),
            (TileType.LedgeLeft, TileBehavior.LedgeWest), (TileType.LedgeRight, TileBehavior.LedgeEast), (TileType.Ice, TileBehavior.Ice),
            (TileType.Sand, TileBehavior.Sand), (TileType.CaveFloor, TileBehavior.CaveFloor), (TileType.Marsh, TileBehavior.Mud),
            (TileType.Rock, TileBehavior.None), (TileType.Planks, TileBehavior.None), (TileType.Stairs, TileBehavior.None)
        })
        {
            map.SetGroundTile(2, 2, type);
            Assert.Equal(behaviour, map.BehaviourAt(2, 2));
        }

        // A behaviour given outright wins, on that tile only
        map.SetGroundTile(2, 2, TileType.Snow);
        map.SetBehaviour(2, 2, TileBehavior.DeepestSnow);
        map.SetGroundTile(3, 2, TileType.Snow);
        Assert.Equal(TileBehavior.DeepestSnow, map.BehaviourAt(2, 2));
        Assert.Equal(TileBehavior.ShallowSnow, map.BehaviourAt(3, 2));
        Assert.Equal(TileBehavior.None, map.BehaviourAt(-1, 2));

        // What can be stood on: not a ledge, not deep water, not a tile someone is on
        map.SetGroundTile(5, 5, TileType.Water);
        map.SetGroundTile(6, 5, TileType.LedgeDown);
        map.SetGroundTile(7, 5, TileType.TallGrass);
        Assert.False(map.IsWalkable(5, 5));
        Assert.True(map.IsDeepWater(5, 5));
        Assert.False(map.IsWalkable(6, 5));
        Assert.True(map.IsLedge(6, 5));
        Assert.True(map.IsWalkable(7, 5));
        Assert.True(map.IsTallGrass(7, 5));
    }

    // ------------------------------------------------------------------ ledges

    [Theory]
    [InlineData(TileType.LedgeDown, Direction.Down)]
    [InlineData(TileType.LedgeLeft, Direction.Left)]
    [InlineData(TileType.LedgeRight, Direction.Right)]
    public void ALedgeIsHoppedTheWayItFacesAndIsAWallFromEveryOtherSide(TileType ledge, Direction facing)
    {
        var map = Lawn();
        map.SetGroundTile(4, 4, ledge);
        var (dx, dy) = FieldMovement.Delta(facing);

        // From the tile behind it, two tiles on in one hop
        var hop = Step(map, 4 - dx, 4 - dy, facing);
        AssertMoves(StepKind.Hop, 4 + dx, 4 + dy, hop);
        Assert.Equal(TravelMode.OnFoot, hop.Mode);

        // Never back up it, nor onto it from its ends
        AssertBlocked(Obstacle.Ledge, Step(map, 4 + dx, 4 + dy, FieldMovement.Opposite(facing)));
        foreach (var side in new[] { North, South, West, East })
        {
            if (side == facing) continue;
            var (sx, sy) = FieldMovement.Delta(side);
            AssertBlocked(Obstacle.Ledge, Step(map, 4 - sx, 4 - sy, side));
        }

        // Someone on a Bicycle hops it too
        AssertMoves(StepKind.Hop, 4 + dx, 4 + dy, Step(map, 4 - dx, 4 - dy, facing, TravelMode.Cycling));

        // With nowhere to land there is no hop: something solid, someone standing there, water, another ledge
        map.SetSolid(4 + dx, 4 + dy, true);
        AssertBlocked(Obstacle.Ledge, Step(map, 4 - dx, 4 - dy, facing));
        map.SetSolid(4 + dx, 4 + dy, false);
        map.SetGroundTile(4 + dx, 4 + dy, TileType.Water);
        AssertBlocked(Obstacle.Ledge, Step(map, 4 - dx, 4 - dy, facing));
        map.SetGroundTile(4 + dx, 4 + dy, ledge);
        AssertBlocked(Obstacle.Ledge, Step(map, 4 - dx, 4 - dy, facing));
    }

    [Fact]
    public void NoLedgeIsHoppedNorthward()
    {
        Assert.Equal(Direction.Down, FieldMovement.LedgeDirection(TileBehavior.LedgeSouth));
        Assert.Equal(Direction.Left, FieldMovement.LedgeDirection(TileBehavior.LedgeWest));
        Assert.Equal(Direction.Right, FieldMovement.LedgeDirection(TileBehavior.LedgeEast));
        foreach (var behaviour in Enum.GetValues<TileBehavior>())
            Assert.NotEqual(Direction.Up, FieldMovement.LedgeDirection(behaviour));
    }

    // ------------------------------------------------------------------ heights

    [Fact]
    public void AStepOfATileAndAQuarterIsACliff()
    {
        var map = Lawn();
        for (int y = 0; y < 9; y++)
            for (int x = 0; x < 9; x++)
                map.SetHeight(x, y, x >= 5 ? 2f : 0f);
        Assert.True(map.HasRelief);
        Assert.Equal(1.25f, FieldMovement.StepLimit);

        // Two tiles up or down: a wall, from below and from above
        AssertBlocked(Obstacle.Cliff, Step(map, 4, 4, East));
        AssertBlocked(Obstacle.Cliff, Step(map, 5, 4, West));
        AssertMoves(StepKind.Walk, 6, 4, Step(map, 5, 4, East));

        // One tile is a step that can be taken; a tile and a quarter is not
        map.SetHeight(5, 4, 1f);
        var up = Step(map, 4, 4, East);
        AssertMoves(StepKind.Walk, 5, 4, up);
        Assert.Equal(1f, up.Height);
        AssertMoves(StepKind.Walk, 6, 4, Step(map, 5, 4, East));
        map.SetHeight(5, 4, 1.25f);
        AssertBlocked(Obstacle.Cliff, Step(map, 4, 4, East));
        map.SetHeight(5, 4, 1.24f);
        AssertMoves(StepKind.Walk, 5, 4, Step(map, 4, 4, East));
    }

    [Fact]
    public void StairsAreGroundThatRisesATileAlong()
    {
        // A flight of four going up eastward from x = 3: each tile's middle is a tile higher than the last
        var map = Lawn();
        var sheer = Lawn();
        for (int y = 0; y < 9; y++)
            for (int x = 0; x < 9; x++)
            {
                if (x <= 2) map.SetHeight(x, y, 0f);
                else if (x <= 6) map.SetHeight(x, y, x - 2.5f, slopeX: 1f);
                else map.SetHeight(x, y, 4f);
                sheer.SetHeight(x, y, x <= 2 ? 0f : 4f);
            }
        Assert.Equal(0.5f, map.HeightAt(3, 4));
        Assert.Equal(0f, map.HeightAt(3, 4, 0f, 0.5f));    // its west edge meets the ground below
        Assert.Equal(1f, map.HeightAt(3, 4, 1f, 0.5f));    // its east edge meets the next step
        Assert.Equal(4f, map.HeightAt(6, 4, 1f, 0.5f));    // and the top one meets the terrace

        var player = new Player(1, 4);
        for (int i = 0; i < 6; i++) Walk(player, map, East);
        Assert.Equal((7, 4), (player.GridX, player.GridY));
        Assert.Equal(4f, player.HeightOn(map));

        // And back down
        for (int i = 0; i < 6; i++) Walk(player, map, West);
        Assert.Equal((1, 4), (player.GridX, player.GridY));
        Assert.Equal(0f, player.HeightOn(map));

        // The same four tiles of height without the stairs can't be climbed
        AssertBlocked(Obstacle.Cliff, Step(sheer, 2, 4, East));
        AssertBlocked(Obstacle.Cliff, Step(sheer, 3, 4, West));
    }

    [Fact]
    public void ABridgeIsCrossedOnItsDeckAndPassedUnderOnTheWater()
    {
        // A river running north to south down the middle column, in a cut two tiles deep; a bridge across it
        var map = Lawn();
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++) map.SetHeight(x, y, 2f);
            map.SetGroundTile(4, y, TileType.Water);
            map.SetHeight(4, y, 0f);
        }
        map.SetGroundTile(4, 4, TileType.Planks);
        map.SetBehaviour(4, 4, TileBehavior.BridgeOverWater);
        map.SetDeck(4, 4, 2f);
        Assert.Equal(2f, map.DeckAt(4, 4));
        Assert.Null(map.DeckAt(4, 3));
        Assert.True(map.IsWalkable(4, 4));

        // From the bank: onto the deck, and across
        var onto = Step(map, 3, 4, East);
        AssertMoves(StepKind.Walk, 4, 4, onto);
        Assert.Equal(2f, onto.Height);
        AssertMoves(StepKind.Walk, 5, 4, Step(map, 4, 4, East, height: 2f));

        // Elsewhere the bank is a cliff over the water, and nobody steps off the deck's sides
        AssertBlocked(Obstacle.Cliff, Step(map, 3, 2, East));
        AssertBlocked(Obstacle.Cliff, Step(map, 4, 4, North, height: 2f));

        // A surfer on the river goes under the bridge and stays on the water
        var under = Step(map, 4, 3, South, TravelMode.Surfing);
        AssertMoves(StepKind.Walk, 4, 4, under);
        Assert.Equal((0f, TravelMode.Surfing), (under.Height, under.Mode));
        AssertMoves(StepKind.Walk, 4, 5, Step(map, 4, 4, South, TravelMode.Surfing, height: 0f));
        // and can't climb out onto banks two tiles up
        AssertBlocked(Obstacle.Cliff, Step(map, 4, 3, East, TravelMode.Surfing));

        Assert.Equal((2f, true), map.SurfaceAt(4, 4, 1.8f));
        Assert.Equal((0f, false), map.SurfaceAt(4, 4, 0.3f));

        // The player walks it: across on the deck, a deck's height above the water all the way
        var player = new Player(2, 4);
        for (int i = 0; i < 4; i++)
        {
            Walk(player, map, East);
            Assert.Equal(2f, player.HeightOn(map));
        }
        Assert.Equal((6, 4, TravelMode.OnFoot), (player.GridX, player.GridY, player.Mode));

        // A save made on the bridge says which level: without that the player would wake up in the river under it
        var save = new PokemonPlatinumEngine.Core.SaveData { PlayerGridX = 4, PlayerGridY = 4, PlayerHeight = 2f, Travel = TravelMode.OnFoot };
        var read = System.Text.Json.JsonSerializer.Deserialize<PokemonPlatinumEngine.Core.SaveData>(System.Text.Json.JsonSerializer.Serialize(save))!;
        Assert.Equal((2f, TravelMode.OnFoot), (read.PlayerHeight, read.Travel));
        player.SetPosition(read.PlayerGridX, read.PlayerGridY, East);
        Assert.Equal(0f, player.HeightOn(map));
        player.SetHeight(read.PlayerHeight!.Value);
        Walk(player, map, East);
        Assert.Equal((5, 4), (player.GridX, player.GridY));
        Assert.Equal(2f, player.HeightOn(map));

        // An older save has neither field: on the ground, on foot
        var old = System.Text.Json.JsonSerializer.Deserialize<PokemonPlatinumEngine.Core.SaveData>("{}")!;
        Assert.Null(old.PlayerHeight);
        Assert.Equal(TravelMode.OnFoot, old.Travel);
    }

    // ------------------------------------------------------------------ closed sides

    [Fact]
    public void AClosedSideStopsAStepAcrossItEitherWay()
    {
        var map = Lawn();
        map.SetBehaviour(4, 4, TileBehavior.BlockEast);
        AssertBlocked(Obstacle.Rail, Step(map, 4, 4, East));
        AssertBlocked(Obstacle.Rail, Step(map, 5, 4, West));
        AssertMoves(StepKind.Walk, 3, 4, Step(map, 4, 4, West));
        AssertMoves(StepKind.Walk, 4, 3, Step(map, 4, 4, North));
        AssertMoves(StepKind.Walk, 4, 4, Step(map, 4, 5, North));

        map.SetBehaviour(4, 4, TileBehavior.BlockWest);
        AssertBlocked(Obstacle.Rail, Step(map, 4, 4, West));
        AssertBlocked(Obstacle.Rail, Step(map, 3, 4, East));
        AssertMoves(StepKind.Walk, 5, 4, Step(map, 4, 4, East));

        // Snowpoint's walkways: closed on both sides, open along
        map.SetBehaviour(4, 4, TileBehavior.BlockNorthAndSouth);
        AssertBlocked(Obstacle.Rail, Step(map, 4, 4, North));
        AssertBlocked(Obstacle.Rail, Step(map, 4, 3, South));
        AssertMoves(StepKind.Walk, 5, 4, Step(map, 4, 4, East));
        map.SetBehaviour(4, 4, TileBehavior.BlockEastAndWest);
        AssertBlocked(Obstacle.Rail, Step(map, 4, 4, East));
        AssertMoves(StepKind.Walk, 4, 5, Step(map, 4, 4, South));
    }

    // ------------------------------------------------------------------ water

    /// <summary>A pond in the middle of a lawn: water at (3..5, 3..5).</summary>
    private static Map Pond()
    {
        var map = Lawn();
        for (int y = 3; y <= 5; y++)
            for (int x = 3; x <= 5; x++)
                map.SetGroundTile(x, y, TileType.Water);
        return map;
    }

    [Fact]
    public void DeepWaterWaitsForSurf()
    {
        var map = Pond();
        AssertBlocked(Obstacle.Water, Step(map, 2, 4, East));
        AssertBlocked(Obstacle.Water, Step(map, 2, 4, East, TravelMode.Cycling));

        // At the water's edge, facing it, one can set out
        Assert.True(FieldMovement.CanStartSurf(map, 2, 4, East, new Walker()));
        Assert.False(FieldMovement.CanStartSurf(map, 2, 4, North, new Walker()));
        Assert.False(FieldMovement.CanStartSurf(map, 2, 4, East, new Walker(TravelMode.Surfing)));
        Assert.False(FieldMovement.CanStartSurf(map, 2, 4, East, new Walker(TravelMode.Cycling)));

        // On the water a step is a glide at a run's pace; the shore is landed on, and one is on foot again
        var glide = Step(map, 3, 4, East, TravelMode.Surfing);
        AssertMoves(StepKind.Walk, 4, 4, glide);
        Assert.Equal((Pace.Fast, TravelMode.Surfing), (glide.Pace, glide.Mode));
        var land = Step(map, 3, 4, West, TravelMode.Surfing);
        AssertMoves(StepKind.Land, 2, 4, land);
        Assert.Equal(TravelMode.OnFoot, land.Mode);

        // A rock standing in the water is a blocked tile of water: no way through it, and no setting out onto it
        map.SetSolid(4, 4, true);
        AssertBlocked(Obstacle.Solid, Step(map, 3, 4, East, TravelMode.Surfing));
        map.SetSolid(4, 3, true);
        Assert.False(FieldMovement.CanStartSurf(map, 4, 2, South, new Walker()));
        Assert.True(FieldMovement.CanStartSurf(map, 3, 2, South, new Walker()));
        // Nor is a tree on the shore a place to land
        map.SetGroundTile(2, 4, TileType.Tree, isSolid: true);
        AssertBlocked(Obstacle.Solid, Step(map, 3, 4, West, TravelMode.Surfing));

        // Nobody is carried onto the water over a ledge, or from two tiles up
        var cut = Pond();
        for (int y = 0; y < 9; y++)
            for (int x = 0; x < 9; x++)
                cut.SetHeight(x, y, cut.GetGroundTile(x, y) == TileType.Water ? 0f : 2f);
        Assert.False(FieldMovement.CanStartSurf(cut, 2, 4, East, new Walker(Height: 2f)));
    }

    [Fact]
    public void PuddlesAndShallowWaterAreWalkedThrough()
    {
        var map = Lawn();
        foreach (var behaviour in new[] { TileBehavior.Puddle, TileBehavior.StillPuddle, TileBehavior.ShallowWater })
        {
            map.SetBehaviour(5, 4, behaviour);
            Assert.False(TileBehaviors.IsSurfable(behaviour));
            AssertMoves(StepKind.Walk, 5, 4, Step(map, 4, 4, East));
            Assert.True(map.IsWalkable(5, 4));
            Assert.False(FieldMovement.CanStartSurf(map, 4, 4, East, new Walker()));
        }
    }

    [Fact]
    public void AWaterfallIsClimbedOnceAskedAndRiddenDownWithTheMove()
    {
        // A river down the middle column with a fall of three tiles in it: the water above is three tiles higher
        var map = Lawn(5, 12);
        for (int y = 0; y < 12; y++)
        {
            map.SetGroundTile(2, y, TileType.Water);
            map.SetHeight(2, y, y < 4 ? 3f : 0f);
            if (y is >= 4 and <= 6) map.SetBehaviour(2, y, TileBehavior.Waterfall);
        }

        // On foot it is water like any other, and nobody sets out onto the fall itself
        map.SetGroundTile(1, 5, TileType.Grass);
        AssertBlocked(Obstacle.Water, Step(map, 1, 5, East));
        Assert.False(FieldMovement.CanStartSurf(map, 1, 5, East, new Walker()));

        // From below: not without the move, and not by swimming into it with the move either: up it goes once the
        // player has said yes to Waterfall (a climb they chose), to the water above
        AssertBlocked(Obstacle.Waterfall, Step(map, 2, 7, North, TravelMode.Surfing));
        AssertBlocked(Obstacle.Waterfall, Step(map, 2, 7, North, TravelMode.Surfing, FieldMoves.Waterfall));
        AssertBlocked(Obstacle.Waterfall, Step(map, 2, 7, North, TravelMode.Surfing, climbing: true));
        var up = Step(map, 2, 7, North, TravelMode.Surfing, FieldMoves.Waterfall, climbing: true);
        AssertMoves(StepKind.Climb, 2, 3, up);
        Assert.Equal((3f, TravelMode.Surfing), (up.Height, up.Mode));

        // From above: the water carries down whoever has a Pokémon that knows the move, unasked (ov5_021E04A8);
        // without one the fall is a wall
        AssertBlocked(Obstacle.Waterfall, Step(map, 2, 3, South, TravelMode.Surfing, height: 3f));
        var down = Step(map, 2, 3, South, TravelMode.Surfing, FieldMoves.Waterfall, height: 3f);
        AssertMoves(StepKind.Climb, 2, 7, down);
        Assert.Equal(0f, down.Height);

        // Never sideways
        map.SetGroundTile(1, 5, TileType.Water);
        AssertBlocked(Obstacle.Waterfall, Step(map, 1, 5, East, TravelMode.Surfing, FieldMoves.Waterfall, climbing: true));

        // A fall with rock above it leads nowhere
        map.SetSolid(2, 3, true);
        AssertBlocked(Obstacle.Waterfall, Step(map, 2, 7, North, TravelMode.Surfing, FieldMoves.Waterfall, climbing: true));
    }

    // ------------------------------------------------------------------ rock faces

    [Fact]
    public void ARockFaceIsClimbedAlongItsGrainWithRockClimb()
    {
        // A face three tiles deep running north to south, between low ground to the south and a ledge four tiles up
        var map = Lawn(5, 9);
        for (int y = 0; y < 9; y++)
            for (int x = 0; x < 5; x++)
                map.SetHeight(x, y, y <= 2 ? 4f : 0f);
        for (int y = 3; y <= 5; y++)
        {
            map.SetGroundTile(2, y, TileType.Rock, isSolid: true);
            map.SetBehaviour(2, y, TileBehavior.RockClimbNorthSouth);
        }

        // Walking into it is a bump, move or no move: it is climbed once the player has said yes to Rock Climb
        AssertBlocked(Obstacle.RockFace, Step(map, 2, 6, North));
        AssertBlocked(Obstacle.RockFace, Step(map, 2, 6, North, moves: FieldMoves.RockClimb));
        AssertBlocked(Obstacle.RockFace, Step(map, 2, 6, North, climbing: true));
        var up = Step(map, 2, 6, North, moves: FieldMoves.RockClimb, climbing: true);
        AssertMoves(StepKind.Climb, 2, 2, up);
        Assert.Equal(4f, up.Height);
        var down = Step(map, 2, 2, South, moves: FieldMoves.RockClimb, height: 4f, climbing: true);
        AssertMoves(StepKind.Climb, 2, 6, down);
        Assert.Equal(0f, down.Height);

        // Not across its grain, and not on wheels
        AssertBlocked(Obstacle.RockFace, Step(map, 1, 4, East, moves: FieldMoves.RockClimb, climbing: true));
        AssertBlocked(Obstacle.RockFace, Step(map, 2, 6, North, TravelMode.Cycling, FieldMoves.RockClimb, climbing: true));
        Assert.True(FieldMovement.IsRockFace(TileBehavior.RockClimbEastWest, East));
        Assert.False(FieldMovement.IsRockFace(TileBehavior.RockClimbEastWest, North));

        // With someone standing at the top there is nowhere to climb to
        map.NPCs.Add(new NPC { Name = "Hiker", GridX = 2, GridY = 2 });
        AssertBlocked(Obstacle.RockFace, Step(map, 2, 6, North, moves: FieldMoves.RockClimb, climbing: true));
    }

    // ------------------------------------------------------------------ ice and moving floors

    [Fact]
    public void IceCarriesAWalkerOnUntilSomethingStopsThem()
    {
        Assert.Equal(East, FieldMovement.Carries(TileBehavior.Ice, East));
        Assert.Equal(North, FieldMovement.Carries(TileBehavior.Ice, North));
        Assert.Null(FieldMovement.Carries(TileBehavior.None, East));
        Assert.Null(FieldMovement.Carries(TileBehavior.TallGrass, East));

        // A sheet of ice from x = 2 to x = 6, with a rock on it at (5, 2)
        var map = Lawn();
        for (int y = 1; y <= 7; y++)
            for (int x = 2; x <= 6; x++)
                map.SetGroundTile(x, y, TileType.Ice);
        map.SetGroundTile(5, 2, TileType.Rock, isSolid: true);

        // Across the open sheet: one step on, and the ice does the rest, out onto the lawn beyond
        var player = new Player(1, 4);
        Walk(player, map, East);
        Assert.Equal((7, 4), (player.GridX, player.GridY));
        Assert.False(player.IsSliding);

        // Toward the rock: stopped on the tile before it, where a turn can be made
        player.SetPosition(1, 2, East);
        Walk(player, map, East);
        Assert.Equal((4, 2), (player.GridX, player.GridY));
        Walk(player, map, South);
        Assert.Equal((4, 8), (player.GridX, player.GridY));
    }

    [Fact]
    public void AMovingFloorCarriesItsOwnWay()
    {
        Assert.Equal(East, FieldMovement.Carries(TileBehavior.SlideEast, North));
        Assert.Equal(West, FieldMovement.Carries(TileBehavior.SlideWest, North));
        Assert.Equal(North, FieldMovement.Carries(TileBehavior.SlideNorth, East));
        Assert.Equal(South, FieldMovement.Carries(TileBehavior.SlideSouth, East));

        // Stepping north onto a belt that runs east: carried along it to its end, then one tile off it
        var map = Lawn();
        for (int x = 2; x <= 5; x++) map.SetBehaviour(x, 4, TileBehavior.SlideEast);
        var player = new Player(2, 5);
        Walk(player, map, North);
        Assert.Equal((6, 4), (player.GridX, player.GridY));

        // A belt that ends at a wall holds its rider against it
        map.SetGroundTile(6, 4, TileType.Tree, isSolid: true);
        player.SetPosition(3, 5, North);
        Walk(player, map, North);
        Assert.Equal((5, 4), (player.GridX, player.GridY));
        Assert.False(player.IsSliding);
    }

    // ------------------------------------------------------------------ snow and mud

    [Fact]
    public void DeepSnowAndMudSlowAWalker()
    {
        var walker = new Walker(Running: true);
        Assert.Equal(Pace.Fast, FieldMovement.PaceOn(TileBehavior.None, walker));
        Assert.Equal(Pace.Fast, FieldMovement.PaceOn(TileBehavior.ShallowSnow, walker));
        Assert.Equal(Pace.Fast, FieldMovement.PaceOn(TileBehavior.ShadedSnow, walker));

        // Nobody runs in deep snow, and each depth is slower than the last
        Assert.Equal(Pace.Walk, FieldMovement.PaceOn(TileBehavior.DeepSnow, walker));
        Assert.Equal(Pace.Slow, FieldMovement.PaceOn(TileBehavior.DeeperSnow, walker));
        Assert.Equal(Pace.Slowest, FieldMovement.PaceOn(TileBehavior.DeepestSnow, walker));
        Assert.Equal(Pace.Walk, FieldMovement.PaceOn(TileBehavior.Mud, walker));
        Assert.Equal(Pace.Slow, FieldMovement.PaceOn(TileBehavior.DeepMud, walker));

        // The pace is that of the tile one stands in, not of the tile ahead
        var map = Lawn();
        map.SetGroundTile(5, 4, TileType.Snow);
        map.SetBehaviour(5, 4, TileBehavior.DeepestSnow);
        Assert.Equal(Pace.Walk, Step(map, 4, 4, East).Pace);
        Assert.Equal(Pace.Slowest, Step(map, 5, 4, East).Pace);

        float last = 0f;
        foreach (var pace in Enum.GetValues<Pace>())
        {
            Assert.True(FieldMovement.TilesPerSecond(pace) > last);
            last = FieldMovement.TilesPerSecond(pace);
        }
        Assert.Equal(4.5f, FieldMovement.TilesPerSecond(Pace.Walk));
        Assert.Equal(8f, FieldMovement.TilesPerSecond(Pace.Fast));
    }

    // ------------------------------------------------------------------ the Bicycle

    [Fact]
    public void ABicycleIsFastAndStaysOffSnowMudAndTheTallestGrass()
    {
        var map = Lawn();
        Assert.Equal(Pace.Fast, Step(map, 4, 4, East, TravelMode.Cycling).Pace);
        Assert.Equal(Pace.Fastest, Step(map, 4, 4, East, TravelMode.Cycling, fastGear: true).Pace);

        foreach (var behaviour in new[]
        {
            TileBehavior.DeepSnow, TileBehavior.DeeperSnow, TileBehavior.DeepestSnow, TileBehavior.ShallowSnow, TileBehavior.VeryTallGrass,
            TileBehavior.Mud, TileBehavior.DeepMud, TileBehavior.MarshGrass, TileBehavior.DeepMarshGrass
        })
        {
            map.SetBehaviour(5, 4, behaviour);
            AssertBlocked(Obstacle.NoBicycles, Step(map, 4, 4, East, TravelMode.Cycling));
            AssertMoves(StepKind.Walk, 5, 4, Step(map, 4, 4, East));
        }

        // Tall grass, sand and the thin snow of Twinleaf Town are ridden over
        foreach (var behaviour in new[] { TileBehavior.TallGrass, TileBehavior.Sand, TileBehavior.ShadedSnow, TileBehavior.Puddle })
        {
            map.SetBehaviour(5, 4, behaviour);
            AssertMoves(StepKind.Walk, 5, 4, Step(map, 4, 4, East, TravelMode.Cycling));
        }
    }

    [Fact]
    public void APlankIsRiddenAlongAndNeverWalked()
    {
        var map = Lawn();
        for (int y = 2; y <= 6; y++) map.SetBehaviour(4, y, TileBehavior.BikeBridgeNorthSouth);

        AssertBlocked(Obstacle.BicyclesOnly, Step(map, 4, 7, North));
        AssertMoves(StepKind.Walk, 4, 6, Step(map, 4, 7, North, TravelMode.Cycling));
        AssertMoves(StepKind.Walk, 4, 5, Step(map, 4, 6, North, TravelMode.Cycling));
        // Onto it from the side is across it
        AssertBlocked(Obstacle.Rail, Step(map, 3, 4, East, TravelMode.Cycling));

        map.SetBehaviour(6, 4, TileBehavior.BikeBridgeEastWestOverWater);
        Assert.False(FieldMovement.BikePlankRunsNorthSouth(TileBehavior.BikeBridgeEastWestOverWater));
        Assert.True(FieldMovement.BikePlankRunsNorthSouth(TileBehavior.BikeBridgeNorthSouthOverSand));
        Assert.Null(FieldMovement.BikePlankRunsNorthSouth(TileBehavior.Bridge));
    }

    [Fact]
    public void ABicyclesRampIsJumpedTheWayItFaces()
    {
        // A ramp facing east, blocked as the original's are, with a gap after it (PlayerAvatar_TileMove_BikeRampEast:
        // JUMP_FARTHER, 4 × 12 = 48 pixels, three tiles, in top gear; JUMP_NEAR_SLOW, one tile, in low)
        var map = Lawn(12, 9);
        map.SetBehaviour(4, 4, TileBehavior.BikeRampEast);
        map.SetSolid(4, 4, true);
        map.SetSolid(5, 4, true);

        AssertBlocked(Obstacle.Ramp, Step(map, 3, 4, East));
        AssertBlocked(Obstacle.Ramp, Step(map, 3, 4, East, running: true));
        AssertMoves(StepKind.Jump, 7, 4, Step(map, 3, 4, East, TravelMode.Cycling, fastGear: true));
        // Low gear lands on the tile after it, which here is blocked: nowhere to land
        AssertBlocked(Obstacle.Ramp, Step(map, 3, 4, East, TravelMode.Cycling));
        map.SetSolid(5, 4, false);
        AssertMoves(StepKind.Jump, 5, 4, Step(map, 3, 4, East, TravelMode.Cycling));

        // From its far end and from the side it is a wall, on a Bicycle too
        AssertBlocked(Obstacle.Ramp, Step(map, 5, 4, West, TravelMode.Cycling, fastGear: true));
        AssertBlocked(Obstacle.Ramp, Step(map, 4, 3, South, TravelMode.Cycling, fastGear: true));

        // A ramp facing west is jumped westward
        map.SetBehaviour(8, 6, TileBehavior.BikeRampWest);
        AssertMoves(StepKind.Jump, 5, 6, Step(map, 9, 6, West, TravelMode.Cycling, fastGear: true));
        Assert.Equal(Direction.Left, FieldMovement.RampDirection(TileBehavior.BikeRampWest));
        Assert.Null(FieldMovement.RampDirection(TileBehavior.TallGrass));
    }

    [Fact]
    public void ThePlayerFliesOffARampAndLandsBeyondIt()
    {
        var map = Lawn(12, 9);
        map.SetBehaviour(4, 4, TileBehavior.BikeRampEast);
        map.SetSolid(4, 4, true);
        var player = new Player(3, 4) { FastGear = true };
        player.SetMode(TravelMode.Cycling);
        Walk(player, map, East);
        Assert.Equal((7, 4), (player.GridX, player.GridY));
        Assert.True(player.JustLanded);
    }

    [Fact]
    public void AMuddySlopeIsOnlyClimbedInTheFastGear()
    {
        // A slope two tiles long, climbed northward
        var map = Lawn();
        map.SetBehaviour(4, 4, TileBehavior.BikeSlopeTop);
        map.SetBehaviour(4, 5, TileBehavior.BikeSlopeBottom);

        AssertBlocked(Obstacle.MudSlope, Step(map, 4, 6, North));
        AssertBlocked(Obstacle.MudSlope, Step(map, 4, 6, North, TravelMode.Cycling));
        AssertMoves(StepKind.Walk, 4, 5, Step(map, 4, 6, North, TravelMode.Cycling, fastGear: true));
        AssertMoves(StepKind.Walk, 4, 4, Step(map, 4, 5, North, TravelMode.Cycling, fastGear: true));
        AssertMoves(StepKind.Walk, 4, 3, Step(map, 4, 4, North, TravelMode.Cycling, fastGear: true));

        // Down it is free for anyone
        AssertMoves(StepKind.Walk, 4, 4, Step(map, 4, 3, South));
        AssertMoves(StepKind.Walk, 4, 5, Step(map, 4, 4, South));
    }

    // ------------------------------------------------------------------ the player plays the rules out

    [Fact]
    public void ThePlayerWalksHopsAndKeepsToTheGroundsHeight()
    {
        var map = Lawn();
        for (int y = 0; y < 9; y++)
            for (int x = 0; x < 9; x++)
                map.SetHeight(x, y, y >= 6 ? 0f : 1f);
        map.SetGroundTile(4, 5, TileType.LedgeDown);
        map.SetHeight(4, 5, 1f);

        var player = new Player(4, 3);
        Assert.Equal(1f, player.HeightOn(map));
        Walk(player, map, South);
        Assert.Equal((4, 4), (player.GridX, player.GridY));

        // The hop: two tiles in one go, in the air on the way, a tile lower at the end
        player.Facing = South;
        player.Advance(1f / 60f, map, South, false, _ => { }, _ => { });
        Assert.True(player.IsMoving && player.IsHoppingLedge);
        for (int i = 0; i < 5; i++) player.Advance(1f / 60f, map, null, false, _ => { }, _ => { });
        Assert.True(player.HopHeight > 0f);
        Assert.InRange(player.HeightOn(map), 0.01f, 0.99f);
        Walk(player, map, South);
        Assert.Equal((4, 6), (player.GridX, player.GridY));
        Assert.Equal(0f, player.HeightOn(map));
        Assert.Equal(0f, player.HopHeight);
        Assert.False(player.IsHoppingLedge);

        // Turning comes before stepping: facing south and pressing east only turns the player
        player.Advance(1f / 60f, map, East, false, _ => { }, _ => { });
        Assert.Equal(East, player.Facing);
        Assert.False(player.IsMoving);

        // Put somewhere else, the player stands on that ground
        player.SetPosition(2, 2, North);
        Assert.Equal(1f, player.HeightOn(map));
    }

    [Fact]
    public void ThePlayerRidesOutOntoWaterAndLandsOnFootAgain()
    {
        var map = Pond();
        var player = new Player(2, 4) { Facing = East };
        Assert.Null(player.Mount);

        // Facing the lawn there is nothing to surf on
        player.Facing = West;
        Assert.False(player.StartSurf(map));
        player.Facing = East;
        Assert.True(player.StartSurf(map));

        // The Pokémon waits on the water while the player hops onto it
        Assert.True(player.IsMoving && player.IsHoppingLedge);
        Assert.Equal((3f, 4f), player.Mount);
        Walk(player, map, East);
        Assert.Equal((3, 4, TravelMode.Surfing), (player.GridX, player.GridY, player.Mode));
        Assert.Equal((3f, 4f), player.Mount);

        // Across the pond, under the player all the way
        Walk(player, map, East);
        Walk(player, map, East);
        Assert.Equal((5, 4, TravelMode.Surfing), (player.GridX, player.GridY, player.Mode));

        // Onto the far shore: the Pokémon stays on its water as the player hops off
        player.Facing = East;
        player.Advance(1f / 60f, map, East, false, _ => { }, _ => { });
        Assert.True(player.IsMoving);
        Assert.Equal((5f, 4f), player.Mount);
        Walk(player, map, East);
        Assert.Equal((6, 4, TravelMode.OnFoot), (player.GridX, player.GridY, player.Mode));
        Assert.Null(player.Mount);

        // A save made on the water wakes up on it; put down on land, the player is on foot
        player.SetPosition(4, 4, South);
        player.SetMode(TravelMode.Surfing);
        player.Advance(1f / 60f, map, null, false, _ => { }, _ => { });
        Assert.Equal(TravelMode.Surfing, player.Mode);
        player.SetPosition(1, 1, South);
        player.Advance(1f / 60f, map, null, false, _ => { }, _ => { });
        Assert.Equal(TravelMode.OnFoot, player.Mode);

        // The Bicycle is for dry land
        Assert.True(player.SetCycling(true));
        Assert.Equal(TravelMode.Cycling, player.Mode);
        Assert.True(player.SetCycling(false));
        player.SetPosition(4, 4, South);
        player.SetMode(TravelMode.Surfing);
        Assert.False(player.SetCycling(true));
    }

    [Fact]
    public void WaterHasItsOwnWildPokemonForWhoeverSurfsOnIt()
    {
        MapDatabase.Initialize();
        var world = MapDatabase.Get("Sinnoh");
        var twinleaf = world.FindArea("twinleaf_town")!;

        // Twinleaf Town's pond, Platinum's table for it
        Assert.Equal(new[] { "Golduck", "Psyduck" }, twinleaf.WaterEncounters.Select(e => e.SpeciesName).Distinct().OrderBy(n => n));
        Assert.Equal(100, twinleaf.WaterEncounters.Sum(e => e.Weight));
        Assert.Equal(10, twinleaf.WaterRate);
        Assert.Empty(twinleaf.WildEncounters);
        Assert.All(twinleaf.WaterEncounters, e => Assert.True(e.MinLevel >= 20 && e.MaxLevel >= e.MinLevel));
        Assert.Equal(30, world.FindArea("route_201")!.LandRate);

        // The pond's water is at (108..115, 891..894): surfing there meets them, walking beside it meets nothing
        (int X, int Y) water = default;
        for (int y = 880; y < 896 && water == default; y++)
            for (int x = 100; x < 124 && water == default; x++)
                if (world.IsDeepWater(x, y) && !world.IsSolid(x, y)) water = (x, y);
        Assert.NotEqual(default, water);
        Assert.Equal("twinleaf_town", world.AreaAt(water.X, water.Y)!.Key);

        var steps = new EncounterSteps();
        var met = new HashSet<string>();
        for (int i = 0; i < 6000; i++)
            if (world.RollWildEncounter(water.X, water.Y, steps, water: true) is { } wild) met.Add(wild.SpeciesName);
        Assert.Equal(new[] { "Golduck", "Psyduck" }, met.OrderBy(n => n));
        Assert.Null(world.RollWildEncounter(water.X, water.Y, new EncounterSteps(), water: false));

        // A small map has no table for water
        Assert.Equal(0, Lawn().WildAt(1, 1, water: true).Rate);
        Assert.Equal(30, Lawn().WildAt(1, 1).Rate);
    }

    [Fact]
    public void AStepInTheGrassMeetsAPokemonAsOftenAsInPlatinum()
    {
        var rng = new Random(20261004);

        // Past the first steps: four attempts in ten get through, and then the place's rate decides
        foreach (var (rate, thick, expected) in new[] { (30, false, 0.12), (30, true, 0.21), (10, false, 0.04), (100, false, 0.40) })
        {
            var steps = new EncounterSteps();
            for (int i = 0; i < 400; i++) steps.Meets(rate, thick, rng);   // use the grace period up
            int met = 0;
            const int trials = 200_000;
            for (int i = 0; i < trials; i++)
                if (steps.Meets(rate, thick, rng)) met++;
            Assert.InRange(met / (double)trials, expected * 0.93, expected * 1.07);
        }

        // Right after a battle or a map change nineteen attempts in twenty fail before any of that
        int early = 0;
        for (int i = 0; i < 200_000; i++)
        {
            var fresh = new EncounterSteps();
            if (fresh.Meets(30, false, rng)) early++;
        }
        Assert.InRange(early / 200_000.0, 0.12 * 0.05 * 0.8, 0.12 * 0.05 * 1.2);

        // The grace period is eight steps less one for every ten of the rate, and a reset starts it again
        var counted = new EncounterSteps();
        var never = new NeverRandom();
        for (int i = 0; i < 5; i++) Assert.False(counted.Meets(30, false, never));   // 8 - 3 steps that the "5 in 100" check stops
        Assert.True(counted.Meets(30, false, new AlwaysRandom()));
        counted.Reset();
        Assert.False(counted.Meets(30, false, never));
        Assert.False(new EncounterSteps().Meets(0, false, new AlwaysRandom()));
    }

    /// <summary>Rolls the highest number every time: nothing that needs luck happens.</summary>
    private sealed class NeverRandom : Random
    {
        public override int Next(int maxValue) => maxValue - 1;
    }

    /// <summary>Rolls zero every time: everything that needs luck happens.</summary>
    private sealed class AlwaysRandom : Random
    {
        public override int Next(int maxValue) => 0;
    }

    [Fact]
    public void ThePartysMovesOpenTheWay()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 30));
        Assert.Equal(FieldMoves.None, FieldMovement.MovesOf(party));

        party.Members[0].Moves.Add(MoveDatabase.Create("Surf"));
        Assert.Equal(FieldMoves.Surf, FieldMovement.MovesOf(party));

        var second = new Pokemon(PokemonDatabase.Get("Bibarel")!, 30);
        second.Moves.Add(MoveDatabase.Create("Waterfall"));
        second.Moves.Add(MoveDatabase.Create("Rock Climb"));
        party.Add(second);
        Assert.Equal(FieldMoves.Surf | FieldMoves.Waterfall | FieldMoves.RockClimb, FieldMovement.MovesOf(party));

        // A Pokémon that has fainted still carries its trainer
        party.Members[0].CurrentHP = 0;
        Assert.True(FieldMovement.MovesOf(party).HasFlag(FieldMoves.Surf));
    }

    /// <summary>
    /// The table of what the game does with each behaviour (<see cref="TileBehaviors.InGame"/>, which
    /// <c>docs/tile-behaviours.md</c> prints) says the truth about the rules: a behaviour it calls plain changes
    /// nothing about a step, and one it says has a rule changes something. So a behaviour can't be forgotten
    /// when a place that uses it is built.
    /// </summary>
    [Fact]
    public void EveryBehaviourIsAccountedFor()
    {
        var all = Enum.GetValues<TileBehavior>();
        var ruled = all.Where(b => TileBehaviors.InGame(b).Support == BehaviourSupport.Ruled).ToHashSet();
        var waiting = all.Where(b => TileBehaviors.InGame(b).Support == BehaviourSupport.Waiting).ToHashSet();
        Assert.True(ruled.Count >= 41, $"only {ruled.Count} behaviours with a rule");
        Assert.All(all, b => Assert.False(string.IsNullOrWhiteSpace(TileBehaviors.InGame(b).Note)));

        // Plain: ground like any other as far as a step goes, where only the blocked flag and the height matter
        var map = Lawn();
        foreach (var behaviour in all)
        {
            if (ruled.Contains(behaviour) || waiting.Contains(behaviour)) continue;
            map.SetBehaviour(5, 4, behaviour);
            var onto = Step(map, 4, 4, East, running: true);
            Assert.True(onto.Kind == StepKind.Walk && onto.Pace == Pace.Fast, $"{behaviour} changes a step onto it and isn't listed as having a rule");
            var off = Step(map, 5, 4, East, running: true);
            Assert.True(off.Kind == StepKind.Walk && off.Pace == Pace.Fast, $"{behaviour} changes a step off it and isn't listed as having a rule");
            Assert.Null(FieldMovement.Carries(behaviour, East));
            AssertMoves(StepKind.Walk, 5, 4, Step(map, 4, 4, East, TravelMode.Cycling));
        }

        // And every one with a rule does change something
        foreach (var behaviour in ruled)
        {
            map.SetBehaviour(5, 4, behaviour);
            bool changes = false;
            foreach (var mode in new[] { TravelMode.OnFoot, TravelMode.Cycling })
                foreach (var dir in new[] { North, South, West, East })
                {
                    var (dx, dy) = FieldMovement.Delta(dir);
                    var onto = Step(map, 5 - dx, 4 - dy, dir, mode, running: true);
                    var off = Step(map, 5, 4, dir, mode, running: true);
                    // A run and a Bicycle in its low gear go at the same pace
                    changes |= onto.Kind != StepKind.Walk || off.Kind != StepKind.Walk || onto.Pace != Pace.Fast || off.Pace != Pace.Fast;
                }
            changes |= FieldMovement.Carries(behaviour, East) != null;
            Assert.True(changes, $"{behaviour} is listed as having a rule and changes nothing");
        }
    }
}
