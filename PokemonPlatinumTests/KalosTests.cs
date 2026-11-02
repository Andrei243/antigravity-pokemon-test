using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Kalos drawn by hand (plan 23 · Z1; the user's decision of 2026-11-02): the region's record, the world files the
/// drawings of <c>tools/MapDraw/kalos</c> make, Vaniville Town, Route 1 and Aquacorde Town built from them, their rooms,
/// where a new game there begins and the walk from the bedroom to the café. Floors, not counts, wherever a later session
/// draws more.
/// </summary>
public class KalosTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Kalos"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Kalos)!.Start!));

    private static World Kalos => LoadedWorld.Value;
    private static Region Region => RegionDatabase.Get(RegionDatabase.Kalos)!;
    private static Map Overworld => BuiltMaps.Value["Kalos"];

    // The rooms are hand-made map files: each test reads its own copy, so nothing another test does to them shows here
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    private static readonly string[] Areas = { "vaniville_town", "kalos_route_1", "aquacorde_town" };

    [Fact]
    public void KalosIsBuiltWithItsFileAStartAndMapsOfItsOwn()
    {
        Assert.True(Region.IsBuilt);
        Assert.Equal(6, Region.Generation);
        Assert.Equal(new[] { "Chespin", "Fennekin", "Froakie" }, Region.Starters);
        Assert.Equal(8, Region.BadgeNames.Count);
        Assert.Equal("Bug", Region.BadgeNames[0]);
        Assert.Equal("Prof. Sycamore", Region.Professor!.Name);
        Assert.Equal("kalos-central", Region.Dex);
        Assert.True(Region.HasStory);
        Assert.True(Region.Skates);
        Assert.False(Region.HasSeasons);
        // The neighbour is the hero the player isn't
        Assert.Equal("Serena", Region.RivalFor(PokemonPlatinumEngine.Core.PlayerLook.Boy));
        Assert.Equal("Calem", Region.RivalFor(PokemonPlatinumEngine.Core.PlayerLook.Girl));
        // Its levels for a crossed team climb from Unova's Champion to its own, and its caps are X and Y's
        Assert.Equal(185, Region.Levels!.Map(2));
        Assert.Equal(205, Region.Levels!.Map(68));
        Assert.Equal(new[] { 12, 25, 32, 34, 37, 42, 48, 59, 68 }, Region.File!.Caps);
        // Every map it makes is its own and no other region's
        foreach (string name in Region.Maps)
        {
            Assert.Equal(1, RegionDatabase.All.Count(r => r.Maps.Contains(name)));
            Assert.NotNull(MapNamed(name));
        }
        // The way from Unova lands here now that Kalos is built
        var story = new StoryState();
        story.CompleteRegion(RegionDatabase.Unova);
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(RegionDatabase.LinkFrom(RegionDatabase.Unova)!, story));
    }

    [Fact]
    public void TheWorldFilesAreWhatTheDrawingsGive()
    {
        string root = MapDraw.Repository.Root();
        var files = MapDraw.Repository.Files(Path.Combine(root, "tools", "MapDraw", "kalos"), Path.Combine(root, "PokemonPlatinumEngine", "Data", "world", "kalos"));
        foreach (var (path, text) in files)
        {
            Assert.True(File.Exists(path), $"{path} is missing: run `dotnet run --project tools/MapDraw -- kalos`");
            Assert.True(File.ReadAllText(path).Replace("\r\n", "\n") == text, $"{path} is not what the drawings give: run `dotnet run --project tools/MapDraw -- kalos`");
        }
        Assert.Empty(MapDraw.Repository.Stale(Path.Combine(root, "PokemonPlatinumEngine", "Data", "world", "kalos"), files));
    }

    [Fact]
    public void ADrawingsCharactersMakeTheTilesTheLegendSays()
    {
        var layout = new MapDraw.Layout { Region = "Test", Width = 1, Height = 1, Areas = { new MapDraw.AreaRect { Key = "here", X = 0, Z = 0, Width = 32, Depth = 32 } } };
        var piece = new MapDraw.Piece { File = "test.txt", X = 2, Z = 3 };
        var world = MapDraw.Drawing.Build(layout, new[] { (piece, new[] { "HHD.", "w~b=" }) });
        var chunk = Assert.Single(world.Chunks);
        Assert.True(chunk.SolidAt(2, 3));
        Assert.Equal(TerrainCover.Unknown, chunk.CoverAt(2, 3));          // a house's body is blocked ground nobody names
        Assert.Equal(TileBehavior.Door, chunk.BehaviourAt(4, 3));
        Assert.Equal(TerrainCover.Building, chunk.CoverAt(4, 3));
        Assert.False(chunk.SolidAt(5, 3));
        Assert.Equal(TileBehavior.TallGrass, chunk.BehaviourAt(2, 4));
        Assert.Equal(TileBehavior.River, chunk.BehaviourAt(3, 4));
        Assert.Equal(TerrainCover.Bridge, chunk.CoverAt(4, 4));
        Assert.Equal(TerrainCover.Paving, chunk.CoverAt(5, 4));
        // Where nothing is drawn the woods stand
        Assert.Equal(TerrainCover.Tree, chunk.CoverAt(0, 0));
        Assert.True(chunk.SolidAt(0, 0));
        Assert.Throws<InvalidDataException>(() => MapDraw.Drawing.Build(layout, new[] { (piece, new[] { "#" }) }));
    }

    [Fact]
    public void VanivilleRouteOneAndAquacordeAreOpenWithTheirHousesSongsAndSigns()
    {
        Assert.Equal(Areas, Kalos.Index.Areas.Take(3));
        foreach (string key in Areas)
        {
            Assert.NotNull(Kalos.Area(key));
            Assert.False(string.IsNullOrEmpty(Kalos.Overlay(key)?.BgmTrack), $"{key} has no song");
        }
        // The keys meet no other region's: a route takes the region's name
        var others = World.LoadAll().Where(w => w.Index.Region != "Kalos").SelectMany(w => w.Index.Areas).ToHashSet();
        Assert.DoesNotContain(Kalos.Index.Areas, others.Contains);
        Assert.Equal("Vaniville Town", Overworld.DisplayNameAt(255, 715));
        Assert.Equal("Route 1", Overworld.DisplayNameAt(255, 690));
        Assert.Equal("Aquacorde Town", Overworld.DisplayNameAt(255, 670));
        // Each house is found at its door, in the town's own way of building
        Assert.True(Overworld.PlacedBuildings.Count(b => b.Y1 >= 700) >= 5, "Vaniville's five houses");
        Assert.True(Overworld.PlacedBuildings.Count(b => b.Y1 is >= 646 and < 682) >= 4, "Aquacorde's four houses");
        Assert.Equal(Architecture.Hamlet, Overworld.ArchitectureAt(255, 715));
        Assert.Equal(Architecture.Canalside, Overworld.ArchitectureAt(255, 670));
        // The river runs under the bridge, which is walked; its water is not
        Assert.True(Overworld.IsWalkable(255, 652));
        Assert.False(Overworld.IsWalkable(245, 652));
        Assert.True(Overworld.IsDeepWater(245, 652));
        // The signs read our words
        Assert.Contains("Vaniville Town", Overworld.Signboards[(258, 708)]);
        Assert.Contains("Aquacorde Town", Overworld.Signboards[(259, 679)]);
        // Route 1 has no wild Pokémon, as X and Y's first route has none
        Assert.Null(Kalos.Area("kalos_route_1")!.Land);
    }

    [Fact]
    public void ANewGameInKalosBeginsInThePlayersRoomAndAChampionArrivesBeforeTheDoor()
    {
        var start = Region.Start!;
        Assert.Equal("VanivilleTown_PlayersHouse_2F", start.Map);
        var bedroom = MapNamed(start.Map);
        Assert.True(bedroom.IsIndoors);
        Assert.True(bedroom.IsWalkable(start.X, start.Y));
        var downstairs = MapNamed("VanivilleTown_PlayersHouse_1F");
        Assert.Contains(downstairs.NPCs, n => n.Name == "Mom" && n.IsHealingNurse);

        var arrival = Region.ArrivalSpot!;
        Assert.Equal("Kalos", arrival.Map);
        Assert.True(Overworld.IsWalkable(arrival.X, arrival.Y));
        Assert.Equal("VanivilleTown_PlayersHouse_1F", Overworld.GetWarpAt(arrival.X, arrival.Y - 1)?.TargetMap);
        foreach (var spawn in Region.File!.Spawns)
        {
            Assert.True(Overworld.IsWalkable(spawn.X, spawn.Y), $"spawn {spawn.Id} stands on a blocked tile");
            Assert.Equal(spawn.Room, Overworld.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
        }
    }

    [Fact]
    public void FromTheBedroomEveryRoomAndTheCafeAreReachedOnFoot()
    {
        var walked = Walked.Value;
        bool Reached(string map, int x, int y) => walked.Any(p => p.Key.Name == map && p.Value.Contains((x, y)));
        // Out of the house, through Vaniville, up Route 1 and over Aquacorde's bridge
        Assert.True(Reached("Kalos", 242, 722), "the player's door");
        Assert.True(Reached("Kalos", 255, 690), "Route 1");
        Assert.True(Reached("Kalos", 251, 658), "the friends' table");
        Assert.True(Reached("Kalos", 255, 650), "Aquacorde's bridge");
        foreach (string room in Region.Maps.Where(m => m != "Kalos"))
            Assert.True(walked.Keys.Any(m => m.Name == room), $"{room} is never reached");
    }
}
