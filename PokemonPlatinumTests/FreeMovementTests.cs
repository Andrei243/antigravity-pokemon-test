using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Continuous movement over the grid (plan 30 · FM7 to FM9, stage 2), on maps built in the tests and a player whose
/// place moves <see cref="MovementDirections.Free"/>: the circle against the edges <see cref="FieldMovement.Step"/>
/// answers (a corridor, a closed side, a person, deep water), the hand-offs to the grid (a ledge, a door's warp, ice, a
/// stair, a boulder), the step as a tile entered with its hysteresis and the same count as a cardinal walk, talking ahead
/// along the yaw, a script's walk and a trainer's hold centring the body, the follower's breadcrumbs, the pace by tilt and
/// the held tilt's run, determinism, and nothing reached that the grid doesn't reach. Kalos's own file says
/// <see cref="MovementDirections.Free"/> since the user opened the plan's gate (2026-11-12).
/// </summary>
public class FreeMovementTests
{
    private const float Frame = 1f / 60f;

    private static Map Lawn(int width = 12, int height = 12) => new(width, height) { Name = "FreeLab" };

    private static Player Free(int x, int y, Direction facing = Direction.Down)
    {
        var player = new Player(x, y) { Directions = MovementDirections.Free };
        player.SetPosition(x, y, facing);
        return player;
    }

    // Holds a stick for a number of frames; counts the tiles entered and the warps taken
    private sealed class Walk
    {
        public int Entered;
        public readonly List<Warp> Warps = new();
        public readonly List<(int X, int Y)> Tiles = new();

        public void Hold(Player player, Map map, Vector2 stick, int frames, bool run = false)
        {
            // (a warp taken ends the walk: the game would take the player through it)
            for (int f = 0; f < frames && Warps.Count == 0; f++)
                player.SteerFreely(Frame, map, stick, run, _ => { }, w => Warps.Add(w), () =>
                {
                    Entered++;
                    Tiles.Add((player.GridX, player.GridY));
                    return false;
                });
        }

        public void Idle(Player player, Map map, int frames) => Hold(player, map, Vector2.Zero, frames);
    }

    private static Vector2 Way(float x, float y) => Vector2.Normalize(new Vector2(x, y));

    // ------------------------------------------------------------------ the circle against the grid's edges

    [Fact]
    public void TheBodyMovesBySubTilesAndStopsAgainstAWallTouchingIt()
    {
        var map = Lawn();
        map.SetSolid(8, 5, true);
        var player = Free(5, 5);
        var walk = new Walk();
        walk.Hold(player, map, new Vector2(1, 0), 120);
        // Against the wall east of (7,5): its edge is at 7.5 tiles, the centre a radius short of it
        Assert.Equal(7, player.GridX);
        Assert.Equal(FreeMovement.CentreOf(7) + FreeMovement.Half - FreeMovement.Radius, player.SubX);
        Assert.Equal(FreeMovement.CentreOf(5), player.SubY);
        Assert.Equal(Direction.Right, player.Facing);
        Assert.False(player.IsMoving);
        Assert.True(player.PushedAgainst);
        // Two tiles entered, each once
        Assert.Equal(new[] { (6, 5), (7, 5) }, walk.Tiles);
    }

    [Fact]
    public void ACorridorOneTileWideKeepsTheBodyOnItsLineAndCutsNoCorner()
    {
        var map = Lawn();
        for (int x = 0; x < 12; x++)
        {
            map.SetSolid(x, 4, true);
            map.SetSolid(x, 6, true);
        }
        var player = Free(2, 5);
        var walk = new Walk();
        // Pushed down and right along it, the body slides east and never leaves row 5
        walk.Hold(player, map, Way(1, 1), 90);
        Assert.Equal(5, player.GridY);
        Assert.InRange(player.SubY, FreeMovement.CentreOf(5) - (FreeMovement.Half - FreeMovement.Radius), FreeMovement.CentreOf(5) + FreeMovement.Half - FreeMovement.Radius);
        Assert.True(player.GridX > 4, $"slid only to {player.GridX}");
        Assert.All(walk.Tiles, t => Assert.Equal(5, t.Y));
    }

