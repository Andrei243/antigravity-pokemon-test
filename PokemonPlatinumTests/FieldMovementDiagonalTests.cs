using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>
/// Eight directions on the grid (plan 30 · FM2; the user's answers of 2026-11-02): a diagonal step needs both of its
/// cardinal neighbours open and is refused onto, off or past anything that is more than plain walking; a tile entered
/// is one step; the player faces the nearer cardinal (the part already faced, else the vertical one) until FM3 and FM4
/// draw eight facings; refused, the player slides along the wall; everyone else stays four-way, but a follower treads
/// the player's diagonal; and nothing a diagonal reaches is out of reach of cardinal steps.
/// </summary>
public class FieldMovementDiagonalTests
{
    private static Map Lawn(int width = 9, int height = 9) => new(width, height) { Name = "Lab" };

    private static FieldStep Diagonal(Map map, int x, int y, Heading heading, TravelMode mode = TravelMode.OnFoot, float? height = null) =>
        FieldMovement.StepDiagonal(map, x, y, heading, new Walker(mode, height ?? map.HeightAt(x, y)));

    private static void AssertRefused(FieldStep step, int x, int y)
    {
        Assert.Equal(StepKind.Blocked, step.Kind);
        Assert.Equal((x, y), (step.X, step.Y));
    }

    /// <summary>Holds a heading until the player has stopped on a tile again; returns the seconds the step took.</summary>
    private static float Steer(Player player, Map map, Heading heading, Action<WildEncounterEntry>? onWild = null, Func<bool>? onArrive = null)
    {
        bool started = false;
        float seconds = 0f;
        for (int frame = 0; frame < 600; frame++)
        {
            player.Steer(1f / 60f, map, started ? null : heading, false, onWild ?? (_ => { }), _ => { }, onArrive);
            if (started) seconds += 1f / 60f;
            started |= player.IsMoving;
            if (started && !player.IsMoving) return seconds;
            if (!started && frame > 3) return 0f;
        }
        return seconds;
    }

    // ------------------------------------------------------------------ the rule

    [Fact]
    public void ADiagonalIsOpenWhereBothCardinalNeighboursAreAndCardinalHeadingsAreStepItself()
    {
        var map = Lawn();
        foreach (var heading in Enum.GetValues<Heading>())
        {
            var step = Diagonal(map, 4, 4, heading);
            var (dx, dy) = heading.Delta();
            Assert.Equal((StepKind.Walk, 4 + dx, 4 + dy, Obstacle.None), (step.Kind, step.X, step.Y, step.Obstacle));
            Assert.Equal(Pace.Walk, step.Pace);
            if (heading.Cardinal() is { } d) Assert.Equal(FieldMovement.Step(map, 4, 4, d, new Walker()), step);
        }
        AssertRefused(Diagonal(map, 0, 0, Heading.NW), 0, 0);
        AssertRefused(Diagonal(map, 8, 8, Heading.SE), 8, 8);
    }

    [Fact]
    public void NoCornerIsCutPastAWallATreeAPersonWaterOrACliff()
    {
        // North of (4,4) blocked: the north-east diagonal is refused, the south-east one is not
        var map = Lawn();
        map.SetSolid(4, 3, true);
        AssertRefused(Diagonal(map, 4, 4, Heading.NE), 4, 4);
        AssertRefused(Diagonal(map, 4, 4, Heading.NW), 4, 4);
        Assert.True(Diagonal(map, 4, 4, Heading.SE).Moves);

        // East of it a person: the east diagonals both shut
        map = Lawn();
        map.NPCs.Add(new NPC { Name = "Lass", GridX = 5, GridY = 4 });
        AssertRefused(Diagonal(map, 4, 4, Heading.NE), 4, 4);
        AssertRefused(Diagonal(map, 4, 4, Heading.SE), 4, 4);
        Assert.True(Diagonal(map, 4, 4, Heading.NW).Moves);

        // Someone on the diagonal tile itself stops it as they stop a cardinal step onto them
        map = Lawn();
        map.NPCs.Add(new NPC { Name = "Hiker", GridX = 5, GridY = 3 });
        AssertRefused(Diagonal(map, 4, 4, Heading.NE), 4, 4);

        // Water on a corner: a walker never cuts past it
        map = Lawn();
        map.SetGroundTile(5, 4, TileType.Water);
        AssertRefused(Diagonal(map, 4, 4, Heading.NE), 4, 4);

        // A cliff: the corner a tile and a half up
        map = Lawn();
        map.SetHeight(4, 3, 1.5f);
        AssertRefused(Diagonal(map, 4, 4, Heading.NE), 4, 4);
        // A closed side on the corner's tile, the way the leg crosses it
        map = Lawn();
        map.SetBehaviour(5, 4, TileBehavior.BlockNorth);
        AssertRefused(Diagonal(map, 4, 4, Heading.NE), 4, 4);
    }

