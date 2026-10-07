using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 01 · M8, part 2: the Spring Path the story reveals, Sendoff Spring, and Turnback Cave, whose doors lead
/// somewhere new each time the player comes into a room (the original's <c>ScrCmd_InitTurnbackCave</c>).
/// </summary>
public class TurnbackCaveTests
{
    // Maps of this class's own: the tests aim doors and hide places
    private static readonly Lazy<Dictionary<string, Map>> Maps = new(() =>
        World.LoadAll().Single(w => w.Index.Region == "Sinnoh").BuildMaps().ToDictionary(m => m.Name));

    /// <summary>A generator whose draws are the numbers given, in order.</summary>
    private sealed class Draws(params int[] values) : Random
    {
        private int next;
        public override int Next(int maxValue) => values[next++] % maxValue;
    }

    [Fact]
    public void ThreePillarsLeadToGiratinaAndThirtyRoomsBackToTheStart()
    {
        // ScrCmd_InitTurnbackCave: pillars first, then the rooms' count, then one draw in four for a pillar's
        // room, then one of the six rooms of the next pillar
        Assert.Equal(TurnbackCave.GiratinaRoom, TurnbackCave.Next(3, 40, new Draws()));
        Assert.Equal(TurnbackCave.Entrance, TurnbackCave.Next(2, 30, new Draws()));
        Assert.Equal(TurnbackCave.PillarRoom, TurnbackCave.Next(0, 0, new Draws(24)));
        Assert.Equal("turnback_cave_pillar_1_room_4", TurnbackCave.Next(0, 0, new Draws(25, 3)));
        Assert.Equal("turnback_cave_pillar_3_room_6", TurnbackCave.Next(2, 10, new Draws(99, 5)));
    }

    [Fact]
    public void TheDoorTheyCameInByLeadsBackToTheEntranceAndTheOthersOn()
    {
        var room = Maps.Value["TurnbackCavePillar1Room1"];
        var host = new HeadlessScriptHost(new StoryState()) { Map = room, Rng = new Draws(50, 2) };
        // In through the south door (warp 2): the player stands one step inside it
        host.PlayerTile = (11, 19);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.In("turnback_cave_pillar_1_room_1", ScriptLibrary.OnEnter)!);
        runner.RunToEnd();

        Assert.Equal(1, host.Story.Var(TurnbackCave.RoomsVisited));
        Assert.Equal("turnback_cave_pillar_1_room_3", host.TurnbackChose);
        Assert.Equal("TurnbackCaveEntrance", room.GetWarpAt(11, 20)!.TargetMap);
        foreach (var (x, y) in new[] { (11, 1), (20, 11), (2, 11) })
            Assert.Equal("TurnbackCavePillar1Room3", room.GetWarpAt(x, y)!.TargetMap);
        // North leads in through the next room's south door, and so on round
        var north = room.GetWarpAt(11, 1)!;
        Assert.Equal(TurnbackCave.WayInto("turnback_cave_pillar_1_room_3", 2)!.TargetY, north.TargetY);
    }

    [Fact]
    public void WalkingOnPastThreePillarsComesToGiratinasRoom()
    {
        // Through the rooms by their scripts, always on through the north door, until the doors lead to Giratina
        var story = new StoryState();
        var rng = new Random(7);
        var map = Maps.Value["TurnbackCaveEntrance"];
        (int X, int Y) tile = (11, 15);
        for (int rooms = 0; rooms < 60 && map.Name != "TurnbackCaveGiratinaRoom"; rooms++)
        {
            var host = new HeadlessScriptHost(story) { Map = map, PlayerTile = tile, Rng = rng };
            string key = map.AreaAt(tile.X, tile.Y)!.Key;
            if (ScriptLibrary.Default.In(key, ScriptLibrary.OnEnter) is { } enter)
            {
                var runner = new ScriptRunner(ScriptLibrary.Default, host);
                runner.Start(enter);
                runner.RunToEnd();
            }
            var north = map.GetWarpAt(11, 1)!;
            map = Maps.Value[north.TargetMap];
            tile = (north.TargetX, north.TargetY);
        }
        Assert.Equal("TurnbackCaveGiratinaRoom", map.Name);
        Assert.Equal(3, story.Var(TurnbackCave.PillarsSeen));
        Assert.True(story.Has("FLAG_FIRST_ARRIVAL_TURNBACK_CAVE"));
    }

    [Fact]
    public void TheSpringPathIsForestUntilTheStoryRevealsIt()
    {
        var sinnoh = Maps.Value["Sinnoh"];
        var place = Assert.Single(sinnoh.HiddenPlaces);
        Assert.NotNull(sinnoh.GetWarpAt(762, 713));
        Assert.Equal("spring_path", sinnoh.AreaAt(762, 713)?.Key);

        // A new game: the original's chunk of forest stands where the path is
        Assert.Single(sinnoh.ApplyHiddenPlaces(_ => 0));
        Assert.True(place.Hidden);
        Assert.Null(sinnoh.GetWarpAt(762, 713));
        Assert.Null(sinnoh.AreaAt(762, 713));
        Assert.True(sinnoh.IsSolid(762, 712));
        Assert.Equal(TileType.Tree, sinnoh.GetGroundTile(762, 712));
        Assert.Empty(sinnoh.ApplyHiddenPlaces(_ => 0));

        // Revealed by the original's number for it, everything is back
        Assert.Single(sinnoh.ApplyHiddenPlaces(v => v == "VAR_HIDDEN_LOCATION_SPRING_PATH" ? 0x0312 : 0));
        Assert.False(place.Hidden);
        Assert.Equal("SendoffSpring", sinnoh.GetWarpAt(762, 713)?.TargetMap);
        Assert.Equal("spring_path", sinnoh.AreaAt(762, 713)?.Key);
    }

    [Fact]
    public void ABallWaitsInGiratinasRoomOnceItIsCaughtAndHangsOnTheRoomsWalked()
    {
        var story = new StoryState();
        story.Set("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_ITEM");
        // Coming through Sendoff Spring brings it back only once Giratina is caught
        void ThroughTheSpring()
        {
            var runner = new ScriptRunner(ScriptLibrary.Default, new HeadlessScriptHost(story));
            runner.Start(ScriptLibrary.Default.In("sendoff_spring", ScriptLibrary.OnEnter)!);
            runner.RunToEnd();
        }
        ThroughTheSpring();
        Assert.True(story.Has("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_ITEM"));
        story.Set("FLAG_CAUGHT_GIRATINA");
        ThroughTheSpring();
        Assert.False(story.Has("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_ITEM"));

        foreach (var (rooms, item) in new[] { (3, "Reaper Cloth"), (8, "Rare Bone"), (20, "Stardust") })
        {
            var room = Maps.Value["TurnbackCaveGiratinaRoom"];
            var ball = room.Everyone.Single(n => n.Key == "item");
            var host = new HeadlessScriptHost(new StoryState()) { Map = room };
            host.Story.SetVar(TurnbackCave.RoomsVisited, rooms);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.In("turnback_cave_giratina_room", "Item")!, ball);
            runner.RunToEnd();
            Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get(item)!));
            Assert.True(host.Story.Has("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_ITEM"));
        }
    }
}
