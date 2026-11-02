using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Kalos's roller skates (plan 23 · Z1; docs/mechanics/rulings.md, "Kalos's roller skates"), on small maps made for them:
/// a skater's pace, the glide of a tile on stopping, the ground skates refuse, the run button that puts them on and
/// where it may.
/// </summary>
public class SkatingTests
{
    private const Direction North = Direction.Up, South = Direction.Down, East = Direction.Right;

    private static Map Lawn(int width = 12, int height = 9) => new(width, height) { Name = "Lab" };

    private static FieldStep Skate(Map map, int x, int y, Direction dir) =>
        FieldMovement.Step(map, x, y, dir, new Walker(TravelMode.Skating, map.HeightAt(x, y)));

    [Fact]
    public void ASkaterGoesAtARunsPace()
    {
        var map = Lawn();
        var step = Skate(map, 4, 4, East);
        Assert.Equal((StepKind.Walk, 5, 4, TravelMode.Skating), (step.Kind, step.X, step.Y, step.Mode));
        Assert.Equal(Pace.Fast, step.Pace);
        Assert.Equal(FieldMovement.TilesPerSecond(Pace.Fast), FieldMovement.TilesPerSecond(FieldMovement.PaceOn(TileBehavior.None, new Walker(TravelMode.Skating))));
    }

    [Fact]
    public void SkatesTakeNoTallGrassAndNoStairs()
    {
        var map = Lawn();
        map.SetGroundTile(5, 4, TileType.TallGrass);
        Assert.Equal(Obstacle.Skates, Skate(map, 4, 4, East).Obstacle);
        // A walker goes into it as ever
        Assert.True(FieldMovement.Step(map, 4, 4, East, new Walker()).Moves);

        // A slope met along its fall is a stair
        map.SetHeight(4, 2, 0.5f, slopeZ: 0.5f);
        Assert.Equal(Obstacle.Skates, Skate(map, 4, 3, North).Obstacle);
        // ... and the side stairs
        map.SetBehaviour(7, 4, TileBehavior.StairsEast);
        Assert.Equal(Obstacle.Skates, Skate(map, 6, 4, East).Obstacle);
        Assert.True(FieldMoveCheck(map, 6, 4, East));
    }

    private static bool FieldMoveCheck(Map map, int x, int y, Direction dir) => FieldMovement.Step(map, x, y, dir, new Walker()).Moves;

    [Fact]
    public void SkatesLetGoOfGlideATileOnAndNeverThroughADoor()
    {
        var map = Lawn();
        var walker = new Walker(TravelMode.Skating);
        Assert.Equal((5, 4), FieldMovement.Coast(map, 4, 4, East, walker) is { } g ? (g.X, g.Y) : (-1, -1));
        // Nothing to glide onto: they stop
        map.SetGroundTile(5, 4, TileType.Tree, isSolid: true);
        Assert.Null(FieldMovement.Coast(map, 4, 4, East, walker));
        // A door ahead is never glided into
        map.Warps.Add(new Warp { SourceX = 4, SourceY = 3, TargetMap = "Lab", TargetX = 1, TargetY = 1 });
        Assert.Null(FieldMovement.Coast(map, 4, 4, North, walker));
        // Only a skater glides
        Assert.Null(FieldMovement.Coast(map, 4, 4, South, new Walker()));
    }

    [Fact]
    public void TheRunButtonPutsTheSkatesOnAndAGlideFollowsTheLastStep()
    {
        var map = Lawn();
        var player = new Player(2, 4) { Facing = East, CanSkate = true };
        // One step held with the run button: a skater's
        player.Advance(1f / 60f, map, East, true, _ => { }, _ => { });
        Assert.True(player.IsMoving);
        Assert.Equal(TravelMode.Skating, player.Mode);
        for (int i = 0; i < 10 && player.IsMoving; i++) player.Advance(1f / 60f, map, East, true, _ => { }, _ => { });
        Assert.Equal(3, player.GridX);
        // Let go: a tile's glide, then the skates are off
        for (int i = 0; i < 60; i++) player.Advance(1f / 60f, map, null, false, _ => { }, _ => { });
        Assert.Equal(4, player.GridX);
        Assert.Equal(TravelMode.OnFoot, player.Mode);
        Assert.False(player.IsMoving);
    }

    [Fact]
    public void WithoutTheSkatesTheRunButtonRunsAndIntoGrassTheSkaterWalks()
    {
        var map = Lawn();
        var runner = new Player(2, 4) { Facing = East };
        runner.Advance(1f / 60f, map, East, true, _ => { }, _ => { });
        Assert.Equal(TravelMode.OnFoot, runner.Mode);
        Assert.True(runner.IsRunning);

        map.SetGroundTile(3, 6, TileType.TallGrass);
        var skater = new Player(2, 6) { Facing = East, CanSkate = true };
        skater.Advance(1f / 60f, map, East, true, _ => { }, _ => { });
        Assert.True(skater.IsMoving);
        Assert.Equal(TravelMode.OnFoot, skater.Mode);
    }

    [Fact]
    public void OnlyARegionThatSkatesLetsThemOnOnceTheStoryHasGivenThem()
    {
        var story = new StoryState();
        var kalos = RegionDatabase.Get(RegionDatabase.Kalos)!;
        Assert.True(kalos.Skates);
        Assert.False(SkateRules.Allowed(kalos, story));
        story.Set(SkateRules.Flag);
        Assert.True(SkateRules.Allowed(kalos, story));
        foreach (var other in RegionDatabase.All.Where(r => r.Id != RegionDatabase.Kalos))
            Assert.False(SkateRules.Allowed(other, story), other.Id);
    }
}