    [Fact]
    public void ACornerOfAWallIsRoundedNotCut()
    {
        // A wall at (6,4) only: walking north-east from (5,5) the body passes round its corner and never into it
        var map = Lawn();
        map.SetSolid(6, 4, true);
        var player = Free(5, 5);
        var walk = new Walk();
        walk.Hold(player, map, Way(1, -1), 60);
        Assert.DoesNotContain((6, 4), walk.Tiles);
        Assert.NotEqual((6, 4), player.Standing);
        // The corner's point stays a radius from the centre whenever the centre is beside it
        foreach (var tile in walk.Tiles) Assert.False(map.IsSolid(tile.X, tile.Y));
    }

    [Fact]
    public void AClosedSideAPersonAndDeepWaterAreWallsAndASurferGoesFreelyOnWater()
    {
        var map = Lawn();
        map.SetBehaviour(5, 3, TileBehavior.BlockSouth);
        var player = Free(5, 5);
        var walk = new Walk();
        walk.Hold(player, map, new Vector2(0, -1), 90);
        Assert.Equal((5, 4), (player.GridX, player.GridY));

        map = Lawn();
        map.NPCs.Add(new NPC { Name = "Lass", GridX = 5, GridY = 3 });
        player = Free(5, 5);
        walk.Hold(player, map, new Vector2(0, -1), 90);
        Assert.Equal((5, 4), (player.GridX, player.GridY));
        Assert.Equal(FreeMovement.CentreOf(4) - (FreeMovement.Half - FreeMovement.Radius), player.SubY);

        map = Lawn();
        for (int y = 0; y < 12; y++) for (int x = 0; x < 4; x++) map.SetGroundTile(x, y, TileType.Water);
        player = Free(6, 5);
        walk.Hold(player, map, new Vector2(-1, 0), 90);
        Assert.Equal(4, player.GridX);
        Assert.Equal(TravelMode.OnFoot, player.Mode);

        // On the water a surfer goes freely, any way
        player = Free(2, 5);
        player.SetMode(TravelMode.Surfing);
        var before = (player.SubX, player.SubY);
        walk.Hold(player, map, Way(-1, 1), 20);
        Assert.Equal(TravelMode.Surfing, player.Mode);
        Assert.True(player.SubX < before.SubX && player.SubY > before.SubY);
        Assert.False(player.IsMoving);
    }

    // ------------------------------------------------------------------ the hand-offs

    [Fact]
    public void ALedgeIsHandedToTheGridFromTheTilesCentreAndHopped()
    {
        var map = Lawn();
        map.SetBehaviour(5, 6, TileBehavior.LedgeSouth);
        var player = Free(5, 4);
        player.SetOffset(40, 0);
        var walk = new Walk();
        bool hopped = false;
        for (int f = 0; f < 120 && !hopped; f++)
        {
            walk.Hold(player, map, new Vector2(0, 1), 1);
            hopped = player.IsHoppingLedge;
            // Never across the edge slantwise: the body is on the column's centre line by the time it hops
            if (hopped) Assert.Equal(FreeMovement.CentreOf(5), (int)MathF.Round(player.PixelX * 8f));
        }
        Assert.True(hopped);
        walk.Hold(player, map, new Vector2(0, 1), 0);
        while (player.IsMoving) walk.Hold(player, map, Vector2.Zero, 1);
        Assert.Equal((5, 7), (player.GridX, player.GridY));
        Assert.True(player.JustLanded);
        Assert.Equal((0, 0), player.Offset);
    }

    [Fact]
    public void ADoorsWarpIsWalkedIntoFromItsFrontAndTakenAsTheStepEnds()
    {
        var map = Lawn();
        map.Warps.Add(new Warp { SourceX = 5, SourceY = 2, TargetMap = "Elsewhere", TargetX = 1, TargetY = 1 });
        var player = Free(5, 5);
        player.SetOffset(-30, 10);
        var walk = new Walk();
        walk.Hold(player, map, Way(0.1f, -1), 120);
        var warp = Assert.Single(walk.Warps);
        Assert.Equal(0, player.Offset.X);
        Assert.Equal("Elsewhere", warp.TargetMap);
        Assert.Equal((5, 2), (player.GridX, player.GridY));
        // Approached slantwise the door's edge is a wall: no warp is taken from the side
        map = Lawn();
        map.Warps.Add(new Warp { SourceX = 5, SourceY = 2, TargetMap = "Elsewhere", TargetX = 1, TargetY = 1 });
        player = Free(3, 2);
        walk = new Walk();
        walk.Hold(player, map, Way(1, -0.9f), 40);
        Assert.Empty(walk.Warps);
    }

