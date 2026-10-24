using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Hoenn as Emerald's maps make it (plan 21 · H1): what <c>Data/world/hoenn</c> opens, the overworld and its rooms
/// built from the GBA reader's files, the region's record, where a new game there begins, and the importer's rows for
/// Emerald's tilesets. Floors, not counts, wherever a later session opens more (plan 16 · T16).
/// </summary>
public class HoennTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Hoenn"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, WorldWalk.OpenedAhead.Where(s => s.Map == "Hoenn").Prepend(RegionDatabase.Get(RegionDatabase.Hoenn)!.Start!).ToArray()));

    private static World Hoenn => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Hoenn"];
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);

    private static readonly string[] FirstAreas = { "littleroot_town", "route_101", "oldale_town", "route_102", "route_103", "petalburg_city" };

    [Fact]
    public void TheIndexOpensLittlerootTownToPetalburgCityWithTheirRooms()
    {
        Assert.Equal("Hoenn", Hoenn.Index.Region);
        Assert.True(Hoenn.Index.BuildingsAtDoors);
        foreach (string key in FirstAreas) Assert.Contains(key, Hoenn.Index.Areas);
        Assert.True(Hoenn.Index.Areas.Count >= 22, $"only {Hoenn.Index.Areas.Count} areas open in Hoenn");
        // Every map the index makes is the region's, and every room is a room
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        foreach (var entry in Hoenn.Index.Maps)
        {
            Assert.Contains(entry.Name, hoenn.Maps);
            Assert.True(BuiltMaps.Value.ContainsKey(entry.Name), $"{entry.Name} isn't built");
            if (entry.Setting == MapSetting.Indoors) Assert.True(BuiltMaps.Value[entry.Name].IsIndoors, $"{entry.Name} is no room");
        }
        // Every open area has its file and its overlay, and every map of the region a home in the index
        foreach (string key in Hoenn.Index.Areas)
        {
            Assert.NotNull(Hoenn.Area(key));
            Assert.NotNull(Hoenn.Overlay(key));
        }
        foreach (string map in hoenn.Maps) Assert.Contains(Hoenn.Index.Maps, e => e.Name == map);
    }

    [Fact]
    public void AnAreaKeyOrAMapOfHoennNeverMeetsAnotherRegions()
    {
        var others = World.LoadAll().Where(w => w.Index.Region != "Hoenn").SelectMany(w => w.Index.Areas).ToHashSet();
        Assert.Empty(Hoenn.Index.Areas.Where(others.Contains));
        foreach (var entry in Hoenn.Index.Maps) Assert.Equal(1, RegionDatabase.All.Count(r => r.Maps.Contains(entry.Name)));
        Assert.Contains("Hoenn", MapDatabase.MapNames);
    }

    [Fact]
    public void TheRegionsRecordIsEmeralds()
    {
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        Assert.True(hoenn.IsBuilt);
        Assert.Equal("hoenn", hoenn.Folder);
        Assert.Equal("Hoenn", hoenn.OverworldMap);
        Assert.Equal(new[] { "Treecko", "Torchic", "Mudkip" }, hoenn.Starters);
        Assert.Equal("Torchic", hoenn.RivalStarterFor("Treecko"));
        Assert.Equal(new[] { "Stone", "Knuckle", "Dynamo", "Heat", "Balance", "Feather", "Mind", "Rain" }, hoenn.BadgeNames);
        Assert.Equal("Stone", hoenn.Gates["Cut"]);
        Assert.Equal("Balance", hoenn.Gates["Surf"]);
        Assert.Equal("Rain", hoenn.Gates["Waterfall"]);
        Assert.Equal("Balance", FieldMoveRules.BadgeFor(FieldMove.Surf, RegionDatabase.Hoenn));
        Assert.Equal("hoenn", hoenn.Dex);
        // Emerald's regional Pokédex: 202 species, Treecko first
        Assert.Equal(202, PokemonDatabase.GetAll().Count(s => s.DexNumbers.ContainsKey("hoenn")));
        Assert.Equal(1, PokemonDatabase.Get("Treecko")!.DexNumbers["hoenn"]);
        // The rival is the other hero, so the introduction asks no rival's name; the professor is Birch
        Assert.Null(hoenn.Rival);
        Assert.Equal("Prof. Birch", hoenn.Professor!.Name);
        // The story begins with nothing since plan 21 · H2
        Assert.True(hoenn.HasStory);
        Assert.Equal("May", hoenn.RivalFor(PlayerLook.Boy));
    }

    [Fact]
    public void ANewGameInHoennBeginsBesideTheVanBeforeTheFamilysHouseInLittlerootTown()
    {
        var start = RegionDatabase.Get(RegionDatabase.Hoenn)!.Start!;
        // (The van stops beside the house, two tiles west and one south of its door: plan 21 · H2)
        Assert.Equal((123, 292), (start.X, start.Y));
        Assert.Equal("Hoenn", start.Map);
        Assert.True(Overworld.IsWalkable(start.X, start.Y));
        Assert.Equal("littleroot_town", Overworld.AreaAt(start.X, start.Y)?.Key);
        Assert.Equal("Littleroot Town", Overworld.DisplayNameAt(start.X, start.Y));
        Assert.Equal("LittlerootTown_BrendansHouse_1F", Overworld.GetWarpAt(start.X + 2, start.Y - 2)?.TargetMap);
        Assert.Equal("hoenn/littleroot", Overworld.BgmTrackAt(start.X, start.Y));
        Assert.Equal(Architecture.Sapling, Overworld.ArchitectureAt(start.X, start.Y));
    }

    [Fact]
    public void TheWalkFromTheStartReachesEveryOpenAreaAndEveryRoom()
    {
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Hoenn").Value;
        foreach (string key in FirstAreas)
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        foreach (var entry in Hoenn.Index.Maps)
            Assert.True(walked.Keys.Any(m => m.Name == entry.Name), $"nobody walks into {entry.Name}");
        // A floor of tiles, so a route that opened half way shows
        Assert.True(outside.Count >= 2200, $"only {outside.Count} tiles of Hoenn walked");
    }

    [Fact]
    public void TheSeaFloorLiesTileForTileUnderTheSeaItIsDivedTo()
    {
        // Plan 21 · H5: Underwater Route 124 under Route 124, the first pair; a dive lands on the same coordinates, so
        // an offset wrong by a tile would put the player in rock
        var floor = BuiltMaps.Value["HoennUnderwater"];
        Assert.True(floor.IsUnderwater);
        Assert.Contains("route_124", Hoenn.Index.Areas);
        Assert.Contains("underwater_route_124", Hoenn.Index.Areas);
        int down = 0, up = 0;
        for (int y = 0; y < Overworld.Height; y++)
            for (int x = 0; x < Overworld.Width; x++)
            {
                if (Overworld.BehaviourAt(x, y) != TileBehavior.DiveWater || Overworld.DiveAt(x, y) is not { } below) continue;
                Assert.Equal("HoennUnderwater", below);
                Assert.True(floor.InBounds(x, y) && !floor.IsSolid(x, y), $"the dark water at {x},{y} dives onto rock");
                down++;
            }
        for (int y = 0; y < floor.Height; y++)
            for (int x = 0; x < floor.Width; x++)
            {
                if (floor.IsSolid(x, y) || !FieldMovement.CanSurface(floor, x, y)) continue;
                Assert.Equal("Hoenn", floor.EmergeAt(x, y));
                Assert.True(FieldMovement.SurfacesOnto(Overworld, x, y), $"the sea floor at {x},{y} comes up onto {Overworld.BehaviourAt(x, y)}");
                up++;
            }
        // Route 124's trenches: 188 tiles each way when it was opened
        Assert.True(down >= 150, $"only {down} tiles of dark water dive");
        Assert.True(up >= 150, $"only {up} tiles of the sea floor come up");
        // Clamperl, Chinchou and Relicanth in its seaweed, and the seaweed is kelp
        var area = floor.Areas.Single(a => a.Key == "underwater_route_124");
        Assert.Contains(area.WaterEncounters, e => e.SpeciesName == "Clamperl");
        Assert.Contains(area.WaterEncounters, e => e.SpeciesName == "Relicanth");
        int kelp = 0;
        for (int y = 0; y < floor.Height; y++)
            for (int x = 0; x < floor.Width; x++)
                if (floor.BehaviourAt(x, y) is TileBehavior.Seaweed or TileBehavior.SeaweedNoSurfacing && !floor.IsSolid(x, y))
                {
                    Assert.Equal(TileType.Kelp, floor.GetGroundTile(x, y));
                    kelp++;
                }
        Assert.True(kelp > 0, "no kelp on the sea floor");
    }

    [Fact]
    public void TheWalkDivesToTheSeaFloorAndComesBackUp()
    {
        // From Route 124, come to by sea from Lilycove City since plan 21 · H7, part a, down to the floor and up again
        var floor = Walked.Value.Single(kv => kv.Key.Name == "HoennUnderwater").Value;
        Assert.True(floor.Count >= 400, $"only {floor.Count} tiles of the sea floor swum");
        var outside = Walked.Value.Single(kv => kv.Key.Name == "Hoenn").Value;
        Assert.Contains(outside, t => Overworld.AreaAt(t.X, t.Y)?.Key == "route_124" && Overworld.BehaviourAt(t.X, t.Y) == TileBehavior.DiveWater);
        Assert.Empty(WorldWalk.OpenedAhead);
    }

    [Fact]
    public void EveryWayIntoARoomLandsOnOpenGroundAndLeadsBack()
    {
        foreach (var room in BuiltMaps.Value.Values.Where(m => m.IsIndoors))
            foreach (var warp in room.Warps.Where(w => w.TargetMap == "Hoenn"))
            {
                // (someone of the story may stand on it for a while: Team Aqua queues at the Oceanic Museum's door until
                // the shipyard sends them in, plan 21 · H3, and nobody comes out of the museum before then)
                Assert.True(Overworld.IsWalkable(warp.TargetX, warp.TargetY) || Overworld.GetWarpAt(warp.TargetX, warp.TargetY) != null
                    || (!Overworld.IsSolid(warp.TargetX, warp.TargetY) && Overworld.GetNpcAt(warp.TargetX, warp.TargetY) is { HiddenBy: not null }),
                    $"{room.Name}'s way out lands on {warp.TargetX},{warp.TargetY}, which is shut");
                Assert.Contains(Overworld.Warps, w => w.TargetMap == room.Name);
            }
    }

    [Fact]
    public void RoomsAreOfTheirKindAndFurnished()
    {
        Assert.Equal(InteriorStyle.House, BuiltMaps.Value["LittlerootTown_BrendansHouse_1F"].Interior);
        Assert.Equal(InteriorStyle.PokemonCenter, BuiltMaps.Value["OldaleTown_PokemonCenter_1F"].Interior);
        Assert.Equal(InteriorStyle.PokeMart, BuiltMaps.Value["OldaleTown_Mart"].Interior);
        Assert.Equal(InteriorStyle.Lab, BuiltMaps.Value["LittlerootTown_ProfessorBirchsLab"].Interior);
        // Emerald's rows of the interior table: the lab's shelves and machines, the house's television, the Center's machine
        Assert.Contains(BuiltMaps.Value["LittlerootTown_ProfessorBirchsLab"].Props, p => p.Type == PropType.Bookshelf);
        Assert.Contains(BuiltMaps.Value["LittlerootTown_ProfessorBirchsLab"].Props, p => p.Type == PropType.LabMachine);
        Assert.Contains(BuiltMaps.Value["LittlerootTown_BrendansHouse_1F"].Props, p => p.Type == PropType.Television);
        Assert.Contains(BuiltMaps.Value["OldaleTown_PokemonCenter_1F"].Props, p => p.Type == PropType.HealingMachine);
        Assert.Contains(BuiltMaps.Value["OldaleTown_Mart"].Props, p => p.Type == PropType.StoreShelf);
    }

    [Fact]
    public void MomSeesToTheTeamAndAWhiteOutWakesBesideHer()
    {
        var home = BuiltMaps.Value["LittlerootTown_BrendansHouse_1F"];
        var mom = Assert.Single(home.Everyone, n => n.IsHealingNurse);
        var (x, y, _) = GameEngine.HomeSpot(mom);
        Assert.True(home.IsWalkable(x, y), $"nobody can wake at {x},{y}");
        var first = SpawnLocations.Of(RegionDatabase.Get(RegionDatabase.Hoenn)!)[0];
        Assert.Equal("LittlerootTown_BrendansHouse_1F", first.Room);
        Assert.Equal("littleroot_town", first.Area);
        // And every Pokémon Center has its nurse
        foreach (string center in new[] { "OldaleTown_PokemonCenter_1F", "PetalburgCity_PokemonCenter_1F" })
            Assert.Single(BuiltMaps.Value[center].Everyone, n => n.Script == "Nurse");
    }

    [Fact]
    public void RouteOneHundredAndOneHasEmeraldsPokemonInItsGrass()
    {
        var route = Overworld.Areas.Single(a => a.Key == "route_101");
        Assert.Equal(new[] { "Poochyena", "Wurmple", "Zigzagoon" }, route.WildEncounters.Select(e => e.SpeciesName).Distinct().OrderBy(s => s));
        Assert.True(route.LandRate > 0);
        Assert.Equal("Route 101", route.DisplayName);
        Assert.Equal("hoenn/route101", route.BgmTrack);
        int grass = 0;
        for (int y = 0; y < Overworld.Height; y++)
            for (int x = 0; x < Overworld.Width; x++)
                if (Overworld.AreaAt(x, y) == route && Overworld.BehaviourAt(x, y) == TileBehavior.TallGrass) grass++;
        Assert.True(grass >= 60, $"only {grass} tiles of tall grass on Route 101");
    }

    [Fact]
    public void HoennsTrainersAreEmeraldsUnderTheirOwnPrefixWithEmeraldsPrize()
    {
        // Calvin of Route 102: a Youngster with a Poochyena at 5, by Emerald's rate of 4 (four times the level, times the rate)
        var calvin = TrainerDatabase.Get("TRAINER_CALVIN_1", RegionDatabase.Hoenn)!;
        Assert.Equal("hoenn_calvin_1", calvin.Id);
        Assert.Equal(80, calvin.PrizeMoney);
        // The placed trainers of the open routes battle with Emerald's teams
        var route102 = Overworld.Everyone.Where(n => n.IsTrainer && Overworld.AreaAt(n.GridX, n.GridY)?.Key == "route_102").ToList();
        Assert.Equal(4, route102.Count);
        Assert.All(route102, t => Assert.StartsWith("hoenn_", t.TrainerData!.Id));
        Assert.Equal(4 * 5 * 4, MapImporter.Gba.GbaTables.PrizeMoney("YOUNGSTER", 5, false, MapImporter.Gba.GbaTables.EmeraldMoneyRates));
        Assert.Equal(4 * 58 * 50, MapImporter.Gba.GbaTables.PrizeMoney("CHAMPION", 58, false, MapImporter.Gba.GbaTables.EmeraldMoneyRates));
    }

    [Fact]
    public void EmeraldsTilesetsHaveTheirOwnRowsApartFromFireReds()
    {
        var emerald = MapImporter.Gba.GbaGame.Emerald;
        // Emerald's forest, path and lawn under an overhang, which FireRed's rows for the same tileset's name would misread
        Assert.Equal(TerrainCover.Tree, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_General", 0x1D4, emerald));
        Assert.Equal(TerrainCover.Path, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_General", 0x1D9, emerald));
        Assert.Equal(TerrainCover.Grass, MapImporter.Gba.GbaCover.Of(TileBehavior.None, "METATILE_General_Grass_TreeLeft", "gTileset_General", 0x1CE, emerald));
        Assert.Equal(TerrainCover.Cliff, MapImporter.Gba.GbaCover.Of(TileBehavior.None, "METATILE_General_RockWall_GrassBase", "gTileset_General", 0x079, emerald));
        Assert.Equal(TerrainCover.Fence, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_Petalburg", 0x24C, emerald));
        // FireRed's General keeps its own rows, and Emerald's never see them
        Assert.Equal(TerrainCover.Tree, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_General", 0x014));
        Assert.Equal(TerrainCover.Unknown, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_General", 0x0E7, emerald));
        // The interior table likewise
        Assert.Equal(PropType.Bookshelf, MapImporter.Gba.GbaFurniture.Of("MB_NORMAL", "gTileset_Lab", 0x22C, emerald));
        Assert.Equal(PropType.LabDesk, MapImporter.Gba.GbaFurniture.Of("MB_NORMAL", "gTileset_Lab", 0x2A8));
        Assert.Equal(PropType.StoreShelf, MapImporter.Gba.GbaFurniture.Of("MB_SHOP_SHELF", "gTileset_Shop", 0x22B, emerald));
        foreach (var (_, ranges) in MapImporter.Gba.GbaCover.HoennRanges)
            foreach (var (from, to, _) in ranges) Assert.True(from <= to && to < 0x400);
    }

    // ------------------------------------------------------------------ plan 21 · H6: Route 118 to Fortree City

    [Fact]
    public void FortreeCityIsOpenWithItsFlySpotItsTreetopsAndItsRooms()
    {
        foreach (string key in new[] { "route_118", "route_119", "fortree_city", "route_120", "fortree_city_gym", "route_119_weather_institute_1f", "route_119_weather_institute_2f" })
            Assert.Contains(key, Hoenn.Index.Areas);
        var fortree = SpawnLocations.Of(RegionDatabase.Get(RegionDatabase.Hoenn)!).Single(s => s.Area == "fortree_city");
        Assert.Equal("FortreeCity_PokemonCenter_1F", fortree.Room);
        Assert.True(Overworld.IsWalkable(fortree.X, fortree.Y), $"nobody can land at {fortree.X},{fortree.Y}");
        Assert.Equal(Architecture.Treetop, Overworld.ArchitectureAt(335, 10));
        Assert.Equal("hoenn/fortree", Overworld.BgmTrackAt(335, 10));
        Assert.Single(BuiltMaps.Value["FortreeCity_PokemonCenter_1F"].Everyone, n => n.Script == "Nurse");
        // Fly over Hoenn: the Town Map has a place for Fortree on its own cell, and one for every spawn on the overworld
        var townMap = RegionMap.Of(RegionDatabase.Hoenn)!;
        Assert.Contains(townMap.FlySpots, f => f.Spawn == 7 && f.Cells.Contains((10, 0)));
        foreach (var spawn in SpawnLocations.Of(RegionDatabase.Get(RegionDatabase.Hoenn)!).Where(s => s.Id != 0))
            Assert.Contains(townMap.FlySpots, f => f.Spawn == spawn.Id);
        Assert.IsType<FortreeGates>(BuiltMaps.Value["FortreeCity_Gym"].Puzzle);
        // The plank walks between the tree houses are bridges, the forest floor under them earth, the trunks trees
        var emerald = MapImporter.Gba.GbaGame.Emerald;
        Assert.Equal(TerrainCover.Bridge, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_Fortree", 0x23B, emerald));
        Assert.Equal(TerrainCover.Path, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_Fortree", 0x241, emerald));
        Assert.Equal(TerrainCover.Tree, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_Fortree", 0x206, emerald));
    }

    [Fact]
    public void RoutesOneNineteenAndOneTwentyHaveTheirWeatherAndTheirSongs()
    {
        // Emerald sets the weather by script as the player crosses its rows: here each stretch is a zone of the area
        Assert.True(Weathers.IsRain(Overworld.WeatherAt(300, 60)), "Route 119's middle should be under rain");
        Assert.Equal(FieldWeather.Clear, Overworld.WeatherAt(270, 148));
        Assert.True(Weathers.IsRain(Overworld.WeatherAt(380, 30)), "Route 120's north should be under rain");
        Assert.Equal(FieldWeather.Cloudy, Overworld.WeatherAt(380, 80));
        Assert.Equal("hoenn/route118", Overworld.BgmTrackAt(270, 148));
        Assert.Equal("hoenn/route119", Overworld.BgmTrackAt(300, 60));
        Assert.Equal("hoenn/route120", Overworld.BgmTrackAt(380, 30));
    }

    [Fact]
    public void AKecleonNobodyCanSeeStandsInTheWayOnFortreesBridge()
    {
        var kecleon = Assert.Single(Overworld.Everyone, n => n.Key == "kecleon" && n.GridX == 345 && n.GridY == 8);
        Assert.True(kecleon.IsUnseen);
        Assert.True(kecleon.IsThing);
        Assert.Equal("FLAG_HIDE_FORTREE_CITY_KECLEON", kecleon.HiddenBy);
        // And the one on Route 120's bridge, beside Steven
        var bridge = Assert.Single(Overworld.Everyone, n => n.Key == "bridge_kecleon");
        Assert.True(bridge.IsUnseen);
        Assert.Contains(Overworld.Everyone, n => n.Key == "route120_steven");
    }

    [Fact]
    public void TheFortreeGymsGatesTurnWhenPushedAndHoldAgainstAWall()
    {
        var gym = BuiltMaps.Value["FortreeCity_Gym"];
        var gates = new FortreeGates();
        int count = FortreeGates.Layout.Length;
        var start = Enumerable.Range(0, count).Select(gates.TurnOf).ToArray();
        Assert.Equal(FortreeGates.Layout.Select(g => g.Start), start);
        int turned = 0, held = 0;
        foreach (var gate in FortreeGates.Layout)
            for (int y = gate.Y - 2; y <= gate.Y + 1; y++)
                for (int x = gate.X - 2; x <= gate.X + 1; x++)
                    foreach (var dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
                    {
                        var probe = new FortreeGates();
                        if (probe.Bars(gym, x, y, dir)) { held++; continue; }
                        probe.Crossed(gym, x, y, dir);
                        if (Enumerable.Range(0, count).Any(i => probe.TurnOf(i) != start[i])) turned++;
                    }
        Assert.True(turned > 0, "no step pushes a gate round");
        Assert.True(held > 0, "no gate ever holds against a wall");
        // Coming in again lays every gate out afresh
        gates.Reset();
        Assert.Equal(start, Enumerable.Range(0, count).Select(gates.TurnOf));
    }

    // ------------------------------------------------------------------ plan 21 · H6, part b: Route 121 and the Safari Zone

    [Fact]
    public void RouteOneTwentyOneAndTheSafariZoneAreOpenWithTheirTables()
    {
        foreach (string key in new[] { "route_121", "route_121_safari_zone_entrance", "hoenn_safari_zone_south", "hoenn_safari_zone_southwest",
                     "hoenn_safari_zone_north", "hoenn_safari_zone_northwest", "hoenn_safari_zone_northeast", "hoenn_safari_zone_southeast",
                     "hoenn_safari_zone_rest_house", "ancient_tomb", "scorched_slab" })
            Assert.Contains(key, Hoenn.Index.Areas);
        Assert.Equal("route_121", Overworld.AreaAt(429, 94)?.Key);
        Assert.Equal("hoenn/route120", Overworld.BgmTrackAt(429, 94));
        // The zone's six parts are one map, each part its own area with Emerald's own table and the zone's own song
        var zone = BuiltMaps.Value["HoennSafariZone"];
        Assert.Equal("hoenn_safari_zone_south", zone.AreaAt(72, 75)?.Key);
        Assert.Equal("hoenn/safari_zone", zone.BgmTrackAt(72, 75));
        Assert.Contains(Hoenn.Area("hoenn_safari_zone_south")!.Land!, e => e.Species == "Girafarig");
        foreach (string part in new[] { "south", "southwest", "north", "northwest" })
            Assert.NotEmpty(Hoenn.Area($"hoenn_safari_zone_{part}")!.Land!);
        // FireRed's Safari Zone keeps its keys: Hoenn's take the region's prefix, so the two never meet
        Assert.DoesNotContain("safari_zone_south", Hoenn.Index.Areas);
        // The zone's attendant stands before its door back to the gate, and the counter's trigger is in the gate
        Assert.Contains(zone.Everyone, n => n.Key == "safari_exit_attendant" && n.GridX == 72 && n.GridY == 74);
        Assert.Contains(BuiltMaps.Value["Route121_SafariZoneEntrance"].Triggers, t => t.Script == "Counter");
        Assert.True(MusicLibrary.Exists("hoenn/safari_zone"));
        Assert.True(MusicLibrary.Exists("hoenn/sealed_chamber"));
    }

    [Fact]
    public void TheAncientTombAndTheScorchedSlabOpenOffRouteOneTwenty()
    {
        // Route 120's two ways in are open since part b
        foreach (string target in new[] { "AncientTomb", "ScorchedSlab" })
            Assert.Contains(Overworld.Warps, w => w.TargetMap == target && !w.Shut);
        var tomb = BuiltMaps.Value["AncientTomb"];
        Assert.Equal("hoenn/sealed_chamber", tomb.BgmTrackAt(8, 25));
        // Registeel stays away until its chamber is read (plan 21 · H8 and after)
        Assert.DoesNotContain(tomb.NPCs, n => n.Key == "registeel");
        // The Scorched Slab's TM11, Sunny Day, lies on its far side
        Assert.Contains(BuiltMaps.Value["ScorchedSlab"].Everyone, n => n.IsItemBall && n.Item == "TM11");
    }

    [Fact]
    public void LavaridgesBuriedTrainersLieOnPatchesThatLeadNowhere()
    {
        // A buried trainer springs up at whoever comes near and stays on the patch once beaten, so the warp under one
        // is never taken: the reader writes it as a landing (plan 21 · H6, part b), as Gerald's at 2,15 is
        var gym = Hoenn.Area("lavaridge_town_gym_1f")!;
        Assert.True(gym.Warps.Single(w => w.X == 2 && w.Z == 15).Landing == true);
        Assert.True(gym.Warps.Single(w => w.X == 5 && w.Z == 14).Landing != true);
        var b1f = Hoenn.Area("lavaridge_town_gym_b1f")!;
        Assert.Contains(b1f.Warps, w => w.Landing == true);
    }

    // ------------------------------------------------------------------ plan 21 · H7, part a: Lilycove City and Mt. Pyre

    [Fact]
    public void LilycoveCityIsOpenWithItsFlySpotItsStoreAndItsShutDoors()
    {
        foreach (string key in new[] { "lilycove_city", "route_122", "route_123", "lilycove_city_department_store_1f",
                     "lilycove_city_department_store_rooftop", "lilycove_city_lilycove_museum_2f", "route_123_berry_masters_house" })
            Assert.Contains(key, Hoenn.Index.Areas);
        var lilycove = SpawnLocations.Of(RegionDatabase.Get(RegionDatabase.Hoenn)!).Single(s => s.Area == "lilycove_city");
        Assert.Equal("LilycoveCity_PokemonCenter_1F", lilycove.Room);
        Assert.True(Overworld.IsWalkable(lilycove.X, lilycove.Y), $"nobody can land at {lilycove.X},{lilycove.Y}");
        Assert.Equal("hoenn/lilycove", Overworld.BgmTrackAt(507, 80));
        Assert.Single(BuiltMaps.Value["LilycoveCity_PokemonCenter_1F"].Everyone, n => n.Script == "Nurse");
        // The Contest Hall (plan 21 · H11), Team Aqua's hideout (part b) and the harbour (H9) stay shut, and so does the
        // store's lift on every floor
        foreach (string target in new[] { "LilycoveCity_ContestLobby", "AquaHideout_1F", "LilycoveCity_Harbor" })
            Assert.DoesNotContain(Overworld.Warps, w => w.TargetMap == target && !w.Shut);
        foreach (string floor in new[] { "1F", "2F", "3F", "4F", "5F" })
            Assert.DoesNotContain(BuiltMaps.Value[$"LilycoveCity_DepartmentStore_{floor}"].Warps,
                w => w.TargetMap == "LilycoveCity_DepartmentStoreElevator" && !w.Shut);
        // The floors are joined by their stairs, up to the roof
        Assert.Contains(BuiltMaps.Value["LilycoveCity_DepartmentStore_5F"].Warps, w => w.TargetMap == "LilycoveCity_DepartmentStoreRooftop");
        // Team Aqua's Wailmer lie in the water east of the city, on the tiles Emerald draws them on
        Assert.Equal(3, Overworld.Everyone.Count(n => n.Species == "Wailmer" && n.GridX >= 556 && n.GridY >= 82 && n.GridY <= 87));
        // Lilycove's open metatiles are its paving
        var emerald = MapImporter.Gba.GbaGame.Emerald;
        Assert.Equal(TerrainCover.Paving, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_Lilycove", 0x22B, emerald));
    }

    [Fact]
    public void MtPyreIsOpenFromRouteOneTwentyTwoToItsSummit()
    {
        foreach (string floor in new[] { "MtPyre_1F", "MtPyre_2F", "MtPyre_3F", "MtPyre_4F", "MtPyre_5F", "MtPyre_6F", "MtPyre_Exterior", "MtPyre_Summit" })
            Assert.True(BuiltMaps.Value.ContainsKey(floor), floor);
        Assert.Contains(Overworld.Warps, w => w.TargetMap == "MtPyre_1F" && !w.Shut);
        Assert.Equal("hoenn/mt_pyre", BuiltMaps.Value["MtPyre_3F"].BgmTrackAt(6, 6));
        // The summit is in mist, and the mountainside above its middle too
        Assert.Equal(FieldWeather.Fog, BuiltMaps.Value["MtPyre_Summit"].WeatherAt(23, 20));
        Assert.Equal(FieldWeather.Fog, BuiltMaps.Value["MtPyre_Exterior"].WeatherAt(20, 12));
        Assert.NotEqual(FieldWeather.Fog, BuiltMaps.Value["MtPyre_Exterior"].WeatherAt(10, 40));
        // Team Aqua's four grunts are trainers of Emerald's table on the way up
        var summit = BuiltMaps.Value["MtPyre_Summit"];
        Assert.Equal(4, summit.Everyone.Count(n => n.TrainerData?.Id.StartsWith("hoenn_grunt_mt_pyre_") == true));
        Assert.Contains(summit.Triggers, t => t.Script == "AquaTrigger1" && t.X == 23 && t.Y == 7);
        // Shuppet haunt the tower's halls
        Assert.Contains(Hoenn.Area("mt_pyre_1f")!.Land!, e => e.Species == "Shuppet");
    }

    [Fact]
    public void TheTwoTeamsHideoutsAreOpenWithTheirTrainersAndTheirSong()
    {
        // Team Magma's eight halls, come to from Jagged Pass, and Team Aqua's three floors, from Lilycove's cove
        string[] magma = { "MagmaHideout_1F", "MagmaHideout_2F_1R", "MagmaHideout_2F_2R", "MagmaHideout_2F_3R",
            "MagmaHideout_3F_1R", "MagmaHideout_3F_2R", "MagmaHideout_3F_3R", "MagmaHideout_4F" };
        string[] aqua = { "AquaHideout_1F", "AquaHideout_B1F", "AquaHideout_B2F" };
        foreach (string room in magma.Concat(aqua))
        {
            Assert.True(BuiltMaps.Value.ContainsKey(room), room);
            Assert.Equal("hoenn/aqua_magma_hideout", BuiltMaps.Value[room].BgmTrackAt(5, 5));
        }
        Assert.Contains(BuiltMaps.Value["JaggedPass"].Warps, w => w.TargetMap == "MagmaHideout_1F");
        Assert.Contains(Overworld.Warps, w => w.TargetMap == "AquaHideout_1F");

        // Sixteen grunts and Tabitha in Team Magma's, eight grunts in Team Aqua's, all of Emerald's table
        Assert.Equal(16, magma.Sum(r => BuiltMaps.Value[r].Everyone.Count(n => n.TrainerData?.Id.StartsWith("hoenn_grunt_magma_hideout_") == true)));
        Assert.Contains(BuiltMaps.Value["MagmaHideout_4F"].Everyone, n => n.TrainerData?.Id == "hoenn_tabitha_magma_hideout");
        Assert.Equal(8, aqua.Sum(r => BuiltMaps.Value[r].Everyone.Count(n => n.TrainerData?.Id.StartsWith("hoenn_grunt_aqua_hideout_") == true)));

        // Team Aqua's first basement is a maze of warp panels that lead about the same floor
        var b1f = BuiltMaps.Value["AquaHideout_B1F"];
        Assert.True(b1f.Warps.Count(w => w.TargetMap == "AquaHideout_B1F") >= 15);
        Assert.Contains(b1f.Warps, w => b1f.BehaviourAt(w.SourceX, w.SourceY) == TileBehavior.WarpPanel);
    }
}