    [Fact]
    public void HeightsGoAlongWhereEveryTileOfTheCornerIsWithinAStepButNeverOnStairs()
    {
        // A terrace a tile up north-east: both ways round climb it, so the diagonal does, to its height
        var map = Lawn();
        for (int y = 0; y <= 3; y++) for (int x = 5; x < 9; x++) map.SetHeight(x, y, 1f);
        map.SetHeight(4, 3, 1f);
        map.SetHeight(5, 4, 1f);
        var up = Diagonal(map, 4, 4, Heading.NE);
        Assert.True(up.Moves);
        Assert.Equal(1f, up.Height);

        // A flight of stairs on the corner (a slope) is taken straight, never slantwise
        var stairs = Lawn();
        for (int y = 0; y < 9; y++) stairs.SetHeight(5, y, 0.5f, slopeX: 1f);
        AssertRefused(Diagonal(stairs, 4, 4, Heading.NE), 4, 4);
        // and so are the side stairs of a room
        var side = Lawn();
        side.SetBehaviour(5, 3, TileBehavior.StairsEast);
        AssertRefused(Diagonal(side, 4, 4, Heading.NE), 4, 4);
    }

    [Theory]
    [InlineData(TileBehavior.Ice)]
    [InlineData(TileBehavior.LedgeSouth)]
    [InlineData(TileBehavior.LedgeEast)]
    [InlineData(TileBehavior.SlideEast)]
    [InlineData(TileBehavior.WarpPanel)]
    [InlineData(TileBehavior.Hole)]
    [InlineData(TileBehavior.BikeRampEast)]
    [InlineData(TileBehavior.BikeBridgeNorthSouth)]
    [InlineData(TileBehavior.RockClimbNorthSouth)]
    [InlineData(TileBehavior.CyclingRoadPull)]
    public void NothingThatIsMoreThanWalkingIsCrossedDiagonally(TileBehavior behaviour)
    {
        Assert.True(FieldMovement.RefusesDiagonal(behaviour));
        // On any tile of the corner: under the walker, on either neighbour, or on the diagonal tile
        foreach (var (x, y) in new[] { (4, 4), (4, 3), (5, 4), (5, 3) })
        {
            var map = Lawn();
            map.SetBehaviour(x, y, behaviour);
            AssertRefused(Diagonal(map, 4, 4, Heading.NE), 4, 4);
        }
        Assert.False(FieldMovement.RefusesDiagonal(TileBehavior.None));
        Assert.False(FieldMovement.RefusesDiagonal(TileBehavior.TallGrass));
        Assert.False(FieldMovement.RefusesDiagonal(TileBehavior.Sand));
    }

    [Fact]
    public void ADoorIsNeverWalkedIntoDiagonallyNorAPuzzlesRoomCrossed()
    {
        var map = Lawn();
        map.Warps.Add(new Warp { SourceX = 5, SourceY = 3, TargetMap = "Elsewhere", TargetX = 1, TargetY = 1 });
        AssertRefused(Diagonal(map, 4, 4, Heading.NE), 4, 4);
        Assert.True(FieldMovement.Step(map, 5, 4, Direction.Up, new Walker()).Moves);
    }

    [Fact]
    public void ASurferCrossesOpenWaterDiagonallyButNeverLandsOrSetsOutSlantwise()
    {
        var map = Lawn();
        for (int y = 2; y <= 6; y++) for (int x = 2; x <= 6; x++) map.SetGroundTile(x, y, TileType.Water);
        Assert.True(Diagonal(map, 4, 4, Heading.NE, TravelMode.Surfing).Moves);
        Assert.Equal(TravelMode.Surfing, Diagonal(map, 4, 4, Heading.NE, TravelMode.Surfing).Mode);
        // From the water's last tile onto the shore at a slant: a landing is a cardinal step alone
        AssertRefused(Diagonal(map, 6, 6, Heading.SE, TravelMode.Surfing), 6, 6);
        // From the shore onto the water: the walker meets water
        AssertRefused(Diagonal(map, 1, 1, Heading.SE), 1, 1);
    }