    [Fact]
    public void IceIsTheGridsTheBodySlidesOnAndIsFreeAgainWhereItStops()
    {
        var map = Lawn(16, 12);
        for (int x = 6; x <= 9; x++) map.SetBehaviour(x, 5, TileBehavior.Ice);
        var player = Free(4, 5);
        var walk = new Walk();
        bool slid = false;
        for (int f = 0; f < 240; f++)
        {
            walk.Hold(player, map, new Vector2(1, 0), 1);
            slid |= player.IsSliding;
        }
        Assert.True(slid);
        Assert.True(player.GridX >= 10, $"stopped on the ice at {player.GridX}");
        Assert.False(player.IsSliding);
        // Off the ice the body glides again
        int y0 = player.SubY;
        walk.Hold(player, map, new Vector2(0, -1), 3);
        Assert.NotEqual(y0, player.SubY);
        Assert.True(player.IsGliding);
    }

    [Fact]
    public void AStairIsTheGridsAndHeightsAreTheStepsOwn()
    {
        var map = Lawn();
        for (int y = 0; y < 12; y++) map.SetHeight(7, y, 0.5f, slopeX: 1f);
        for (int y = 0; y < 12; y++) for (int x = 8; x < 12; x++) map.SetHeight(x, y, 1f);
        var player = Free(5, 5);
        var walk = new Walk();
        bool stepped = false;
        for (int f = 0; f < 180; f++)
        {
            walk.Hold(player, map, new Vector2(1, 0), 1);
            stepped |= player.IsMoving;
        }
        Assert.True(stepped, "the stair was never handed to the grid");
        Assert.True(player.GridX >= 8, $"never got up the stair: {player.GridX}");
        Assert.Equal(1f, player.HeightOn(map), 2);
    }

    [Fact]
    public void ABoulderIsPushedWithStrengthAndIsAWallWithout()
    {
        var map = Lawn();
        var boulder = new NPC { Name = "boulder", NpcType = NPC.BoulderType, GridX = 5, GridY = 3 };
        map.NPCs.Add(boulder);
        var player = Free(5, 5);
        var walk = new Walk();
        walk.Hold(player, map, new Vector2(0, -1), 60);
        Assert.Equal((5, 3), (boulder.GridX, boulder.GridY));
        Assert.Equal(4, player.GridY);

        player.PushesBoulders = true;
        for (int f = 0; f < 60 && player.TakePush() == null; f++) walk.Hold(player, map, new Vector2(0, -1), 1);
        Assert.Equal((5, 2), (boulder.GridX, boulder.GridY));
    }

    // ------------------------------------------------------------------ steps, hysteresis and counts

    [Fact]
    public void ATileIsCountedOnlyOnceTheBodyHasLeftTheOneBehindAndBackAndForthCountsNothing()
    {
        Assert.Equal(5, FreeMovement.Counted(5, FreeMovement.CentreOf(5) + FreeMovement.Half + FreeMovement.Hysteresis - 1));
        Assert.Equal(6, FreeMovement.Counted(5, FreeMovement.CentreOf(5) + FreeMovement.Half + FreeMovement.Hysteresis));
        Assert.Equal(4, FreeMovement.Counted(5, FreeMovement.CentreOf(4) - FreeMovement.Hysteresis + 1 - FreeMovement.Half + FreeMovement.Half - 1));
        Assert.Equal(7, FreeMovement.Counted(5, FreeMovement.CentreOf(7)));

        var map = Lawn();
        var player = Free(5, 5);
        var walk = new Walk();
        // Over the edge by a little and back, again and again: nothing is entered
        for (int i = 0; i < 6; i++)
        {
            walk.Hold(player, map, new Vector2(1, 0), 10);
            walk.Hold(player, map, new Vector2(-1, 0), 10);
        }
        Assert.Equal(0, walk.Entered);
    }

