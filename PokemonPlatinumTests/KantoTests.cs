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
    public void ANewGameInKantoBeginsInThePlayersRoomAndAChampionArrivesBeforeTheHouse()
    {
        // Since plan 19 · K3 a new game wakes upstairs at home, on FireRed's tile of the bedroom
        var kanto = RegionDatabase.Get(RegionDatabase.Kanto)!;
        var start = kanto.Start!;
        Assert.Equal(new MapSpot("PalletTown_PlayersHouse_2F", 6, 6, Direction.Up), start);
        Assert.True(BuiltMaps.Value[start.Map].IsWalkable(start.X, start.Y));
        Assert.True(kanto.HasStory);
        // Whoever crosses from Sinnoh steps out before the house, the door right behind
        var arrival = kanto.ArrivalSpot!;
        Assert.Equal("Kanto", arrival.Map);
        Assert.True(Overworld.IsWalkable(arrival.X, arrival.Y));
        Assert.Equal("pallet_town", Overworld.AreaAt(arrival.X, arrival.Y)?.Key);
        Assert.Equal("Pallet Town", Overworld.DisplayNameAt(arrival.X, arrival.Y));
        Assert.Equal("PalletTown_PlayersHouse_1F", Overworld.GetWarpAt(arrival.X, arrival.Y - 1)?.TargetMap);
        Assert.Equal("Kanto", kanto.OverworldMap);
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
                        // (A bed lies over the tiles FireRed lets the player climb onto as well: plan 19 · K3)
                        else if (prop.IsSolid && prop.Type != PropType.Bed) Assert.True(room.IsSolid(x, y), $"{room.Name}: a {prop.Type} at {x},{y} stands on open floor");
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
    public void PewterCityRouteThreeAndPewtersRoomsAreOpenAndFurnished()
    {
        // Plan 19 · K3: Pewter City, built in pale stone, with Route 3 east of it and its eight rooms
        foreach (string key in new[] { "pewter_city", "route_3", "pewter_city_gym", "pewter_city_museum_1f", "pewter_city_pokemon_center_1f" })
            Assert.Contains(key, Kanto.Index.Areas);
        var pewter = Overworld.Areas.Single(a => a.Key == "pewter_city");
        Assert.Equal("kanto/viridian", pewter.BgmTrack);
        Assert.Equal(Architecture.Granite, Overworld.ArchitectureAt(70, 80));
        Assert.Equal("kanto/route3", Overworld.Areas.Single(a => a.Key == "route_3").BgmTrack);
        Assert.True(Walked.Value.Keys.Any(m => m.Name == "PewterCity_Gym"), "nobody walks into Pewter's Gym");
        // The Gym's hall: boulders on its blocked tiles (K2 drew them as cabinets) and the two statues by the door
        var gym = BuiltMaps.Value["PewterCity_Gym"];
        var boulders = gym.Props.Where(p => p.Type == PropType.Boulder).ToList();
        Assert.True(boulders.Count >= 8, $"only {boulders.Count} boulders in Pewter's Gym");
        Assert.All(boulders, b => Assert.True(gym.IsSolid(b.X, b.Y), $"a boulder at {b.X},{b.Y} stands on open floor"));
        Assert.Equal(2, gym.Props.Count(p => p.Type == PropType.TrainerStatue));
        Assert.Equal("kanto/gym", gym.Areas.Single().BgmTrack);
        // The bedroom's bed is one bed, three metatiles by three, its foot the one tile FireRed blocks
        var room = BuiltMaps.Value["PalletTown_PlayersHouse_2F"];
        var bed = Assert.Single(room.Props, p => p.Type == PropType.Bed);
        Assert.Equal((1, 4, 3, 3), (bed.X, bed.Y, bed.Width, bed.Depth));
        Assert.DoesNotContain(room.Props, p => p.Type == PropType.Chair);
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

    // ------------------------------------------------------------------ plan 19 · K5, part a: Rock Tunnel to Lavender

    [Fact]
    public void RockTunnelToLavenderIsOpenAndWalkedTo()
    {
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Kanto").Value;
        foreach (string key in new[] { "route_9", "route_10", "lavender_town", "route_8", "route_7" })
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        foreach (string room in new[] { "RockTunnel_1F", "RockTunnel_B1F", "PokemonTower_6F", "UndergroundPath_EastWestTunnel", "Route7_EastEntrance" })
            Assert.True(walked.Keys.Any(m => m.Name == room), $"nobody walks into {room}");
        // Rock Tunnel is dark until Flash is used, as FireRed's map says (requires_flash), and a cave
        Assert.True(BuiltMaps.Value["RockTunnel_1F"].IsDark);
        Assert.True(BuiltMaps.Value["RockTunnel_B1F"].IsDark);
        Assert.False(BuiltMaps.Value["MtMoon_1F"].IsDark);
        // The Power Plant's doors on Route 10, shut until plan 19 · K6, part b, lead into it since
        Assert.Contains(Overworld.Warps, w => w.TargetMap == "PowerPlant");
    }

    [Fact]
    public void LavenderBuildsInLilacAndItsTowerIsATowerOfStoneWithGravestonesInside()
    {
        Assert.Equal(Architecture.Lilac, Overworld.ArchitectureAt(398, 140));
        Assert.Equal(Architecture.Portside, Overworld.ArchitectureAt(283, 220));
        var tower = Overworld.PlacedBuildings.Single(b => b.Kind == BuildingKind.Tower && Overworld.AreaAt(b.X0, b.Y1)?.Key == "lavender_town");
        Assert.Equal(BuildingKind.Tower, tower.Kind);
        var third = BuiltMaps.Value["PokemonTower_3F"];
        Assert.Equal(InteriorStyle.Lab, third.Interior);
        Assert.True(third.Props.Count(p => p.Type == PropType.Headstone) >= 30, "the third floor's gravestones aren't laid");
        Assert.All(third.Props.Where(p => p.Type == PropType.Headstone), p => Assert.True(third.IsSolid(p.X, p.Y)));
    }

    [Fact]
    public void TheGangwayAtVermilionsPierIsNoHouseAndTheShipsCabinsAreFurnished()
    {
        // A way off the open country onto a map that is no room (the gangway onto SSAnne_Exterior) is no building's door
        Assert.DoesNotContain(Overworld.PlacedBuildings, b => b.X0 <= 288 && b.X1 >= 286 && b.Y0 <= 236 && b.Y1 >= 235);
        Assert.DoesNotContain(Overworld.PlacedBuildings, b => b.Doors.Any(d => d.Target == "SSAnne_Exterior"));
        // The cabins have their beds, tables and chairs, the galley its worktops, the captain his desk and bed
        Assert.Contains(BuiltMaps.Value["SSAnne_1F_Room1"].Props, p => p.Type == PropType.Bed);
        Assert.Contains(BuiltMaps.Value["SSAnne_1F_Room1"].Props, p => p.Type == PropType.Table);
        Assert.Contains(BuiltMaps.Value["SSAnne_2F_Room1"].Props, p => p.Type == PropType.Bed);
        Assert.Contains(BuiltMaps.Value["SSAnne_Kitchen"].Props, p => p.Type == PropType.KitchenCounter);
        Assert.Contains(BuiltMaps.Value["SSAnne_CaptainsOffice"].Props, p => p.Type == PropType.Bed);
        // Vermilion's paved square before the harbour is paving, not the lawn round it
        Assert.Equal(TileType.Paving, Overworld.GetGroundTile(292, 229));
    }

    [Fact]
    public void AGymRoomOfKantoIsFoughtInOnItsLeadersStage()
    {
        foreach (var (gym, type) in new[] { ("PewterCity_Gym", PokemonType.Rock), ("CeruleanCity_Gym", PokemonType.Water), ("VermilionCity_Gym", PokemonType.Electric) })
        {
            var map = BuiltMaps.Value[gym];
            Assert.Equal(type, map.ArenaType);
            Assert.Equal(BattleArena.Gym, map.ArenaAt(5, 10));
        }
        Assert.Equal(BattleArena.Indoors, BuiltMaps.Value["VermilionCity_PokemonFanClub"].ArenaAt(5, 5));
        foreach (var (gym, type) in new[] { ("CeladonCity_Gym", PokemonType.Grass), ("SaffronCity_Gym", PokemonType.Psychic) })
            Assert.Equal(type, BuiltMaps.Value[gym].ArenaType);
    }

    [Fact]
    public void SaffronCityIsLaidIntoTheOverworldWhereItsStandInWas()
    {
        // Plan 19 · K5, part b: FireRed's routes join SaffronCity_Connection; the real city is laid in ten tiles west and
        // seven north of it, its frame giving way to the routes, so its doors and the far doors of its four gates are on
        // the overworld, and the overworld is as large as it was
        Assert.Equal(("MAP_SAFFRON_CITY", -10, -7), MapImporter.Gba.GbaWorld.StandIns["MAP_SAFFRON_CITY_CONNECTION"]);
        Assert.Equal(416, Overworld.Width);
        Assert.Equal(416, Overworld.Height);
        Assert.Equal("saffron_city", Overworld.AreaAt(287, 143)?.Key);
        foreach (string target in new[] { "SilphCo_1F", "SaffronCity_Gym", "SaffronCity_Dojo", "SaffronCity_PokemonCenter_1F", "Route7_EastEntrance", "Route8_WestEntrance", "Route5_SouthEntrance", "Route6_NorthEntrance" })
            Assert.Contains(Overworld.Warps, w => w.TargetMap == target);
        // The frame gives way to the routes it copies: Route 7's gate stands on Route 7's tiles
        Assert.Equal("route_7", Overworld.AreaAt(262, 140)?.Key);
        Assert.DoesNotContain(Kanto.Index.Maps, m => m.Area == "saffron_city");
    }

    [Fact]
    public void TheRocketHideoutsArrowsSpinThePlayerOnUntilAStop()
    {
        // FireRed's spin arrows are slides on a map with stop tiles: a spin goes on over plain floor until a stop
        var b2f = BuiltMaps.Value["RocketHideout_B2F"];
        Assert.True(b2f.HasSpinStops);
        Assert.Contains(Enumerable.Range(0, b2f.Width).SelectMany(x => Enumerable.Range(0, b2f.Height).Select(y => b2f.BehaviourAt(x, y))), b => b == TileBehavior.SlideEast);
        Assert.Equal(Direction.Right, FieldMovement.Spins(b2f, TileBehavior.SlideEast, Direction.Right, null));
        Assert.Equal(Direction.Right, FieldMovement.Spins(b2f, TileBehavior.None, null, Direction.Right));
        Assert.Null(FieldMovement.Spins(b2f, TileBehavior.Spinner, null, Direction.Right));
        // Platinum's own slides end on plain floor, on a map with no stop tiles
        Assert.False(Overworld.HasSpinStops);
        Assert.Null(FieldMovement.Spins(Overworld, TileBehavior.SlideEast, Direction.Right, null));
        Assert.Null(FieldMovement.Spins(Overworld, TileBehavior.None, null, null));
    }

    [Fact]
    public void CeladonAndSaffronAreOpenWithTheirCastsLooks()
    {
        foreach (string key in new[] { "celadon_city", "saffron_city", "rocket_hideout_b4f", "silph_co_11f", "route_16", "route_12", "celadon_city_gym", "saffron_city_gym" })
            Assert.Contains(key, Kanto.Index.Areas);
        foreach (string look in new[] { "erika", "sabrina", "giovanni", "biker", "chef" })
            Assert.True(CharacterStyles.Default.Has(look), look);
    }

    [Fact]
    public void FuchsiaTheCyclingRoadAndTheSafariZoneAreOpenAndWalkedTo()
    {
        // Plan 19 · K6, part a: Route 12's foot opens onto Route 13, and on along the coast to Fuchsia City; the Cycling
        // Road runs down from Route 16 to Route 18; the Safari Zone is behind its gate
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Kanto").Value;
        foreach (string key in new[] { "route_13", "route_14", "route_15", "route_17", "route_18", "fuchsia_city" })
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        foreach (string room in new[] { "FuchsiaCity_Gym", "FuchsiaCity_WardensHouse", "SafariZone_Center", "SafariZone_West", "SafariZone_SecretHouse", "Route18_EastEntrance_2F", "Route15_WestEntrance_2F" })
            Assert.True(walked.Keys.Any(m => m.Name == room), $"nobody walks into {room}");
        // Fuchsia builds in boards under fuchsia roofs, and Koga's hall is the Poison type's
        Assert.Equal(Architecture.Wildwood, Overworld.ArchitectureAt(216, 320));
        Assert.Equal(PokemonType.Poison, BuiltMaps.Value["FuchsiaCity_Gym"].ArenaType);
        // The Safari Zone's four parts are open country, its houses rooms
        Assert.False(BuiltMaps.Value["SafariZone_Center"].IsIndoors);
        Assert.True(BuiltMaps.Value["SafariZone_SecretHouse"].IsIndoors);
        Assert.NotEmpty(BuiltMaps.Value["SafariZone_Center"].AreaAt(26, 20)!.WildEncounters);
        // Koga and the classes the chapter meets first have their looks
        foreach (string look in new[] { "koga", "juggler", "bird_keeper" })
            Assert.True(CharacterStyles.Default.Has(look), look);
    }

    [Fact]
    public void TheCyclingRoadsSlopePullsARiderSouthAndLetsSomeoneOnFootStand()
    {
        // FireRed's MB_CYCLING_ROAD_PULL_DOWN on Route 17, read by the GBA reader as CyclingRoadPull
        (int X, int Y) slope = default;
        for (int y = 150; y < 320 && slope == default; y++)
            for (int x = 120; x < 170 && slope == default; x++)
                if (Overworld.AreaAt(x, y)?.Key == "route_17" && Overworld.BehaviourAt(x, y) == TileBehavior.CyclingRoadPull
                    && Overworld.BehaviourAt(x, y + 1) == TileBehavior.CyclingRoadPull && !Overworld.IsSolid(x, y + 1) && Overworld.GetNpcAt(x, y + 1) == null)
                    slope = (x, y);
        Assert.NotEqual(default, slope);
        Assert.Equal(Direction.Down, FieldMovement.Pulls(TileBehavior.CyclingRoadPull, TravelMode.Cycling));
        Assert.Null(FieldMovement.Pulls(TileBehavior.CyclingRoadPull, TravelMode.OnFoot));
        Assert.Null(FieldMovement.Pulls(TileBehavior.None, TravelMode.Cycling));

        // A rider who steers nowhere rolls on down
        var rider = new Player(slope.X, slope.Y);
        Assert.True(rider.SetCycling(true));
        for (int frame = 0; frame < 60 && rider.GridY == slope.Y; frame++)
            rider.Advance(1f / 60f, Overworld, null, false, _ => { }, _ => { });
        Assert.Equal((slope.X, slope.Y + 1), (rider.GridX, rider.GridY));
        Assert.Equal(Direction.Down, rider.Facing);

        // Someone on foot stands where they are
        var walker = new Player(slope.X, slope.Y);
        for (int frame = 0; frame < 60; frame++)
            walker.Advance(1f / 60f, Overworld, null, false, _ => { }, _ => { });
        Assert.Equal((slope.X, slope.Y), (walker.GridX, walker.GridY));
    }

    // ------------------------------------------------------------------ plan 19 · K6, part b: the sea and Cinnabar

    [Fact]
    public void TheSeaRoutesCinnabarIslandAndTheSeafoamIslandsAreOpenAndWalkedTo()
    {
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Kanto").Value;
        foreach (string key in new[] { "route_19", "route_20", "route_21_north", "route_21_south", "cinnabar_island" })
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        foreach (string room in new[] { "SeafoamIslands_1F", "SeafoamIslands_B1F", "SeafoamIslands_B2F", "SeafoamIslands_B3F", "SeafoamIslands_B4F",
                     "PowerPlant", "CinnabarIsland_Gym", "CinnabarIsland_PokemonLab_ExperimentRoom", "CinnabarIsland_PokemonCenter_2F",
                     "CinnabarIsland_Mart", "PokemonMansion_1F", "PokemonMansion_2F", "PokemonMansion_3F", "PokemonMansion_B1F" })
            Assert.True(walked.Keys.Any(m => m.Name == room), $"nobody walks into {room}");
        // The Seafoam Islands are caves; the Mansion, the Power Plant and the lab are rooms
        Assert.False(BuiltMaps.Value["SeafoamIslands_B4F"].IsIndoors);
        Assert.True(BuiltMaps.Value["PokemonMansion_B1F"].IsIndoors);
        Assert.True(BuiltMaps.Value["PowerPlant"].IsIndoors);
        Assert.Equal(PokemonType.Fire, BuiltMaps.Value["CinnabarIsland_Gym"].ArenaType);
    }

    [Fact]
    public void AHoleLeadsDownAndWhereItLandsLeadsNowhere()
    {
        // FireRed's floors keep a warp under each hole above, on plain floor, as the place its faller comes down: the
        // reader writes it as a landing, which leads nowhere and shuts nothing
        var b1f = Kanto.Area("seafoam_islands_b1f")!;
        Assert.Equal(2, b1f.Warps.Count(w => w.Landing == true));
        var first = BuiltMaps.Value["SeafoamIslands_1F"];
        var hole = first.GetWarpAt(21, 8)!;
        Assert.Equal(TileBehavior.Hole, first.BehaviourAt(21, 8));
        Assert.Equal(("SeafoamIslands_B1F", 21, 8), (hole.TargetMap, hole.TargetX, hole.TargetY));
        var below = BuiltMaps.Value["SeafoamIslands_B1F"];
        Assert.Null(below.GetWarpAt(21, 8));
        Assert.False(below.IsSolid(21, 8));
        // Each hole takes a boulder pushed into it, and the hole's script shows one of its own on the floor below
        Assert.Equal("FallFrom1F", first.BoulderPits[(21, 8)]);
        Assert.Equal("FallFromB3F", BuiltMaps.Value["SeafoamIslands_B3F"].BoulderPits[(6, 18)]);
    }

    [Fact]
    public void ACurrentCarriesASurferAndWhoeverFallsInRidesOutOnIt()
    {
        Assert.Equal(Direction.Down, FieldMovement.Carries(TileBehavior.CurrentSouth, Direction.Up));
        Assert.Equal(Direction.Up, FieldMovement.Carries(TileBehavior.CurrentNorth, Direction.Down));
        Assert.Equal(Direction.Right, FieldMovement.Carries(TileBehavior.CurrentEast, Direction.Left));
        Assert.Equal(Direction.Left, FieldMovement.Carries(TileBehavior.CurrentWest, Direction.Right));

        // Coming down B2F's hole onto B3F's current, the player rides out on it and is swept down to its foot, where the
        // way on to B4F is (FireRed's EnterByFalling)
        var b3f = BuiltMaps.Value["SeafoamIslands_B3F"];
        Assert.Equal(TileBehavior.CurrentSouth, b3f.BehaviourAt(23, 9));
        var player = new Player(23, 9);
        player.ComeDownOn(b3f);
        Assert.Equal(TravelMode.Surfing, player.Mode);
        Warp? took = null;
        for (int frame = 0; frame < 60 * 20 && took == null; frame++)
            player.Advance(1f / 60f, b3f, null, false, _ => { }, w => took = w);
        Assert.True(took != null, $"the current left the player at {player.GridX},{player.GridY}");
        Assert.Equal("SeafoamIslands_B4F", took!.TargetMap);

        // On land nothing changes
        var walker = new Player(21, 8);
        walker.ComeDownOn(BuiltMaps.Value["SeafoamIslands_B1F"]);
        Assert.Equal(TravelMode.OnFoot, walker.Mode);
    }

    [Fact]
    public void TheLowestFloorsLieCalmOnceTheirCurrentIsStopped()
    {
        // FireRed lays out B3F and B4F again by setmaplayoutindex once two boulders lie across each current: the reader
        // writes the other layout's chunks and world.json swaps them in by the floor's flag
        foreach (var (name, flag) in new[] { ("SeafoamIslands_B3F", "FLAG_STOPPED_SEAFOAM_B3F_CURRENT"), ("SeafoamIslands_B4F", "FLAG_STOPPED_SEAFOAM_B4F_CURRENT") })
        {
            var map = LoadedWorld.Value.BuildMaps().Single(m => m.Name == name);
            int Currents() => Enumerable.Range(0, map.Width).SelectMany(x => Enumerable.Range(0, map.Height).Select(y => map.BehaviourAt(x, y)))
                .Count(b => b is TileBehavior.CurrentNorth or TileBehavior.CurrentSouth or TileBehavior.CurrentEast or TileBehavior.CurrentWest);
            var place = Assert.Single(map.SwappedPlaces);
            Assert.Equal(flag, place.Flag);
            int running = Currents();
            Assert.True(running > 0, name);
            Assert.Empty(map.ApplySwappedPlaces(_ => 0, _ => false));
            Assert.Single(map.ApplySwappedPlaces(_ => 0, f => f == flag));
            Assert.True(Currents() < running, $"{name} still runs as fast");
        }
    }

    [Fact]
    public void ArticunosLedgeIsReachedOnlyOnceTheLowestCurrentIsStopped()
    {
        // From the foot of B3F's ladder down, on foot and surfing as the field's rules allow, a current taking whoever
        // is on it its own way: Articuno's ledge is out of reach while the current runs, and in reach once it is still
        foreach (bool stopped in new[] { false, true })
        {
            var b4f = LoadedWorld.Value.BuildMaps().Single(m => m.Name == "SeafoamIslands_B4F");
            if (stopped) b4f.ApplySwappedPlaces(_ => 0, _ => true);
            var reached = Reach(b4f, 15, 10);
            bool byArticuno = new[] { (9, 3), (8, 2), (10, 2), (9, 1) }.Any(reached.Contains);
            Assert.True(byArticuno == stopped, stopped ? "the still water doesn't lead to Articuno" : "the running current lets the player reach Articuno");
        }
    }

    /// <summary>Every tile one can come to from a tile of a map, on foot or surfing, carried where a current carries.</summary>
    private static HashSet<(int, int)> Reach(Map map, int sx, int sy)
    {
        var seen = new HashSet<(int, int)>();
        var states = new HashSet<(int, int, TravelMode)>();
        var queue = new Queue<(int X, int Y, TravelMode Mode)>();
        queue.Enqueue((sx, sy, TravelMode.OnFoot));
        while (queue.Count > 0)
        {
            var (x, y, mode) = queue.Dequeue();
            if (!states.Add((x, y, mode))) continue;
            seen.Add((x, y));
            var carried = FieldMovement.Carries(map.BehaviourAt(x, y), Direction.Down);
            foreach (var dir in carried is { } c ? new[] { c } : new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var walker = new Walker(mode, map.HeightAt(x, y));
                var step = FieldMovement.Step(map, x, y, dir, walker);
                if (step.Moves) queue.Enqueue((step.X, step.Y, step.Mode));
                else if (mode == TravelMode.OnFoot && FieldMovement.CanStartSurf(map, x, y, dir, walker))
                {
                    var (dx, dy) = FieldMovement.Delta(dir);
                    queue.Enqueue((x + dx, y + dy, TravelMode.Surfing));
                }
            }
        }
        return seen;
    }

    [Fact]
    public void CinnabarsGymDoorsAndTheMansionsBarriersStandWhereFireRedDrawsThem()
    {
        // The Gym's six doors are shutters on the doorways the layout draws shut, opened under them
        var gym = BuiltMaps.Value["CinnabarIsland_Gym"];
        foreach (var (x, y) in new[] { (26, 9), (27, 9), (17, 9), (11, 22), (5, 17), (6, 9) })
        {
            Assert.False(gym.IsSolid(x, y), $"the doorway {x},{y} stays a wall");
            Assert.Contains(gym.Everyone, n => n.NpcType == NPC.GalacticDoorType && n.GridX == x && n.GridY == y);
        }
        // The Mansion's barriers: those of the switch's first state and those of its second, on every floor
        foreach (string floor in new[] { "PokemonMansion_1F", "PokemonMansion_2F", "PokemonMansion_3F", "PokemonMansion_B1F" })
        {
            var map = BuiltMaps.Value[floor];
            var barriers = map.Everyone.Where(n => n.NpcType == NPC.ElectricGateType).ToList();
            Assert.Contains(barriers, b => b.HiddenBy == "FLAG_POKEMON_MANSION_SWITCH_STATE");
            Assert.Contains(barriers, b => b.ShownBy == "FLAG_POKEMON_MANSION_SWITCH_STATE");
            Assert.All(barriers, b => Assert.False(map.IsSolid(b.GridX, b.GridY)));
        }
    }

    [Fact]
    public void CinnabarSaffronAndCeladonBuildInTheirOwnWaysAndTheChaptersLooksAreThere()
    {
        Assert.Equal(Architecture.Cinder, Overworld.ArchitectureAt(74, 388));
        Assert.Contains(Overworld.PlacedBuildings, b => Overworld.AreaAt(b.X0, b.Y1)?.Key == "cinnabar_island");
        Assert.Equal(Architecture.Jade, Kanto.Overlay("celadon_city")!.Architecture);
        Assert.Equal(Architecture.Uptown, Kanto.Overlay("saffron_city")!.Architecture);
        foreach (string zone in new[] { "safari_zone_center", "safari_zone_east", "safari_zone_north", "safari_zone_west" })
            Assert.Equal(Architecture.Wildwood, Kanto.Overlay(zone)!.Architecture);
        // Saffron City's streets are paved, not lawn
        int paved = 0;
        for (int y = 140; y < 175; y++)
            for (int x = 260; x < 300; x++)
                if (Overworld.AreaAt(x, y)?.Key == "saffron_city" && Overworld.GetGroundTile(x, y) == TileType.Paving) paved++;
        Assert.True(paved > 100, $"only {paved} paved tiles in Saffron City");
        foreach (string look in new[] { "blaine", "burglar", "tamer", "twins", "crush_kin" })
            Assert.True(CharacterStyles.Default.Has(look), look);
        foreach (string song in new[] { "kanto/surf", "kanto/cinnabar", "kanto/pokemon_mansion" })
            Assert.True(PokemonPlatinumEngine.Audio.MusicLibrary.Exists(song), song);
    }

    // ------------------------------------------------------------------ plan 19 · K7: the road to the League

    [Fact]
    public void RouteTwentyThreeVictoryRoadTheLeagueAndOneIslandAreOpenAndWalkedTo()
    {
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Kanto").Value;
        foreach (string key in new[] { "route_23", "indigo_plateau_exterior" })
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        foreach (string room in new[] { "VictoryRoad_1F", "VictoryRoad_2F", "VictoryRoad_3F", "IndigoPlateau_PokemonCenter_1F",
                     "IndigoPlateau_PokemonCenter_2F", "PokemonLeague_LoreleisRoom", "PokemonLeague_BrunosRoom", "PokemonLeague_AgathasRoom",
                     "PokemonLeague_LancesRoom", "PokemonLeague_ChampionsRoom", "PokemonLeague_HallOfFame", "OneIsland", "OneIsland_PokemonCenter_1F",
                     "OneIsland_PokemonCenter_2F", "OneIsland_House1", "OneIsland_House2", "OneIsland_Harbor" })
            Assert.True(walked.Keys.Any(m => m.Name == room), $"nobody walks into {room}");
        // FireRed's Victory Road takes Kanto's prefix, where Platinum's keys are victory_road_*; it is a cave
        Assert.Contains("kanto_victory_road_1f", Kanto.Index.Areas);
        Assert.DoesNotContain("victory_road_1f", Kanto.Index.Areas);
        Assert.False(BuiltMaps.Value["VictoryRoad_2F"].IsIndoors);
        // One Island's town owns no chunk of its own (Treasure Beach does), and is open all the same
        var island = BuiltMaps.Value["OneIsland"];
        Assert.Equal("one_island", island.AreaAt(12, 132)?.Key);
        Assert.False(island.IsSolid(12, 132));
        Assert.Equal(PokemonType.Ground, BuiltMaps.Value["ViridianCity_Gym"].ArenaType);
        Assert.Equal(PokemonType.Dragon, BuiltMaps.Value["PokemonLeague_LancesRoom"].ArenaType);
    }

    [Fact]
    public void TheLeaguesDoorsLeadOnOnlyAndOpenAsEachIsBeaten()
    {
        string[] rooms = { "PokemonLeague_LoreleisRoom", "PokemonLeague_BrunosRoom", "PokemonLeague_AgathasRoom", "PokemonLeague_LancesRoom", "PokemonLeague_ChampionsRoom" };
        for (int i = 0; i < rooms.Length; i++)
        {
            var room = BuiltMaps.Value[rooms[i]];
            // Nobody comes back out to the hall: each room leads on, through the door its master opens
            var on = Assert.Single(room.Warps);
            Assert.NotNull(on.OpenedBy);
            Assert.Equal(i + 1 < rooms.Length ? rooms[i + 1] : "PokemonLeague_HallOfFame", on.TargetMap);
            var target = BuiltMaps.Value[on.TargetMap];
            Assert.True(target.InBounds(on.TargetX, on.TargetY) && !target.IsSolid(on.TargetX, on.TargetY), $"{on.TargetMap} {on.TargetX},{on.TargetY}");
        }
        var hall = BuiltMaps.Value["IndigoPlateau_PokemonCenter_1F"];
        var first = hall.Warps.Single(w => w.TargetMap == rooms[0]);
        Assert.False(BuiltMaps.Value[rooms[0]].IsSolid(first.TargetX, first.TargetY));
    }

    [Fact]
    public void OneIslandBuildsInItsOwnWayAndTheChaptersLooksAndSongsAreThere()
    {
        Assert.Equal(Architecture.Isle, Kanto.Overlay("one_island")!.Architecture);
        foreach (string look in new[] { "lorelei", "bruno", "agatha", "lance", "celio", "cooltrainer_m", "cooltrainer_f", "swimmer_m_water", "swimmer_f_water" })
            Assert.True(CharacterStyles.Default.Has(look), look);
        foreach (string song in new[] { "kanto/victory_road", "kanto/sevii_123", "kanto/hall_of_fame" })
            Assert.True(PokemonPlatinumEngine.Audio.MusicLibrary.Exists(song), song);
    }

    // ------------------------------------------------------------------ plan 19 · K9, part a: the first Sevii Islands

    [Fact]
    public void KindleRoadMtEmberAndTwoAndThreeIslandAreOpenAndWalkedTo()
    {
        var walked = Walked.Value;
        // The Seagallop's sailors sail on to Two and Three Island, and every room and cave of the three islands is walked into
        foreach (string room in new[] { "OneIsland_KindleRoad_EmberSpa", "MtEmber_Exterior", "MtEmber_SummitPath_1F", "MtEmber_SummitPath_2F",
                     "MtEmber_SummitPath_3F", "MtEmber_Summit", "MtEmber_RubyPath_1F", "MtEmber_RubyPath_B1F", "MtEmber_RubyPath_B2F",
                     "MtEmber_RubyPath_B3F", "MtEmber_RubyPath_B4F", "MtEmber_RubyPath_B5F", "MtEmber_RubyPath_B1F_Stairs",
                     "MtEmber_RubyPath_B2F_Stairs", "TwoIsland", "TwoIsland_JoyfulGameCorner", "TwoIsland_House", "TwoIsland_PokemonCenter_1F",
                     "TwoIsland_PokemonCenter_2F", "TwoIsland_Harbor", "TwoIsland_CapeBrink_House", "ThreeIsland", "ThreeIsland_BerryForest",
                     "ThreeIsland_DunsparceTunnel", "ThreeIsland_House1", "ThreeIsland_PokemonCenter_1F", "ThreeIsland_PokemonCenter_2F",
                     "ThreeIsland_Mart", "ThreeIsland_House2", "ThreeIsland_House3", "ThreeIsland_House4", "ThreeIsland_House5", "ThreeIsland_Harbor" })
        {
            Assert.True(walked.Keys.Any(m => m.Name == room), $"nobody walks into {room}");
            Assert.Contains(room, RegionDatabase.Get(RegionDatabase.Kanto)!.Maps);
        }
        // The islands' outdoors, each a map of its own matrix with several areas on it
        foreach (var (map, keys) in new[] { ("OneIsland", new[] { "one_island", "one_island_kindle_road", "one_island_treasure_beach" }),
                     ("TwoIsland", new[] { "two_island", "two_island_cape_brink" }), ("ThreeIsland", new[] { "three_island", "three_island_bond_bridge", "three_island_port" }) })
        {
            var tiles = walked.Single(kv => kv.Key.Name == map).Value;
            foreach (string key in keys)
                Assert.True(tiles.Any(t => BuiltMaps.Value[map].AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to");
        }
        // The Ruby Path's mouth opens only once Celio has asked for the Ruby (one_island_pokemon_center_1f.txt)
        var mouth = Assert.Single(BuiltMaps.Value["MtEmber_Exterior"].Warps, w => w.TargetMap == "MtEmber_RubyPath_1F");
        Assert.Equal("FLAG_MT_EMBER_RUBY_PATH_OPEN", mouth.OpenedBy);
        Assert.False(BuiltMaps.Value["MtEmber_RubyPath_1F"].IsSolid(22, 16));
    }

    [Fact]
    public void TheIslandsBuildInTheirOwnWayAndTheirLooksSongsAndItemsAreThere()
    {
        foreach (string key in new[] { "two_island", "three_island" })
            Assert.Equal(Architecture.Isle, Kanto.Overlay(key)!.Architecture);
        foreach (string look in new[] { "biker", "little_girl", "balding_man", "crush_girl", "black_belt", "woman_2", "tuber_f", "fat_man" })
            Assert.True(CharacterStyles.Default.Has(look), look);
        Assert.True(PokemonPlatinumEngine.Audio.MusicLibrary.Exists("kanto/sevii_cave"));
        Assert.Equal("kanto/sevii_cave", Kanto.Overlay("mt_ember_exterior")!.BgmTrack);
        // FireRed's two key items of the errand, and the Two Island stall's three stocks
        foreach (string item in new[] { "Ruby", "Rainbow Pass", "Meteorite", "Tri-Pass" })
            Assert.Equal(ItemPocket.KeyItems, ItemDatabase.Get(item)!.Pocket);
        foreach (string stall in new[] { "two_island_1", "two_island_2", "two_island_3" })
            Assert.All(MartDatabase.Specialties[stall], i => Assert.NotNull(ItemDatabase.Get(i)));
        Assert.Equal(new[] { "Great Ball", "Fresh Water" }, MartDatabase.Specialties["two_island_1"]);
        // Moltres stands on Mt. Ember's summit behind FireRed's flag
        var moltres = Assert.Single(BuiltMaps.Value["MtEmber_Summit"].Everyone, n => n.Species == "Moltres");
        Assert.Equal("FLAG_HIDE_MOLTRES", moltres.HiddenBy);
    }

    // ------------------------------------------------------------------ plan 19 · K8: the way on and Kanto's own songs

    [Fact]
    public void TheCaptainWhoSailsAChampionToJohtoStandsOnVermilionsQuayAndNobodyIsLeftOnPalletsShore()
    {
        var attendants = Overworld.Everyone.Where(n => n.IsTransportAttendant).ToList();
        var captain = Assert.Single(attendants);
        Assert.Equal("vermilion_city", Overworld.AreaAt(captain.GridX, captain.GridY)?.Key);
        Assert.Equal("Vermilion City", Overworld.DisplayNameAt(captain.GridX, captain.GridY));
        Assert.True(Overworld.IsWalkable(captain.GridX - 1, captain.GridY) || Overworld.IsWalkable(captain.GridX + 1, captain.GridY),
            "nobody can come up to the captain");
        // Spoken to, he runs the common attendant's script, which asks the region's link
        Assert.Equal(PokemonPlatinumEngine.Story.FieldScripts.Attendant, PokemonPlatinumEngine.Story.FieldScripts.For(captain));
        // The way on opens only once Kanto's Hall of Fame has been entered, and goes to Johto
        var link = RegionDatabase.LinkFrom(RegionDatabase.Kanto)!;
        var story = new PokemonPlatinumEngine.Story.StoryState();
        Assert.Equal(TravelCheck.StoryUnfinished, RegionDatabase.CheckTravel(link, story));
        story.Set("KantoHallOfFame");
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(link, story));
        Assert.Equal(RegionDatabase.Johto, link.To);
        Assert.Contains("Johto", RegionDatabase.AttendantLines(link, TravelCheck.Ready)[0]);
    }

    [Fact]
    public void KantoPlaysItsOwnChampionBattleCreditsAndSlowPalletButSinnohKeepsItsOwn()
    {
        var exists = PokemonPlatinumEngine.Audio.MusicLibrary.Exists;
        var champion = PokemonPlatinumEngine.Audio.MusicRole.BattleChampion;
        // The rival as Champion is of the Champion's class: Kanto's own battle song answers him
        Assert.Equal(champion, PokemonPlatinumEngine.Audio.MusicDirector.BattleRoleOf("Champion"));
        Assert.Equal("kanto/battle_champion", PokemonPlatinumEngine.Audio.MusicDirector.Resolve(champion, RegionDatabase.Kanto, exists));
        Assert.Equal("common/battle_champion", PokemonPlatinumEngine.Audio.MusicDirector.Resolve(champion, RegionDatabase.Sinnoh, exists));
        // The credits and the Hall of Fame's ceremony have songs of Kanto's, and a region with none plays the role it did
        Assert.Equal("kanto/credits", PokemonPlatinumEngine.Audio.MusicDirector.SceneSong(RegionDatabase.Kanto, "credits", exists));
        Assert.Equal("kanto/hall_of_fame", PokemonPlatinumEngine.Audio.MusicDirector.SceneSong(RegionDatabase.Kanto, "hall_of_fame", exists));
        Assert.Null(PokemonPlatinumEngine.Audio.MusicDirector.SceneSong(RegionDatabase.Sinnoh, "credits", exists));
        Assert.Null(PokemonPlatinumEngine.Audio.MusicDirector.SceneSong(null, "credits", exists));
        // The Hall of Fame's room plays the slowed Pallet Town tune, which loops long enough for an area's theme
        Assert.Equal("kanto/slow_pallet", Kanto.Overlay("pokemon_league_hall_of_fame")!.BgmTrack);
        Assert.True(PokemonPlatinumEngine.Audio.MusicLibrary.Get("kanto/slow_pallet")!.LoopDuration() >= 25);
        // The credits' song is a long one: the roll goes on for more than a minute
        Assert.True(PokemonPlatinumEngine.Audio.MusicLibrary.Get("kanto/credits")!.LoopDuration() >= 60);
    }
}