    [Fact]
    public void ABicycleAndSkatesTakeADiagonal()
    {
        var map = Lawn();
        var bike = Diagonal(map, 4, 4, Heading.SW, TravelMode.Cycling);
        Assert.True(bike.Moves);
        Assert.Equal(TravelMode.Cycling, bike.Mode);
        var skates = Diagonal(map, 4, 4, Heading.SW, TravelMode.Skating);
        Assert.True(skates.Moves);
        Assert.Equal(Pace.Fast, skates.Pace);
        // But skates don't cut through tall grass on the corner
        map.SetGroundTile(3, 4, TileType.TallGrass);
        AssertRefused(Diagonal(map, 4, 4, Heading.SW, TravelMode.Skating), 4, 4);
    }

    // ------------------------------------------------------------------ the player

    [Fact]
    public void ThePlayerStepsDiagonallyInRootTwoOfAStepsTimeAndFacesTheNearerCardinal()
    {
        var map = Lawn();
        var player = new Player(4, 4) { Directions = MovementDirections.Eight };
        player.Facing = Direction.Up;
        Assert.Null(player.DiagonalStep);
        float straight = Steer(player, map, Heading.N);
        Assert.Equal((4, 3), (player.GridX, player.GridY));

        float slant = Steer(player, map, Heading.NE);
        Assert.Equal((5, 2), (player.GridX, player.GridY));
        // Already facing north, the player keeps facing north (the part already faced)
        Assert.Equal(Direction.Up, player.Facing);
        Assert.Null(player.DiagonalStep);
        Assert.InRange(slant / straight, 1.3f, 1.55f);

        // Facing east, a north-east step keeps the body turned east
        player.Facing = Direction.Right;
        Steer(player, map, Heading.NE);
        Assert.Equal((6, 1), (player.GridX, player.GridY));
        Assert.Equal(Direction.Right, player.Facing);

        // Facing neither part, the first frame turns to the vertical part (the grid's tie) and the step follows
        player.Facing = Direction.Up;
        Assert.Equal(Direction.Down, Player.DiagonalFacing(Heading.SW, Direction.Up));
        Assert.Equal(Direction.Left, Player.DiagonalFacing(Heading.SW, Direction.Left));
        player.Steer(1f / 60f, map, Heading.SW, false, _ => { }, _ => { });
        Assert.Equal(Direction.Down, player.Facing);
        Assert.False(player.IsMoving);
        Steer(player, map, Heading.SW);
        Assert.Equal((5, 2), (player.GridX, player.GridY));
        Assert.Equal(Direction.Down, player.Facing);
    }

    [Fact]
    public void ADiagonalIsOneStepOneArrivalAndOneRollOfTheGrass()
    {
        var map = Lawn();
        var player = new Player(4, 4) { Directions = MovementDirections.Eight };
        player.Facing = Direction.Up;
        int arrivals = 0;
        Steer(player, map, Heading.NE, onArrive: () => { arrivals++; return false; });
        Assert.Equal(1, arrivals);
        Assert.Equal((5, 3), (player.GridX, player.GridY));
    }

    [Fact]
    public void RefusedThePlayerSlidesAlongTheWallAndWhereFourWaysAreWalkedADiagonalIsItsVerticalPart()
    {
        // A wall to the north: holding north-east walks east along it
        var map = Lawn();
        for (int x = 0; x < 9; x++) map.SetSolid(x, 3, true);
        var player = new Player(4, 4) { Directions = MovementDirections.Eight };
        player.Facing = Direction.Up;
        Steer(player, map, Heading.NE);
        Assert.Equal((5, 4), (player.GridX, player.GridY));
        Assert.Equal(Direction.Right, player.Facing);

        // Where the place moves four ways (Sinnoh, or the options' FOUR WAYS) a diagonal is the grid's own answer
        var grid = new Player(4, 5) { Directions = MovementDirections.Four };
        grid.Facing = Direction.Down;
        Steer(grid, Lawn(), Heading.SE);
        Assert.Equal((4, 6), (grid.GridX, grid.GridY));
    }

    // ------------------------------------------------------------------ everyone else