    [Fact]
    public void AStraightFreeWalkEntersTheSameTilesAsACardinalWalkAndADiagonalOneCountsOnceATile()
    {
        var map = Lawn(20, 12);
        var free = Free(2, 5);
        var walk = new Walk();
        walk.Hold(free, map, new Vector2(1, 0), 200);
        var grid = new Player(2, 5);
        int entered = 0;
        var tiles = new List<(int, int)>();
        for (int f = 0; f < 600 && grid.GridX < free.GridX; f++)
            grid.Advance(Frame, map, Direction.Right, false, _ => { }, _ => { }, () => { entered++; tiles.Add((grid.GridX, grid.GridY)); return false; });
        Assert.Equal(tiles, walk.Tiles);
        Assert.Equal(entered, walk.Entered);

        // A slant entered tile by tile: each tile counted is next to the last, so it is a tile entered whatever the way
        var slant = Free(2, 2);
        var diagonal = new Walk();
        diagonal.Hold(slant, map, Way(1, 1), 150);
        var last = (2, 2);
        foreach (var t in diagonal.Tiles)
        {
            Assert.InRange(Math.Abs(t.X - last.Item1) + Math.Abs(t.Y - last.Item2), 1, 2);
            last = t;
        }
        Assert.True(diagonal.Entered >= 5);
    }

    [Fact]
    public void TheSameFramesPlayedTwiceEndOnTheSameSubTile()
    {
        (int, int, int, int) Run()
        {
            var map = Lawn(20, 20);
            map.SetSolid(9, 9, true);
            map.NPCs.Add(new NPC { Name = "Lass", GridX = 12, GridY = 7 });
            var player = Free(4, 4);
            var walk = new Walk();
            for (int i = 0; i < 40; i++)
            {
                float angle = i * 0.73f;
                walk.Hold(player, map, new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * (0.3f + (i % 4) * 0.23f), 9, run: i % 3 == 0);
            }
            return (player.GridX, player.GridY, player.SubX, player.SubY);
        }
        Assert.Equal(Run(), Run());
    }

    // ------------------------------------------------------------------ talking, scripts, followers

    [Fact]
    public void SomeoneDiagonallyAheadIsTalkedToAlongTheYaw()
    {
        var map = Lawn();
        var player = Free(5, 5);
        var walk = new Walk();
        // Turned north-east, a tap, and settled back on the tile's centre: the tile ahead along the yaw is the diagonal one
        walk.Hold(player, map, Way(1, -1), 1);
        for (int f = 0; f < 60; f++) player.StandStill(Frame);
        Assert.Equal((0, 0), player.Offset);
        var (ahead, faced) = player.InteractTiles();
        Assert.Equal((6, 4), ahead);
        Assert.Contains(player.Facing, new[] { Direction.Up, Direction.Right });
        Assert.NotEqual(ahead, faced);
        // On the grid both are the tile faced
        var grid = new Player(5, 5);
        grid.SetPosition(5, 5, Direction.Left);
        Assert.Equal(((4, 5), (4, 5)), grid.InteractTiles());
    }

    [Fact]
    public void SomeoneSpokenToDiagonallyIsFacedAndNotTheNearestOfFourWays()
    {
        var map = Lawn();
        var player = Free(5, 5);
        var walk = new Walk();
        walk.Hold(player, map, Way(0, -1), 1);
        for (int f = 0; f < 60; f++) player.StandStill(Frame);
        Assert.Equal(Direction.Up, player.Facing);
        // North-east of the body: the body turned along the diagonal, the facing the rules read and the press's aim kept
        var aimBefore = player.InteractTiles();
        player.TurnToFace(6, 4);
        Assert.Equal(FreeMovement.YawOf(Vector2.Normalize(new Vector2(1, -1))), player.Yaw, 3);
        Assert.Equal(Direction.Up, player.Facing);
        Assert.Equal(aimBefore, player.InteractTiles());
        // It holds while the body stands, and a step lets it go
        for (int f = 0; f < 60; f++) player.StandStill(Frame);
        Assert.Equal(FreeMovement.YawOf(Vector2.Normalize(new Vector2(1, -1))), player.Yaw, 3);
        walk.Hold(player, map, Way(0, 1), 6);
        for (int f = 0; f < 60; f++) player.StandStill(Frame);
        Assert.Equal(Direction.Down, player.Facing);
        Assert.Equal(FreeMovement.YawOf(new Vector2(0, 1)), player.Yaw, 2);
        // From off the tile's centre, held while the talk has the field: the body eases onto the centre and keeps the yaw
        var off = Free(5, 5);
        walk.Hold(off, map, Way(0.4f, 0.9f), 2);
        if (off.IsMoving) for (int f = 0; f < 120 && off.IsMoving; f++) off.StandStill(Frame);
        off.TurnToFace(off.GridX + 1, off.GridY - 1);
        float aimed = off.Yaw;
        for (int f = 0; f < 120; f++) off.StandStill(Frame);
        Assert.Equal((0, 0), off.Offset);
        Assert.Equal(aimed, off.Yaw, 3);
        Assert.Equal(FreeMovement.YawOf(Vector2.Normalize(new Vector2(1, -1))), aimed, 3);
        // On the grid nothing turns
        var grid = new Player(5, 5);
        grid.SetPosition(5, 5, Direction.Left);
        float before = grid.Yaw;
        grid.TurnToFace(6, 4);
        Assert.Equal((Direction.Left, before), (grid.Facing, grid.Yaw));
    }

