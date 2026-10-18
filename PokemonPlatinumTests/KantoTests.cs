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

    private static readonly string[] FirstAreas = { "pallet_town", "route_1", "viridian_city", "route_2", "route_22" };

    [Fact]
    public void TheIndexOpensPalletTownRouteOneAndViridianCityWithTheirRooms()
    {
        Assert.Equal("Kanto", Kanto.Index.Region);
        Assert.True(Kanto.Index.BuildingsAtDoors);
        foreach (string key in FirstAreas) Assert.Contains(key, Kanto.Index.Areas);
        Assert.True(Kanto.Index.Areas.Count >= 21, $"only {Kanto.Index.Areas.Count} areas open in Kanto");
        Assert.Contains("viridian_forest", Kanto.Index.Areas);
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
        // Every room is come into, the Gym too: its door is shut by the step before it, which the story opens; and
        // every map besides, Viridian Forest among them (plan 19 · K2), and Route 2's north end through it
        foreach (var entry in Kanto.Index.Maps)
            Assert.True(walked.Keys.Any(m => m.Name == entry.Name), $"nobody walks into {entry.Name}");
        var forest = walked.Single(kv => kv.Key.Name == "ViridianForest");
        Assert.True(forest.Value.Count >= 300, $"only {forest.Value.Count} tiles of Viridian Forest walked");
        Assert.Contains(outside, t => Overworld.AreaAt(t.X, t.Y)?.Key == "route_2" && t.Y < 120);
        // and the house's every way is a way somebody can take: FireRed's third warp for the mat, a row too low on the
        // wall under it, leads nowhere (GbaEvents.NeverTaken), and its tile stays wall
        var home = BuiltMaps.Value["PalletTown_PlayersHouse_1F"];
        Assert.Empty(home.WarpsAt(3, 9));
        Assert.True(home.IsSolid(3, 9));
        Assert.Equal(2, home.Warps.Count(w => w.TargetMap == "Kanto"));
    }

    [Fact]
    public void TheGymsDoorIsShutAsFireRedShutsItUntilSixBadges()
    {
        // The way in is there, and the step before it turns back whoever comes while the door's variable is 0
        var door = Assert.Single(Overworld.Warps, w => w.TargetMap == "ViridianCity_Gym");
        var trigger = Assert.Single(Overworld.TriggersAt(door.SourceX, door.SourceY + 1));
        Assert.Equal(("GymDoorLocked", "VAR_MAP_SCENE_VIRIDIAN_CITY_GYM_DOOR", 0), (trigger.Script, trigger.Variable, trigger.Value));
        Assert.True(FieldMovement.Step(Overworld, door.SourceX, door.SourceY + 1, Direction.Down, new Walker(TravelMode.OnFoot,
            Overworld.HeightAt(door.SourceX, door.SourceY + 1))).Moves, "the player can't be turned back down from the Gym's door");

        // Coming to the city with the Cascade to the Volcano Badges opens it; with one of them missing it stays shut
        int Door(params string[] badges)
        {
            var host = new PokemonPlatinumEngine.Story.HeadlessScriptHost { Region = "Kanto" };
            foreach (var badge in badges) host.Story.GiveBadge("Kanto", badge);
            var library = PokemonPlatinumEngine.Story.ScriptLibrary.Default;
            var runner = new PokemonPlatinumEngine.Story.ScriptRunner(library, host);
            runner.Start(library.Find(PokemonPlatinumEngine.Story.ScriptLibrary.OnEnter, "viridian_city")!);
            runner.RunToEnd();
            return host.Story.Var("VAR_MAP_SCENE_VIRIDIAN_CITY_GYM_DOOR");
        }
        Assert.Equal(0, Door("Boulder", "Cascade", "Thunder", "Rainbow", "Soul", "Marsh"));
        Assert.Equal(1, Door("Cascade", "Thunder", "Rainbow", "Soul", "Marsh", "Volcano"));
    }

    [Fact]
    public void EveryWayIntoARoomLandsOnOpenGroundAndLeadsBack()
    {
        foreach (var room in BuiltMaps.Value.Values.Where(m => m.IsIndoors))
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
    public void EveryRoomIsFurnishedWithoutChangingWhereOneWalks()
    {
        // Plan 19 · K2: a room built from a layout has its walls, a floor, its way back out and its furniture
        foreach (var room in BuiltMaps.Value.Values.Where(m => m.IsIndoors))
        {
            Assert.Contains(room.Warps, w => w.TargetMap == "Kanto" || BuiltMaps.Value.ContainsKey(w.TargetMap));
            Assert.True(room.HasRelief);
            // A room is level: the escalator's foot stood on a step of earth before K2
            float floor = room.HeightAt(room.RoomCorner().Left, room.RoomCorner().Back);
            for (int y = 0; y < room.Height; y++)
                for (int x = 0; x < room.Width; x++) Assert.Equal(floor, room.HeightAt(x, y));
            // What blocks stands on blocked tiles, what is walked onto (a rug, a seat) on open ones; what hangs on the
            // back wall hangs on the row above the floor
            foreach (var prop in room.Props)
                for (int y = prop.Y; y < prop.Y + prop.Depth; y++)
                    for (int x = prop.X; x < prop.X + prop.Width; x++)
                    {
                        if (prop.Type is PropType.Window or PropType.Painting) Assert.Equal(TileType.Wall, room.GetGroundTile(x, y));
                        else if (prop.IsSolid) Assert.True(room.IsSolid(x, y), $"{room.Name}: a {prop.Type} at {x},{y} stands on open floor");
                    }
        }
        PropType[] In(string name) => BuiltMaps.Value[name].Props.Select(p => p.Type).Distinct().ToArray();
        var home = In("PalletTown_PlayersHouse_1F");
        foreach (var kind in new[] { PropType.Table, PropType.Television, PropType.KitchenCounter, PropType.Fridge, PropType.Stairs, PropType.Plant, PropType.Rug, PropType.Window })
            Assert.Contains(kind, home);
        // The rug round the table is one rug under it
        Assert.Single(BuiltMaps.Value["PalletTown_PlayersHouse_1F"].Props, p => p.Type == PropType.Rug);
        Assert.Contains(PropType.HealingMachine, In("ViridianCity_PokemonCenter_1F"));
        Assert.Contains(PropType.Counter, In("ViridianCity_PokemonCenter_1F"));
        Assert.Contains(PropType.Computer, In("ViridianCity_PokemonCenter_1F"));
        Assert.Contains(PropType.StoreShelf, In("ViridianCity_Mart"));
        Assert.Contains(PropType.Bookshelf, In("PalletTown_ProfessorOaksLab"));
        Assert.Contains(PropType.LabMachine, In("PalletTown_ProfessorOaksLab"));
        // The back wall stands behind the floor's first row, which the furniture against it stands on
        var center = BuiltMaps.Value["ViridianCity_PokemonCenter_1F"];
        Assert.Equal(1, center.RoomCorner().Back);
        // Every piece the table names is a kind of this game's, by behaviour or by range
        Assert.Equal(PropType.Bookshelf, MapImporter.Gba.GbaFurniture.Of("MB_BOOKSHELF", "gTileset_Building", 0x033));
        Assert.Equal(PropType.Table, MapImporter.Gba.GbaFurniture.Of("MB_NORMAL", "gTileset_Building", 0x04C));
        Assert.Null(MapImporter.Gba.GbaFurniture.Of("MB_NORMAL", "gTileset_Building", 0x001));
    }

    [Fact]
    public void AKantoTrainerIsKantosOwnFromTheRegionsTable()
    {
        // Plan 18's decision 8, its engine half (plan 19 · K2): a trainer placed in Kanto is found in Kanto's own table
        var forest = BuiltMaps.Value["ViridianForest"];
        var rick = Assert.Single(forest.Everyone, n => n.TrainerData?.Id == "kanto_bug_catcher_rick").TrainerData!;
        Assert.Equal(("Rick", "Bug Catcher"), (rick.Name, rick.TrainerClass));
        var record = TrainerDatabase.Get("kanto_bug_catcher_rick")!;
        Assert.Equal(record.Party.Select(p => (p.Species, p.Level)), rick.Party.Members.Select(p => (p.Species.Name, p.Level)));
        // FireRed's prize: four times the last Pokémon's level times the Bug Catcher's rate of 3
        Assert.Equal(4 * record.Party[^1].Level * 3, rick.PrizeMoney);
        Assert.True(forest.Everyone.Count(n => n.TrainerData != null) >= 5);
        // Where an id meets one of Platinum's, the region's own is the one placed there, and Platinum's stays Platinum's
        Assert.Equal("kanto_youngster_tyler", TrainerDatabase.Get("TRAINER_YOUNGSTER_TYLER", "Kanto")!.Id);
        Assert.Equal("youngster_tyler", TrainerDatabase.Get("TRAINER_YOUNGSTER_TYLER", "Sinnoh")!.Id);
        Assert.Equal("youngster_tyler", TrainerDatabase.Get("youngster_tyler")!.Id);
        Assert.DoesNotContain(TrainerDatabase.All, t => t.Id.StartsWith("kanto_", StringComparison.Ordinal));
        Assert.True(TrainerDatabase.OfRegion("kanto").Count >= 600);
        // Every Kanto trainer's team is of species the game has
        foreach (var t in TrainerDatabase.OfRegion("kanto"))
            foreach (var p in t.Party) Assert.NotNull(PokemonDatabase.Get(p.Species));
    }

    [Fact]
    public void KantosPrizeMoneyAndPersonalitiesAreFireRedsOwn()
    {
        // Youngster Ben: a Rattata and an Ekans at 11, a man's (0x88). The hash adds "BEN" (0xBC + 0xBF + 0xC8 = 579)
        // and "RATTATA" (0xCC + 0xBB + 0xCE + 0xCE + 0xBB + 0xCE + 0xBB = 1383): 1962, so 0x88 + (1962 << 8) = 502408;
        // then "BEN" again and "EKANS" (0xBF + 0xC5 + 0xBB + 0xC8 + 0xCD = 980): 3521, so 0x88 + (3521 << 8) = 901512
        Assert.Equal(new uint[] { 502408, 901512 }, MapImporter.Gba.GbaTables.Personalities("BEN", new[] { "Rattata", "Ekans" }, false, false));
        // A woman's start at 0x78, a double battle's at 0x80
        Assert.Equal(0x78u + (MapImporter.Gba.GbaTables.CharacterSum("ANNE") + MapImporter.Gba.GbaTables.CharacterSum("PIDGEY") << 8),
            MapImporter.Gba.GbaTables.Personalities("ANNE", new[] { "Pidgey" }, false, true)[0]);
        Assert.Equal(0x80u, MapImporter.Gba.GbaTables.Personalities("", new[] { "" }, true, true)[0]);
        Assert.Equal(176, MapImporter.Gba.GbaTables.PrizeMoney("YOUNGSTER", 11, false));
        Assert.Equal(2 * 4 * 20 * 25, MapImporter.Gba.GbaTables.PrizeMoney("LEADER", 20, true));
        Assert.Equal(4 * 10 * 5, MapImporter.Gba.GbaTables.PrizeMoney("NOBODY_KNOWN", 10, false));
        // The file the game reads has them
        var ben = TrainerDatabase.Get("kanto_youngster_ben")!;
        Assert.Equal(176, ben.PrizeMoney);
        Assert.Equal(new uint[] { 502408, 901512 }, ben.Party.Select(p => p.Personality));
    }

    [Fact]
    public void RouteTwoViridianForestAndRouteTwentyTwoAreOpen()
    {
        var forest = BuiltMaps.Value["ViridianForest"];
        var area = forest.AreaAt(29, 58)!;
        Assert.Equal("viridian_forest", area.Key);
        Assert.Equal("kanto/viridian_forest", area.BgmTrack);
        Assert.Contains("Pikachu", area.WildEncounters.Select(e => e.SpeciesName));
        Assert.Contains("Caterpie", area.WildEncounters.Select(e => e.SpeciesName));
        // Its trees are trees, not the grey rock of a metatile nobody named
        int trees = 0, rock = 0; var rocks = new List<string>();
        for (int y = 0; y < forest.Height; y++)
            for (int x = 0; x < forest.Width; x++)
            {
                if (forest.GetGroundTile(x, y) == TileType.Tree) trees++;
                if (forest.GetGroundTile(x, y) == TileType.Rock && forest.AreaAt(x, y)?.Key == "viridian_forest") { rock++; rocks.Add($"{x},{y}"); }
            }
        Assert.True(trees >= 1000, $"only {trees} trees in Viridian Forest");
        Assert.True(rock < 60, $"{rock} tiles of bare rock in Viridian Forest: {string.Join(" ", rocks)}");
        var route22 = Overworld.Areas.Single(a => a.Key == "route_22");
        Assert.Equal("kanto/route3", route22.BgmTrack);
        Assert.Contains("Mankey", route22.WildEncounters.Select(e => e.SpeciesName));
        Assert.Equal("kanto/route1", Overworld.Areas.Single(a => a.Key == "route_2").BgmTrack);
        // Viridian City plays its own song and builds in its own way
        Assert.Equal("kanto/viridian", Overworld.Areas.Single(a => a.Key == "viridian_city").BgmTrack);
        Assert.Equal(Architecture.Evergreen, Overworld.ArchitectureAt(74, 208));
        // The gate to Viridian Forest's north, entered from its north side, is a building and no rock
        Assert.Contains(Overworld.PlacedBuildings, b => b.X0 <= 65 && b.X1 >= 66 && b.Y0 == 114);
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
