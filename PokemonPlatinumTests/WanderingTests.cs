using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>People who move about of their own accord (plan 02 · S6; the original's <c>unk_0206450C.c</c>).</summary>
public class WanderingTests
{
    private static (Map Map, NPC Npc) Field(string movement, int rangeX, int rangeZ, Direction facing = Direction.Down)
    {
        var map = new Map(20, 20);
        var npc = new NPC { Name = "Walker", GridX = 10, GridY = 10, Facing = facing };
        npc.Movement = PersonMovement.Parse(movement, rangeX, rangeZ, 10, 10, facing);
        map.NPCs.Add(npc);
        return (map, npc);
    }

    /// <summary>Runs the field for a while at sixty frames a second, the player standing far off.</summary>
    private static void Run(Wandering wandering, Map map, float seconds, Action<NPC>? each = null, (int, int)? player = null)
    {
        var rng = new Random(7);
        var at = player ?? (0, 0);
        for (int frame = 0; frame < seconds * 60; frame++)
        {
            wandering.Update(1f / 60f, map, at, at, rng);
            foreach (var npc in map.NPCs) each?.Invoke(npc);
        }
    }

    [Theory]
    [InlineData("none")]
    [InlineData("look_south")]
    [InlineData("berry_soil")]
    [InlineData("follow_player")]
    [InlineData("")]
    public void SomeoneWhoStandsStillHasNoMovement(string movement) =>
        Assert.Null(PersonMovement.Parse(movement, 1, 1, 0, 0, Direction.Down));

    [Fact]
    public void TheMovementTypesReadAsTheOriginalNamesThem()
    {
        Assert.Equal(new[] { Direction.Up, Direction.Left }, PersonMovement.Parse("look_north_and_west", 0, 0, 0, 0, Direction.Down)!.Ways);
        Assert.Equal(new[] { Direction.Down, Direction.Left, Direction.Right }, PersonMovement.Parse("look_south_west_and_east", 0, 0, 0, 0, Direction.Down)!.Ways);
        Assert.Equal(new[] { Direction.Left, Direction.Right }, PersonMovement.Parse("wander_west_and_east", 1, 0, 0, 0, Direction.Down)!.Ways);
        Assert.Equal(4, PersonMovement.Parse("wander_around", 1, 1, 0, 0, Direction.Down)!.Ways.Length);
        var loop = PersonMovement.Parse("walk_west_south_east_north", 2, 5, 0, 0, Direction.Down)!;
        Assert.Equal(PersonMoves.Loop, loop.Kind);
        Assert.Equal(new[] { Direction.Left, Direction.Down, Direction.Right, Direction.Up }, loop.Ways);
        Assert.Equal(PersonMoves.Rotate, PersonMovement.Parse("rotate_counterclockwise", 0, 0, 0, 0, Direction.Down)!.Kind);
    }

    [Fact]
    public void SomeoneWhoLooksAboutTurnsAndNeverSteps()
    {
        var (map, npc) = Field("look_around", 1, 1);
        var wandering = new Wandering();
        var seen = new System.Collections.Generic.HashSet<Direction>();
        Run(wandering, map, 30f, n => seen.Add(n.Facing));
        Assert.Equal((10, 10), (npc.GridX, npc.GridY));
        Assert.Equal(4, seen.Count);
    }

    [Fact]
    public void AWandererKeepsToTheBoxRoundWhereTheyFirstStood()
    {
        var (map, npc) = Field("wander_around", 1, 2);
        var wandering = new Wandering();
        var visited = new System.Collections.Generic.HashSet<(int, int)>();
        Run(wandering, map, 120f, n =>
        {
            Assert.InRange(n.GridX, 9, 11);
            Assert.InRange(n.GridY, 8, 12);
            visited.Add((n.GridX, n.GridY));
        });
        // Two minutes is plenty to have gone most places in a box of fifteen tiles
        Assert.True(visited.Count >= 10, $"Only {visited.Count} tiles visited");
    }

    [Fact]
    public void AWandererWithNoRangeOnAnAxisNeverLeavesIt()
    {
        var (map, _) = Field("wander_west_and_east", 1, 0);
        Run(new Wandering(), map, 60f, n => Assert.Equal(10, n.GridY));
    }

    [Fact]
    public void NobodyStepsOntoThePlayerOrIntoSomethingSolid()
    {
        var (map, npc) = Field("wander_around", 1, 1);
        map.SetSolid(11, 10, true);
        map.SetSolid(10, 11, true);
        Run(new Wandering(), map, 120f, n =>
        {
            Assert.NotEqual((11, 10), (n.GridX, n.GridY));
            Assert.NotEqual((10, 11), (n.GridX, n.GridY));
            Assert.NotEqual((9, 10), (n.GridX, n.GridY));
        }, player: (9, 10));
    }

    [Fact]
    public void ALoopGoesRoundItsCornerOfTheBoxAndComesHome()
    {
        // Kelsey's on Route 205: west two, south five, east back to where she began, north home
        var (map, npc) = Field("walk_west_south_east_north", 2, 5);
        var wandering = new Wandering();
        var path = new System.Collections.Generic.List<(int, int)> { (10, 10) };
        Run(wandering, map, 20f, n =>
        {
            if (path[^1] != (n.GridX, n.GridY)) path.Add((n.GridX, n.GridY));
        });
        var lap = new[] { (10, 10), (9, 10), (8, 10), (8, 11), (8, 12), (8, 13), (8, 14), (8, 15), (9, 15), (10, 15), (10, 14), (10, 13), (10, 12), (10, 11), (10, 10), (9, 10) };
        Assert.Equal(lap, path.Take(lap.Length));
    }

    [Fact]
    public void ALoopMarksTimeWhileItsWayIsBlocked()
    {
        var (map, npc) = Field("walk_west_south_east_north", 2, 5);
        map.SetSolid(9, 10, true);
        Run(new Wandering(), map, 5f);
        Assert.Equal((10, 10), (npc.GridX, npc.GridY));
        Assert.Equal(Direction.Left, npc.Facing);
    }

    [Fact]
    public void ComingBackToAMapPutsEveryoneWhereTheyFirstStood()
    {
        var (map, npc) = Field("wander_around", 2, 2);
        Run(new Wandering(), map, 60f);
        Wandering.SendHome(map);
        Assert.Equal((10, 10), (npc.GridX, npc.GridY));
    }

    [Fact]
    public void TheWorldsPeopleMoveAsTheirAreaFilesSay()
    {
        // Floaroma Town's schoolboy wanders a tile each way; its lass at the flower shop looks west and stays
        var map = MapDatabase.Get("Sinnoh");
        var boy = map.FindPerson("school_kid_m", "floaroma_town")!;
        Assert.Equal(PersonMoves.Wander, boy.Movement!.Kind);
        Assert.Equal((1, 1), (boy.Movement.RangeX, boy.Movement.RangeZ));
        Assert.Null(map.FindPerson("lass_east", "floaroma_town")!.Movement);
    }
}