    [Fact]
    public void AScriptsWalkAndATrainersHoldCentreTheBodyFirst()
    {
        var map = Lawn();
        var player = Free(5, 5);
        player.SetOffset(60, -50);
        // A script's walk: the grid's step, from the centre
        var steps = new PlayerWalk(new[] { Direction.Down, Direction.Down }, fast: false);
        bool movedOffCentre = false;
        for (int f = 0; f < 200 && !steps.IsDone; f++)
        {
            steps.Update(Frame, player, map);
            if (player.IsMoving && player.Offset != (0, 0)) movedOffCentre = true;
        }
        Assert.True(steps.IsDone);
        Assert.False(steps.Blocked);
        Assert.False(movedOffCentre);
        Assert.Equal((5, 7), (player.GridX, player.GridY));
        Assert.Equal((0, 0), player.Offset);

        // Held where they stand (a trainer walking up), the body settles onto its tile's centre
        player.SetOffset(-70, 30);
        for (int f = 0; f < 30; f++) player.StandStill(Frame);
        Assert.Equal((0, 0), player.Offset);
    }

    [Fact]
    public void AFollowerKeepsABodysLengthBehindAlongTheBreadcrumbs()
    {
        var crumbs = new Breadcrumbs();
        for (int x = 0; x <= 1024; x += 16) crumbs.Drop(x, 0);
        Assert.Equal(Breadcrumbs.Most, crumbs.Points.Count);
        Assert.Equal((768, 0), crumbs.Behind(1024, 0, 256));
        // Round a corner, measured along the path
        crumbs.Clear();
        crumbs.Drop(0, 0);
        crumbs.Drop(256, 0);
        crumbs.Drop(256, 128);
        Assert.Equal((128, 0), crumbs.Behind(256, 128, 256));

        var map = Lawn();
        var who = new NPC { Name = "Cheryl", GridX = 4, GridY = 5 };
        map.NPCs.Add(who);
        var follower = new Follower(who, "cheryl");
        var player = Free(5, 5);
        var walk = new Walk();
        for (int f = 0; f < 90; f++)
        {
            walk.Hold(player, map, new Vector2(1, 0), 1);
            follower.Crumbs.Drop(player.SubX, player.SubY);
            follower.FollowCrumbs(player.SubX, player.SubY, Frame);
        }
        float behind = player.SubX / 256f - who.DrawX;
        Assert.InRange(behind, 0.9f, 1.1f);
        Assert.Equal(5, who.GridY);
        Assert.Equal(Direction.Right, who.Facing);
    }

    // ------------------------------------------------------------------ pace, the grid's regions and reach