    [Fact]
    public void AFollowerTreadsTheDiagonalThePlayerTrodNeverAHop()
    {
        var who = new NPC { Name = "Cheryl", GridX = 4, GridY = 5 };
        var follower = new Follower(who, "cheryl");
        // The player at (4,4) steps north-east to (5,3): the follower comes up to (4,4), then the diagonal on to (5,3)
        follower.PlayerLeft(4, 4, (5, 3));
        // (a person's tile is where their step ends at once; the step is over when the offset is gone)
        for (int i = 0; i < 60 && ((who.GridX, who.GridY) != (4, 4) || who.StepOffsetY != 0f); i++) follower.Update(1f / 60f, 4.5f);
        Assert.Equal((4, 4, 0f), (who.GridX, who.GridY, who.StepOffsetY));
        follower.PlayerLeft(5, 3, (6, 2));
        int frames = 0;
        for (; frames < 120; frames++)
        {
            follower.Update(1f / 60f, 4.5f);
            Assert.False(follower.Hopping);
            if ((who.GridX, who.GridY) == (5, 3) && who.StepOffsetX == 0f && who.StepOffsetY == 0f) break;
        }
        Assert.Equal((5, 3), (who.GridX, who.GridY));
        Assert.Equal(Direction.Up, who.Facing);
        // About √2 of a step's time at the player's pace (a step at 4.5 tiles a second is 13.3 frames)
        Assert.InRange(frames, 15, 22);
    }

    [Fact]
    public void TheBotsRoutesStayFourWayAndAreWalkedWhereEightWaysAreAllowed()
    {
        var map = Lawn(12, 12);
        for (int y = 2; y < 10; y++) map.SetSolid(6, y, true);
        var route = Policy.Route(map, 1, 1, new Walker(), (x, y) => (x, y) == (10, 10), 10, 10);
        Assert.NotNull(route);
        var player = new Player(1, 1) { Directions = MovementDirections.Eight };
        foreach (var way in route!)
        {
            player.Facing = way;
            bool started = false;
            for (int frame = 0; frame < 120 && !(started && !player.IsMoving); frame++)
            {
                player.Advance(1f / 60f, map, started ? null : way, false, _ => { }, _ => { });
                started |= player.IsMoving;
                Assert.Null(player.DiagonalStep);
            }
        }
        Assert.Equal((10, 10), (player.GridX, player.GridY));
    }

    // ------------------------------------------------------------------ input

    [Fact]
    public void TwoDirectionsHeldAreADiagonalFromTheKeysAndTheStickAlike()
    {
        var bindings = InputBindings.For(ButtonMode.Normal);
        // The stick at a slant is two direction actions, as two keys are: one place reads the pair as a heading
        var stick = bindings.Resolve(_ => false, _ => false, PadSnapshot.None, StickDirs.Up | StickDirs.Right, StickDirs.None);
        Assert.Equal(Heading.NE, Headings.Of(stick));
        var keys = bindings.Resolve(k => k is KeyboardKey.Down or KeyboardKey.Left, _ => false, PadSnapshot.None, StickDirs.None, StickDirs.None);
        Assert.Equal(Heading.SW, Headings.Of(keys));

        // The stick reads eight ways: a 45° push holds both of its parts
        var reader = new StickReader();
        var held = reader.Update(0.7f, -0.7f, 0.3f, out _);
        Assert.Equal(Heading.NE, Headings.FromStick(held));
        // Opposite ways hold no heading
        var both = bindings.Resolve(k => k is KeyboardKey.Left or KeyboardKey.Right, _ => false, PadSnapshot.None, StickDirs.None, StickDirs.None);
        Assert.Null(Headings.Of(both));
    }

    // ------------------------------------------------------------------ reach

    [Fact]
    public void InKalosEveryDiagonalFromATileWalkedReachesATileWalkedByCardinalSteps()
    {
        var world = World.LoadAll().Single(w => w.Index.Region == "Kalos");
        var built = world.BuildMaps().ToDictionary(m => m.Name);
        var rooms = MapDatabase.Fresh();
        Map MapNamed(string name) => built.TryGetValue(name, out var m) ? m : rooms(name);
        var walked = WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Kalos)!.Start!);
        int diagonals = 0;
        foreach (var (map, tiles) in walked)
            foreach (var (x, y) in tiles)
                foreach (var heading in new[] { Heading.NE, Heading.SE, Heading.SW, Heading.NW })
                {
                    var step = FieldMovement.StepDiagonal(map, x, y, heading, new Walker(TravelMode.OnFoot, map.HeightAt(x, y)));
                    if (!step.Moves) continue;
                    diagonals++;
                    Assert.True(tiles.Contains((step.X, step.Y)), $"{map.Name}: a diagonal from {x},{y} reaches {step.X},{step.Y}, which no cardinal walk does");
                }
        Assert.True(diagonals > 1000, $"only {diagonals} diagonals open in Kalos");
    }
}
