using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Kanto as FireRed's maps make it (plan 19 · K1): what <c>Data/world/kanto</c> opens, the overworld and its rooms
/// built from the GBA reader's files, where a new game there begins, and what is left of the stand-in. Floors, not
/// counts, wherever a later session opens more (plan 16 · T16).
/// </summary>
public class KantoTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Kanto"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Kanto)!.Start!));

    private static World Kanto => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Kanto"];
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);

    private static readonly string[] FirstAreas = { "pallet_town", "route_1", "viridian_city" };

    [Fact]
    public void TheIndexOpensPalletTownRouteOneAndViridianCityWithTheirRooms()
    {
        Assert.Equal("Kanto", Kanto.Index.Region);
        Assert.True(Kanto.Index.BuildingsAtDoors);
        foreach (string key in FirstAreas) Assert.Contains(key, Kanto.Index.Areas);
        Assert.True(Kanto.Index.Areas.Count >= 13, $"only {Kanto.Index.Areas.Count} areas open in Kanto");
        // Every map the index makes is the region's, and every room is a room
        var kanto = RegionDatabase.Get(RegionDatabase.Kanto)!;
        foreach (var entry in Kanto.Index.Maps)
        {
            Assert.Contains(entry.Name, kanto.Maps);
            Assert.True(BuiltMaps.Value.ContainsKey(entry.Name), $"{entry.Name} isn't built");
            if (entry.Setting == MapSetting.Indoors) Assert.True(BuiltMaps.Value[entry.Name].IsIndoors, $"{entry.Name} is no room");
        }
        // Every open area has its file and its overlay, and its overlay's song exists
        foreach (string key in Kanto.Index.Areas)
        {
            Assert.NotNull(Kanto.Area(key));
            Assert.NotNull(Kanto.Overlay(key));
        }
    }

    [Fact]
    public void AnAreaKeyOfKantoNeverMeetsOneOfSinnohs()
    {
        // FireRed's keys are its maps' names; four would meet Platinum's (victory_road_1f…), and take a kanto_ prefix
        // the day they open (plan 19's design). None of the open ones does
        var sinnoh = World.LoadAll().Single(w => w.Index.Region == "Sinnoh").Index.Areas.ToHashSet();
        Assert.Empty(Kanto.Index.Areas.Where(sinnoh.Contains));
        var names = MapDatabase.MapNames;
        foreach (var entry in Kanto.Index.Maps) Assert.Equal(1, RegionDatabase.All.Count(r => r.Maps.Contains(entry.Name)));
        Assert.Contains("Kanto", names);
    }

    [Fact]
    public void ANewGameInKantoBeginsBeforeThePlayersHouseInPalletTown()
    {
        var start = RegionDatabase.Get(RegionDatabase.Kanto)!.Start!;
        Assert.Equal("Kanto", start.Map);
        Assert.True(Overworld.IsWalkable(start.X, start.Y));
        Assert.Equal("pallet_town", Overworld.AreaAt(start.X, start.Y)?.Key);
        Assert.Equal("Pallet Town", Overworld.DisplayNameAt(start.X, start.Y));
        // The door right behind is the player's house
        Assert.Equal("PalletTown_PlayersHouse_1F", Overworld.GetWarpAt(start.X, start.Y - 1)?.TargetMap);
        Assert.Equal("Kanto", RegionDatabase.Get(RegionDatabase.Kanto)!.OverworldMap);
    }

    [Fact]
    public void TheWalkFromTheStartReachesEveryOpenAreaAndEveryRoom()
    {
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Kanto").Value;
        foreach (string key in FirstAreas)
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        // Every room but the Gym, whose door stays shut until its Leader is back, is come into
        foreach (var entry in Kanto.Index.Maps.Where(e => e.Setting == MapSetting.Indoors && e.Name != "ViridianCity_Gym"))
            Assert.True(walked.Keys.Any(m => m.Name == entry.Name), $"nobody walks into {entry.Name}");
        Assert.DoesNotContain(walked.Keys, m => m.Name == "ViridianCity_Gym");
    }

    [Fact]
    public void EveryWayIntoARoomLandsOnOpenGroundAndLeadsBack()
    {
        foreach (var room in BuiltMaps.Value.Values.Where(m => m.IsIndoors && m.Name != "ViridianCity_Gym"))
            foreach (var warp in room.Warps.Where(w => w.TargetMap == "Kanto"))
            {
                Assert.True(Overworld.IsWalkable(warp.TargetX, warp.TargetY) || Overworld.GetWarpAt(warp.TargetX, warp.TargetY) != null,
                    $"{room.Name}'s way out lands on {warp.TargetX},{warp.TargetY}, which is shut");
                // And back in from there: a door outside leads into this room
                Assert.Contains(Overworld.Warps, w => w.TargetMap == room.Name);
            }
    }

    [Fact]
    public void ARoomIsAsLargeAsItsLayoutAndOfItsKind()
    {
        var home = BuiltMaps.Value["PalletTown_PlayersHouse_1F"];
        // FireRed's layout is 13 by 10; the room reaches one tile past its last floor tile east and south, for the wall
        Assert.InRange(home.Width, 13, 14);
        Assert.InRange(home.Height, 10, 11);
        Assert.Equal(InteriorStyle.House, home.Interior);
        Assert.Equal(InteriorStyle.PokemonCenter, BuiltMaps.Value["ViridianCity_PokemonCenter_1F"].Interior);
        Assert.Equal(InteriorStyle.PokeMart, BuiltMaps.Value["ViridianCity_Mart"].Interior);
        Assert.Equal(InteriorStyle.Lab, BuiltMaps.Value["PalletTown_ProfessorOaksLab"].Interior);
        Assert.Equal(InteriorStyle.House, WorldMapBuilder.RoomStyleOf("ViridianCity_School"));
    }

    [Fact]
    public void PalletTownsHousesAreFoundAtTheirDoorsOnTheGroundBeforeThem()
    {
        var pallet = Overworld.PlacedBuildings.Where(b => Overworld.AreaAt(b.X0, b.Y1)?.Key == "pallet_town").ToList();
        // Two houses and Professor Oak's lab
        Assert.Equal(3, pallet.Count);
        Assert.Single(pallet, b => b.Kind == BuildingKind.Lab);
        foreach (var house in pallet)
        {
            var (doorX, _) = house.Doors.Single();
            float ground = Overworld.HeightAt(doorX, house.Y1 + 1);
            // No house stands on a plinth of the rock its tiles were raised as before anybody knew what they were
            for (int z = house.Y0; z <= house.Y1; z++)
                for (int x = house.X0; x <= house.X1; x++)
                    if (Overworld.IsSolid(x, z)) Assert.Equal(ground, Overworld.HeightAt(x, z));
        }
        Assert.Equal(Architecture.Clapboard, Overworld.ArchitectureAt(70, 272));
    }

    [Fact]
    public void ASignpostIsReadAndStandsWhereTheOriginalHasIt()
    {
        // The town's sign, on FireRed's MB_SIGNPOST tile: a signpost of ours with our words
        Assert.Equal(TileType.Signpost, Overworld.GetGroundTile(69, 271));
        Assert.StartsWith("Pallet Town", Overworld.GetSignboardAt(69, 271));
        Assert.Equal("{player}'s House", Overworld.GetSignboardAt(64, 267));
        // A sign in a room is read where it is, and stays what the room has there
        var room = BuiltMaps.Value["PalletTown_PlayersHouse_2F"];
        Assert.Contains("PC", room.GetSignboardAt(1, 1));
        Assert.NotEqual(TileType.Signpost, room.GetGroundTile(1, 1));
    }

    [Fact]
    public void RouteOneHasPidgeyAndRattataInItsGrass()
    {
        var route = Overworld.Areas.Single(a => a.Key == "route_1");
        Assert.Equal(new[] { "Pidgey", "Rattata" }, route.WildEncounters.Select(e => e.SpeciesName).Distinct().OrderBy(s => s));
        Assert.True(route.LandRate > 0);
        Assert.Equal("Route 1", route.DisplayName);
        Assert.Equal("kanto/route1", route.BgmTrack);
        // Its tall grass is where the player meets them
        int grass = 0;
        for (int y = 0; y < Overworld.Height; y++)
            for (int x = 0; x < Overworld.Width; x++)
                if (Overworld.AreaAt(x, y) == route && Overworld.BehaviourAt(x, y) == TileBehavior.TallGrass) grass++;
        Assert.True(grass >= 100, $"only {grass} tiles of tall grass on Route 1");
    }

    [Fact]
    public void MomSeesToTheTeamAndAWhiteOutWakesBesideHer()
    {
        var home = BuiltMaps.Value["PalletTown_PlayersHouse_1F"];
        var mom = Assert.Single(home.Everyone, n => n.IsHealingNurse);
        var (x, y, _) = GameEngine.HomeSpot(mom);
        Assert.True(home.IsWalkable(x, y), $"nobody can wake at {x},{y}");
        // The region's first spawn is that room, which a white-out in Kanto goes to
        var first = SpawnLocations.Of(RegionDatabase.Get(RegionDatabase.Kanto)!)[0];
        Assert.Equal("PalletTown_PlayersHouse_1F", first.Room);
        Assert.Equal("pallet_town", first.Area);
    }

    [Fact]
    public void ASaveFromTheStandInWakesInPalletTownOnTheMapOfKanto()
    {
        var outside = new SaveData { CurrentMapName = "PalletTown", PlayerGridX = 9, PlayerGridY = 9, WorldVersion = SaveData.RebuiltHome };
        Assert.Equal(new MapSpot("Kanto", 66, 268), outside.Place());
        var inside = new SaveData { CurrentMapName = "PalletPlayerHouse", PlayerGridX = 4, PlayerGridY = 6, WorldVersion = SaveData.RebuiltHome };
        var spot = inside.PlaceOn(MapNamed);
        Assert.Equal("PalletTown_PlayersHouse_1F", spot.Map);
        Assert.True(MapNamed(spot.Map).IsWalkable(spot.X, spot.Y));
        // Today's saves are made on today's maps and stay where they are
        var today = new SaveData { CurrentMapName = "Kanto", PlayerGridX = 72, PlayerGridY = 240, WorldVersion = SaveData.CurrentWorld };
        Assert.Equal(new MapSpot("Kanto", 72, 240), today.Place());
        Assert.Equal(SaveData.ImportedKanto, SaveData.CurrentWorld);
    }

    [Fact]
    public void KantosFirstCoverRowsSortTheGroundOfItsFirstTowns()
    {
        // A metatile's look by its tileset's ranges, where neither its behaviour nor a label says (plan 19 · K1)
        Assert.Equal(TerrainCover.Tree, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_General", 0x014));
        Assert.Equal(TerrainCover.Grass, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_General", 0x008));
        Assert.Equal(TerrainCover.Path, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_General", 0x0DC));
        Assert.Equal(TerrainCover.Fence, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_General", 0x0E7));
        Assert.Equal(TerrainCover.Path, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_PalletTown", 0x296));
        // The behaviour still wins, and a house's own metatiles are left for the house to be found at its door
        Assert.Equal(TerrainCover.TallGrass, MapImporter.Gba.GbaCover.Of(TileBehavior.TallGrass, null, "gTileset_General", 0x00D));
        Assert.Equal(TerrainCover.Unknown, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_PalletTown", 0x28A));
        // Every range lies in its tileset's half of the numbers: a primary's under 0x280, a secondary's from it
        foreach (var (tileset, ranges) in MapImporter.Gba.GbaCover.Ranges)
            foreach (var (from, to, _) in ranges)
            {
                Assert.True(from <= to, $"{tileset}: {from:X3} to {to:X3}");
                Assert.True(tileset == "gTileset_General" ? to < 0x280 : from >= 0x280, $"{tileset}: {from:X3} to {to:X3}");
            }
        // On the map: the forest round Pallet Town is trees, and nothing of the town is left as bare rock
        for (int z = 260; z < 280; z++)
            for (int x = 60; x < 84; x++)
                Assert.NotEqual(TileType.Rock, Overworld.GetGroundTile(x, z));
    }

    [Fact]
    public void TheStandInIsAFixtureNow()
    {
        Assert.Null(MapDatabase.TryGet("PalletTown"));
        Assert.Null(MapDatabase.TryGet("PalletPlayerHouse"));
        Assert.Equal("Pallet Town", Fixtures.Map("PalletTown").DisplayName);
        // The link on to Johto leaves from the map of Kanto, where its sailor stands until Vermilion City is open
        var link = RegionDatabase.LinkFrom(RegionDatabase.Kanto)!;
        Assert.Equal("Kanto", link.DepartureMap);
        Assert.Contains(Overworld.Everyone, n => n.IsTransportAttendant);
    }
}