    [Fact]
    public void AHalfTiltWalksSlowlyAFullOneWalksAndHeldASecondRunsWhereTheSpeedIsAnalog()
    {
        var walker = new Walker();
        Assert.Equal(Pace.Slow, FreeMovement.PaceFor(0.4f, analog: true, TileBehavior.None, walker));
        Assert.Equal(Pace.Walk, FreeMovement.PaceFor(0.4f, analog: false, TileBehavior.None, walker));
        Assert.Equal(Pace.Walk, FreeMovement.PaceFor(1f, analog: true, TileBehavior.None, walker));
        Assert.Equal(Pace.Fast, FreeMovement.PaceFor(1f, analog: true, TileBehavior.None, walker with { Running = true }));

        var map = Lawn(40, 5);
        var player = Free(2, 2);
        player.AnalogSpeed = true;
        var walk = new Walk();
        walk.Hold(player, map, new Vector2(1, 0), 30);
        Assert.False(player.IsRunning);
        walk.Hold(player, map, new Vector2(1, 0), 40);
        Assert.True(player.IsRunning, "a full tilt held a second runs");
        walk.Hold(player, map, new Vector2(0.4f, 0), 2);
        Assert.False(player.IsRunning);
    }

    [Fact]
    public void WhereThePlaceIsNotFreeTheStickIsTheGridsAndTheBodyStaysOnItsCentre()
    {
        var map = Lawn();
        var player = new Player(5, 5) { Directions = MovementDirections.Four };
        var walk = new Walk();
        walk.Hold(player, map, Way(1, 0.2f), 60);
        Assert.Equal((0, 0), player.Offset);
        Assert.False(player.IsGliding);
        Assert.True(player.GridX > 5);

        // A Gym's puzzle keeps its rooms on the grid even in a free region
        Assert.True(player.MovesFreely(map) == false);
        var free = Free(5, 5);
        Assert.True(free.MovesFreely(map));
    }

    [Fact]
    public void FreeMovementReachesNothingTheGridDoesntOnKalossMaps()
    {
        var region = RegionDatabase.Get(RegionDatabase.Kalos)!;
        // Kalos's own file moves freely since the user opened the plan's gate (2026-11-12)
        Assert.Equal(MovementDirections.Free, region.Movement.Directions);
        var kalos = World.LoadAll().Single(w => w.Index.Region == "Kalos");
        var maps = kalos.BuildMaps().ToDictionary(m => m.Name);
        var rooms = MapDatabase.Fresh();
        Map Named(string name) => maps.TryGetValue(name, out var m) ? m : rooms(name);
        var walked = WorldWalk.From(Named, region.Start!);
        var overworld = maps["Kalos"];
        var reach = walked.First(p => p.Key.Name == "Kalos").Value;
        var start = reach.OrderBy(t => (t.X - 255) * (t.X - 255) + (t.Y - 690) * (t.Y - 690)).First();
        var player = Free(start.X, start.Y);
        var walk = new Walk();
        var random = new Random(30);
        for (int i = 0; i < 400; i++)
        {
            double angle = random.Next(16) * Math.PI / 8;
            walk.Hold(player, overworld, new Vector2((float)Math.Sin(angle), (float)Math.Cos(angle)), random.Next(5, 40), run: random.Next(2) == 0);
            if (walk.Warps.Count > 0)
            {
                player.SetPosition(start.X, start.Y, Direction.Down);
                walk.Warps.Clear();
            }
            string where = $"step {i}: {player.GridX},{player.GridY} standing {player.Standing} offset {player.Offset} mode {player.Mode} moving {player.IsMoving} " +
                $"behaviour {overworld.BehaviourAt(player.GridX, player.GridY)} solid {overworld.IsSolid(player.GridX, player.GridY)} last {string.Join(" ", walk.Tiles.TakeLast(4))}";
            Assert.True(reach.Contains((player.GridX, player.GridY)), where);
            Assert.True(reach.Contains(player.Standing), where);
        }
    }

    [Fact]
    public void FreeMovementReachesNothingTheGridDoesntOnAlolasIslands()
    {
        var region = RegionDatabase.Get(RegionDatabase.Alola)!;
        // Alola's own file moves freely since the user's switch of 2026-11-12
        Assert.Equal(MovementDirections.Free, region.Movement.Directions);
        var alola = World.LoadAll().Single(w => w.Index.Region == "Alola");
        var maps = alola.BuildMaps().ToDictionary(m => m.Name);
        var rooms = MapDatabase.Fresh();
        Map Named(string name) => maps.TryGetValue(name, out var m) ? m : rooms(name);
        var walked = WorldWalk.From(Named, region.Start!);
        var random = new Random(31);
        // From before the new home's door (Route 1) and from Iki Town's square
        foreach (var (island, nearX, nearY) in new[] { ("Melemele", 238, 160), ("Melemele", 250, 120) })
        {
            var overworld = maps[island];
            var reach = walked.First(p => p.Key.Name == island).Value;
            var start = reach.Where(t => overworld.IsWalkable(t.X, t.Y) && FreeMovement.Plain(overworld, t.X, t.Y))
                .OrderBy(t => (t.X - nearX) * (t.X - nearX) + (t.Y - nearY) * (t.Y - nearY)).First();
            var player = Free(start.X, start.Y);
            var walk = new Walk();
            for (int i = 0; i < 300; i++)
            {
                double angle = random.Next(16) * Math.PI / 8;
                walk.Hold(player, overworld, new Vector2((float)Math.Sin(angle), (float)Math.Cos(angle)), random.Next(5, 40), run: random.Next(2) == 0);
                if (walk.Warps.Count > 0)
                {
                    player.SetPosition(start.X, start.Y, Direction.Down);
                    walk.Warps.Clear();
                }
                string where = $"{island} step {i}: {player.GridX},{player.GridY} standing {player.Standing} offset {player.Offset} mode {player.Mode} " +
                    $"behaviour {overworld.BehaviourAt(player.GridX, player.GridY)} last {string.Join(" ", walk.Tiles.TakeLast(4))}";
                Assert.True(reach.Contains((player.GridX, player.GridY)), where);
                Assert.True(reach.Contains(player.Standing), where);
            }
        }
    }

    // ------------------------------------------------------------------ the stick's frame and recordings

    [Fact]
    public void TheStickIsQuantisedWhereItIsReadAndTheKeysAreAFullTilt()
    {
        Assert.Equal((0, 0), StickReader.Tilt(0.1f, 0.1f, 0.25f));
        Assert.Equal((127, 0), StickReader.Tilt(1f, 0f, 0.25f));
        Assert.Equal((0, -127), StickReader.Tilt(0f, -1f, 0.25f));
        var (x, y) = StickReader.Tilt(0.6f, 0f, 0.2f);
        Assert.Equal(64, x);
        Assert.Equal(0, y);

        // The keyboard's four ways at full tilt, its diagonals 90 an axis; a frame with a tilt reads its tilt
        Assert.Equal(new Vector2(1, 0), InputManager.StickOf(ActionFrame.Press(GameAction.Right)));
        var slant = InputManager.StickOf(ActionFrame.Press(GameAction.Right, GameAction.Up));
        Assert.Equal(90f / 127f, slant.X, 4);
        Assert.Equal(-90f / 127f, slant.Y, 4);
        var tilted = ActionFrame.Stick(30, -100);
        Assert.Equal(new Vector2(30f / 127f, -100f / 127f), InputManager.StickOf(tilted));
        Assert.True(tilted.IsDown(GameAction.Up));
        Assert.False(tilted.IsDown(GameAction.Right));
        Assert.True(ActionFrame.Stick(80, 80).IsDown(GameAction.Right) && ActionFrame.Stick(80, 80).IsDown(GameAction.Down));
    }

    [Fact]
    public void ARecordingKeepsTheTiltAndAnOlderOneReadsItsDirectionBits()
    {
        var recording = new InputRecording { GameVersion = "t", DataVersion = "d" };
        recording.Frames.Add(new InputFrame(Frame, 1, ActionFrame.Stick(-60, 101)));
        recording.Frames.Add(new InputFrame(Frame, 2, ActionFrame.Press(GameAction.Left)));
        var stream = new MemoryStream();
        recording.Write(stream);
        stream.Position = 0;
        var back = InputRecording.Read(stream);
        Assert.Equal(4, back.Format);
        Assert.Equal(recording.Frames, back.Frames);
        Assert.Equal((sbyte)-60, back.Frames[0].Actions.MoveX);

        var older = new InputRecording { Format = 3, GameVersion = "t", DataVersion = "d" };
        older.Frames.Add(new InputFrame(Frame, 1, ActionFrame.Stick(-60, 101)));
        stream = new MemoryStream();
        older.Write(stream);
        stream.Position = 0;
        var read = InputRecording.Read(stream);
        Assert.Equal(0, read.Frames[0].Actions.MoveX);
        // Read from its bits, as the keyboard's: down and to the left by the stick's eight ways
        Assert.True(InputManager.StickOf(read.Frames[0].Actions).Y > 0f);
    }

    // ------------------------------------------------------------------ the save

    [Fact]
    public void ASaveKeepsTheBodysPlaceBetweenTilesAndAnOlderSaveStandsOnItsCentre()
    {
        var save = new SaveData { PlayerGridX = 10, PlayerGridY = 11, PlayerSubX = 40, PlayerSubY = -90, SaveVersion = SaveData.CurrentSave };
        var back = SaveManager.Read(SaveManager.Write(save))!;
        Assert.Equal((40, -90), (back.PlayerSubX, back.PlayerSubY));
        // Nought is left out of the file, so a save on the grid is what it was
        Assert.DoesNotContain("PlayerSubX", SaveManager.Write(new SaveData()));

        var old = new SaveData { SaveVersion = SaveData.PokemonIdsGiven, PlayerSubX = 12 };
        SaveMigration.Upgrade(old);
        Assert.Equal(SaveData.CurrentSave, old.SaveVersion);
        Assert.Equal(0, old.PlayerSubX);
        Assert.Equal(2, SaveData.FreePositions);

        var player = Free(3, 3);
        player.SetOffset(500, -500);
        Assert.Equal((FreeMovement.Half + FreeMovement.Hysteresis - 1, -(FreeMovement.Half + FreeMovement.Hysteresis - 1)), player.Offset);
        Assert.Equal((3, 3), (player.GridX, player.GridY));
    }

    // ------------------------------------------------------------------ the skates in a free place (plan 30's gate)

    // A lawn with a column of tall grass at x = 7, and a free skater with the skates given
    private static (Map Map, Player Skater) Skating(int x, int y)
    {
        var map = Lawn(14, 14);
        for (int row = 0; row < 14; row++) map.SetGroundTile(7, row, TileType.TallGrass);
        var player = Free(x, y, Direction.Right);
        player.CanSkate = true;
        return (map, player);
    }

    [Fact]
    public void AFreeSkaterSkatesOnTheRunHeldTakesTheSkatesOffForTallGrassAndPutsThemBackPastIt()
    {
        var (map, player) = Skating(2, 5);
        var walk = new Walk();
        var modes = new List<(int X, TravelMode Mode)>();
        for (int f = 0; f < 600 && player.GridX < 11; f++)
        {
            walk.Hold(player, map, new Vector2(1f, 0f), 1, run: true);
            modes.Add((player.GridX, player.Mode));
        }
        Assert.True(player.GridX >= 11, $"stopped at {player.GridX}");
        // Skating on the lawn before the grass, on foot in it, and skating again past it
        Assert.Contains(modes, m => m.X < 7 && m.Mode == TravelMode.Skating);
        Assert.All(modes.Where(m => m.X == 7), m => Assert.Equal(TravelMode.OnFoot, m.Mode));
        Assert.Contains(modes, m => m.X > 7 && m.Mode == TravelMode.Skating);
        // Let go, the skates come off
        walk.Idle(player, map, 120);
        Assert.Equal(TravelMode.OnFoot, player.Mode);
    }

    [Fact]
    public void AFreeSkaterSlantingIntoTallGrassGoesInOnFootRatherThanSlidingAlongIt()
    {
        foreach (float degrees in new[] { 50f, 60f, 70f })
        {
            // Mostly south, a little east: the nearest cardinal is south, along the grass, never into it
            var (map, player) = Skating(5, 1);
            float a = degrees * MathF.PI / 180f;
            var stick = new Vector2(MathF.Cos(a), MathF.Sin(a));
            var walk = new Walk();
            bool skated = false, inGrass = false;
            for (int f = 0; f < 600 && player.GridY < 12; f++)
            {
                walk.Hold(player, map, stick, 1, run: true);
                skated |= player.Mode == TravelMode.Skating;
                if (player.GridX == 7) { inGrass = true; Assert.Equal(TravelMode.OnFoot, player.Mode); }
            }
            Assert.True(skated, $"{degrees}°: never skated");
            Assert.True(inGrass, $"{degrees}°: slid along the grass to {player.GridX},{player.GridY} and never went in");
        }
    }
}
