using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
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
                // (a warp of the floor is gone through, not come up from: the Seafloor Cavern's way in, plan 21 · H8, part b)
                if (floor.IsSolid(x, y) || !FieldMovement.CanSurface(floor, x, y) || floor.GetWarpAt(x, y) != null) continue;
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
                // (or the water, which the player comes out onto riding: Team Aqua's hideout, left by Surf as in Emerald,
                // plan 21 · H7, part b; Player.ComeDownOn)
                Assert.True(Overworld.IsWalkable(warp.TargetX, warp.TargetY) || Overworld.GetWarpAt(warp.TargetX, warp.TargetY) != null
                    || TileBehaviors.IsSurfable(Overworld.BehaviourAt(warp.TargetX, warp.TargetY))
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
        // The Contest Hall (plan 21 · H11) and the harbour (H9) stay shut, and so does the store's lift on every floor
        // (Team Aqua's hideout opened with part b)
        foreach (string target in new[] { "LilycoveCity_ContestLobby", "LilycoveCity_Harbor" })
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

    // ------------------------------------------------------------------ plan 21 · H8, part a: Mossdeep City

    [Fact]
    public void MossdeepCityIsOpenWithItsFlySpotItsGymAndItsRooms()
    {
        foreach (string key in new[] { "route_125", "route_126", "route_127", "mossdeep_city", "mossdeep_city_gym",
                     "mossdeep_city_space_center_1f", "mossdeep_city_space_center_2f", "mossdeep_city_stevens_house",
                     "route_124_diving_treasure_hunters_house", "underwater_route_126", "underwater_route_127" })
            Assert.Contains(key, Hoenn.Index.Areas);
        var mossdeep = SpawnLocations.Of(RegionDatabase.Get(RegionDatabase.Hoenn)!).Single(s => s.Area == "mossdeep_city");
        Assert.Equal("MossdeepCity_PokemonCenter_1F", mossdeep.Room);
        Assert.True(Overworld.IsWalkable(mossdeep.X, mossdeep.Y), $"nobody can land at {mossdeep.X},{mossdeep.Y}");
        Assert.Equal(Architecture.Starward, Overworld.ArchitectureAt(mossdeep.X, mossdeep.Y));
        Assert.Equal("hoenn/rustboro", Overworld.BgmTrackAt(mossdeep.X, mossdeep.Y));
        Assert.Single(BuiltMaps.Value["MossdeepCity_PokemonCenter_1F"].Everyone, n => n.Script == "Nurse");
        // The eastern sea plays Route 120's song, Route 124's too now that it is written
        Assert.Equal("hoenn/route120", Overworld.BgmTrackAt(647, 91));
        Assert.Equal("route_125", Overworld.AreaAt(647, 91)?.Key);
        // The Game Corner (Emerald's room of link games) and Shoal Cave (part b) stay shut; the treasure hunter's house
        // on Route 124 is open
        Assert.DoesNotContain(Overworld.Warps, w => w.TargetMap == "MossdeepCity_GameCorner_1F" && !w.Shut);
        Assert.DoesNotContain(Overworld.Warps, w => w.TargetMap.StartsWith("ShoalCave", StringComparison.Ordinal) && !w.Shut);
        Assert.Contains(Overworld.Warps, w => w.TargetMap == "Route124_DivingTreasureHuntersHouse" && !w.Shut);
        // The Space Center's two floors are joined by their stairs
        Assert.Contains(BuiltMaps.Value["MossdeepCity_SpaceCenter_1F"].Warps, w => w.TargetMap == "MossdeepCity_SpaceCenter_2F");
        // Route 125 has no dark water until the post-game's Marine Cave (Emerald's abnormal weather lays it), so its sea
        // floor is the post-game's too
        Assert.False(BuiltMaps.Value.ContainsKey("HoennUnderwaterRoute125"));
        Assert.DoesNotContain("underwater_route_125", Hoenn.Index.Areas);
        // Mossdeep's open metatiles are its sandy paths, and Emerald's shallows are water to look at
        var emerald = MapImporter.Gba.GbaGame.Emerald;
        Assert.Equal(TerrainCover.Path, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_Mossdeep", 0x2F0, emerald));
        Assert.Equal(TerrainCover.Water, MapImporter.Gba.GbaCover.Of(TileBehavior.ShallowWater, null, "gTileset_General", 0x19E, emerald));
    }

    [Fact]
    public void TheTwinsAndStevensTeamOfTheSpaceCenterAreEmeralds()
    {
        // Tate and Liza are one trainer of two, a double battle, their name kept apart by its ampersand
        var twins = TrainerDatabase.Get("hoenn_tate_and_liza_1")!;
        Assert.Equal("Tate & Liza", twins.Name);
        Assert.True(twins.DoubleBattle);
        Assert.Equal("Tate & Liza", MapImporter.Gba.GbaTables.Titled("TATE&LIZA"));
        Assert.Equal("Miu & Yuki", MapImporter.Gba.GbaTables.Titled("MIU & YUKI"));
        // Steven's own three of the multi battle beside him (Emerald's sStevenMons)
        var steven = TrainerDatabase.Get("hoenn_steven_partner")!;
        Assert.Equal(new[] { "Metang", "Skarmory", "Aggron" }, steven.Party.Select(p => p.Species));
        Assert.Equal(new[] { 42, 43, 44 }, steven.Party.Select(p => p.Level));
        Assert.All(steven.Party, p => Assert.Equal(255, p.IvScale));
        Assert.Equal(new[] { 2, 8, 3 }, steven.Party.Select(p => (int)(p.Personality % 25)));
        // Steven and Wally are of the rival's class in Emerald, and keep their own names
        var trainer = new Trainer { Id = steven.Id };
        TrainerDatabase.Fill(trainer, steven);
        Assert.Equal("Steven", trainer.Name);
        var wally = new Trainer { Id = "hoenn_wally_mauville" };
        TrainerDatabase.Fill(wally, TrainerDatabase.Get("hoenn_wally_mauville")!);
        Assert.Equal("Wally", wally.Name);
        // Steven's HM is the ninth, Dive, learned by whoever learns Dive by a machine in any game
        var hm = ItemDatabase.Get("HM09")!;
        Assert.Equal("Dive", hm.TeachesMove);
        Assert.True(MoveTeaching.IsHm(hm));
        Assert.Contains("HM09", PokemonDatabase.Get("Wailmer")!.TmMoves!);
        Assert.Contains("HM09", PokemonDatabase.Get("Buizel")!.TmMoves ?? new());
        Assert.DoesNotContain("HM09", PokemonDatabase.Get("Charmander")!.TmMoves ?? new());
    }

    [Fact]
    public void TheMossdeepGymsSwitchesTurnWhoeverStandsOnTheirArrows()
    {
        var gym = BuiltMaps.Value["MossdeepCity_Gym"];
        Assert.IsType<MossdeepTiles>(gym.Puzzle);
        // Eleven loops of four arrows, each pointing on round its loop the other way to a clock's hands
        Assert.Equal(44, MossdeepTiles.Arrows.Count);
        foreach (var arrow in MossdeepTiles.Arrows)
        {
            var (dx, dy) = FieldMovement.Delta(arrow.Way);
            var next = MossdeepTiles.ArrowAt(arrow.X + dx, arrow.Y + dy);
            Assert.True(next != null, $"the arrow at {arrow.X},{arrow.Y} points off its loop");
            Assert.Equal(arrow.Colour, next!.Value.Colour);
            Assert.False(gym.IsSolid(arrow.X, arrow.Y), $"the arrow at {arrow.X},{arrow.Y} lies on a wall");
        }
        // Each switch is a trigger of the room running its colour's script
        foreach (var (x, y, colour) in MossdeepTiles.Switches)
            Assert.Contains(gym.Triggers, t => t.X == x && t.Y == y && t.Script == $"{colour}Switch");
        // The statues are things of the map, drawn as cards
        var statues = gym.Everyone.Where(n => n.NpcType == NPC.GymStatueType).ToList();
        Assert.Equal(21, statues.Count);
        Assert.All(statues, s => Assert.Equal(PropType.GymStatue, s.GymThing));
        // The yellow switch moves the eight on yellow arrows: Preston steps east and turns from south to east
        var preston = gym.Everyone.Single(n => n.TrainerData?.Id == "hoenn_preston");
        Assert.Equal((1, 30), (preston.GridX, preston.GridY));
        var turns = MossdeepTiles.Turns(MossdeepTiles.Colour.Yellow, (x, y) => gym.NPCs.At(x, y));
        Assert.Equal(8, turns.Count);
        var his = turns.Single(t => t.Who == preston);
        Assert.Equal(Direction.Right, his.Step);
        Assert.Equal(MossdeepTiles.Anticlockwise(preston.Facing), his.Facing);
        Assert.Equal(Direction.Up, MossdeepTiles.Anticlockwise(Direction.Right));
        Assert.Equal(Direction.Left, MossdeepTiles.Anticlockwise(Direction.Up));
        // Nobody is on the red arrows but Sylvia and the statues beside her
        Assert.Contains(MossdeepTiles.Turns(MossdeepTiles.Colour.Red, (x, y) => gym.NPCs.At(x, y)), t => t.Who.TrainerData?.Id == "hoenn_sylvia");
    }

    [Fact]
    public void TheSeafloorCavernAndSootopolisCityAreComeToByDive()
    {
        // Plan 21 · H8, part b: Route 128 and its floor, the cavern and its pool, Sootopolis and the pool under its
        // harbour, the Cave of Origin
        foreach (string key in new[] { "route_128", "underwater_route_128", "underwater_seafloor_cavern", "seafloor_cavern_entrance",
                     "seafloor_cavern_room_9", "sootopolis_city", "underwater_sootopolis_city", "sootopolis_city_pokemon_center_1f",
                     "sootopolis_city_mart", "cave_of_origin_entrance", "cave_of_origin_1f", "cave_of_origin_b1f" })
            Assert.Contains(key, Hoenn.Index.Areas);

        // The passage of Route 128's floor leads into the cavern's pool, which comes up in the cavern's mouth at Emerald's
        // setdivewarp, and the mouth's dark water goes back down to the pool
        var floor = BuiltMaps.Value["HoennUnderwater"];
        Assert.Contains(floor.Warps, w => w.TargetMap == "Underwater_SeafloorCavern" && !w.Shut);
        var pool = BuiltMaps.Value["Underwater_SeafloorCavern"];
        Assert.True(pool.IsUnderwater);
        var up = pool.EmergeTargetAt(6, 5);
        Assert.Equal(new MapSpot("SeafloorCavern_Entrance", 10, 17, Direction.Up), up);
        var mouth = BuiltMaps.Value["SeafloorCavern_Entrance"];
        Assert.True(FieldMovement.SurfacesOnto(mouth, 10, 17), "the cavern's mouth has no water to come up into");
        Assert.True(FieldMovement.CanSurface(pool, 6, 5));
        Assert.Contains(mouth.Warps, w => w.TargetMap == "SeafloorCavern_Room1");

        // Sootopolis: its harbour's dark water dives to the pool under it, and the pool comes up in the harbour
        var soot = BuiltMaps.Value["SootopolisCity"];
        var dark = Enumerable.Range(0, soot.Height).SelectMany(y => Enumerable.Range(0, soot.Width).Select(x => (x, y)))
            .Where(t => soot.BehaviourAt(t.x, t.y) == TileBehavior.DiveWater).ToList();
        Assert.NotEmpty(dark);
        foreach (var (x, y) in dark)
            Assert.Equal(new MapSpot("Underwater_SootopolisCity", 9, 6, Direction.Up), soot.DiveTargetAt(x, y));
        var under = BuiltMaps.Value["Underwater_SootopolisCity"];
        Assert.False(under.IsSolid(9, 6), "the pool under Sootopolis is rock where Dive comes down");
        Assert.Equal(new MapSpot("SootopolisCity", 29, 53, Direction.Up), under.EmergeTargetAt(9, 6));
        Assert.True(FieldMovement.SurfacesOnto(soot, 29, 53), "Sootopolis's harbour has no water where the pool comes up");
        // The pool is joined to Route 126's floor by the passage under the crater's wall
        Assert.Contains(floor.Warps, w => w.TargetMap == "Underwater_SootopolisCity" && !w.Shut);
        Assert.Contains(under.Warps, w => w.TargetMap == "HoennUnderwater");

        // The city's look and song, its Pokémon Center's nurse, and its Gym and Mystery Events house shut
        Assert.Equal(Architecture.Crater, soot.ArchitectureAt(43, 30));
        Assert.Equal("hoenn/sootopolis", soot.BgmTrackAt(43, 30));
        Assert.Single(BuiltMaps.Value["SootopolisCity_PokemonCenter_1F"].Everyone, n => n.Script == "Nurse");
        // (the Gym's door waits for Archie and Maxie to leave, plan 21 · H8, part c)
        Assert.Contains(soot.Warps, w => w.TargetMap == "SootopolisCity_Gym_1F" && w.OpenedBy == "FLAG_SOOTOPOLIS_ARCHIE_MAXIE_LEAVE");
        Assert.Contains(soot.Warps, w => w.TargetMap == "CaveOfOrigin_Entrance");
        Assert.Equal(TerrainCover.Paving, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_Sootopolis", 0x2D9, MapImporter.Gba.GbaGame.Emerald));
        foreach (string song in new[] { "sootopolis", "abnormal_weather" })
            Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Data", "music", "hoenn", song + ".mml")), song);
        Assert.Equal(["Ultra Ball", "Hyper Potion", "Max Potion", "Full Heal", "Revive", "Max Repel", "X Attack", "X Defense"],
            MartDatabase.Specialties["sootopolis"]);
    }

    [Fact]
    public void PacifidlogSkyPillarAndJuansGymAreOpen()
    {
        // Plan 21 · H8, part c: Routes 129 to 131, Pacifidlog Town and its rooms, Sky Pillar and Juan's Gym
        foreach (string key in new[] { "route_129", "route_130", "route_131", "pacifidlog_town", "pacifidlog_town_pokemon_center_1f",
                     "pacifidlog_town_house_5", "sky_pillar_outside", "sky_pillar_entrance", "sky_pillar_1f", "sky_pillar_5f", "sky_pillar_top",
                     "sootopolis_city_gym_1f", "sootopolis_city_gym_b1f" })
            Assert.Contains(key, Hoenn.Index.Areas);
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        foreach (string map in new[] { "SootopolisCity_Gym_1F", "SootopolisCity_Gym_B1F", "PacifidlogTown_PokemonCenter_1F", "SkyPillar_Top" })
            Assert.Contains(map, hoenn.Maps);
        // Route 131's shore leads into Sky Pillar's cave, and the cave up to its island; the tower's door opens for Wallace
        Assert.Contains(Overworld.Warps, w => w.TargetMap == "SkyPillar_Entrance");
        Assert.Contains(BuiltMaps.Value["SkyPillar_Entrance"].Warps, w => w.TargetMap == "SkyPillar_Outside");
        Assert.Contains(BuiltMaps.Value["SkyPillar_Outside"].Warps, w => w.TargetMap == "SkyPillar_1F" && w.OpenedBy == "FLAG_WALLACE_GOES_TO_SKY_PILLAR");
        Assert.Contains(BuiltMaps.Value["SkyPillar_5F"].Warps, w => w.TargetMap == "SkyPillar_Top");
        // Pacifidlog's rafts, its song and its Pokémon Center
        Assert.Equal(Architecture.Raft, Overworld.ArchitectureAt(488, 276));
        Assert.Equal("hoenn/lilycove", Overworld.BgmTrackAt(488, 276));
        Assert.Single(BuiltMaps.Value["PacifidlogTown_PokemonCenter_1F"].Everyone, n => n.Script == "Nurse");
        Assert.Contains(Overworld.Warps, w => w.TargetMap == "PacifidlogTown_PokemonCenter_1F");
        // Juan's Gym is its ice
        Assert.IsType<SootopolisIce>(BuiltMaps.Value["SootopolisCity_Gym_1F"].Puzzle);
        Assert.Equal(TileBehavior.ThinIce, BuiltMaps.Value["SootopolisCity_Gym_1F"].BehaviourAt(8, 18));
        Assert.Contains(BuiltMaps.Value["SootopolisCity_Gym_B1F"].Warps, w => w.TargetMap == "SootopolisCity_Gym_1F");
        // Sky Pillar's floors are worn stone, not lawn
        Assert.Equal(TerrainCover.Paving, MapImporter.Gba.GbaCover.Of(TileBehavior.None, null, "gTileset_Pacifidlog", 0x234, MapImporter.Gba.GbaGame.Emerald));
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Data", "music", "hoenn", "rayquaza_appears.mml")));
        Assert.False(MusicLibrary.Get("hoenn/rayquaza_appears")!.Loops);

        // Sootopolis is flown to on its own map, from the middle of Route 126's cells
        var spawn = SpawnLocations.In(RegionDatabase.Hoenn).Single(s => s.Area == "sootopolis_city");
        Assert.Equal("SootopolisCity", spawn.Map);
        Assert.False(BuiltMaps.Value["SootopolisCity"].IsSolid(spawn.X, spawn.Y));
        Assert.Contains(RegionMap.Of(RegionDatabase.Hoenn)!.FlySpots, f => f.Spawn == spawn.Id);
    }

    [Fact]
    public void RouteOneTwentysBridgeIsADeckOverItsPondAndStevenStandsOnIt()
    {
        // Plan 21 · H8, part c: the import gives water a bridge crosses the lowest level of its shores, so the deck
        // stands over the whole pond (its west half took the level of the bridge's raised ends, and had no deck)
        for (int x = 370; x <= 381; x++)
        {
            Assert.True(Overworld.DeckAt(x, 15) is { } deck && deck > Overworld.HeightAt(x, 15) + 1f, $"Route 120's bridge has no deck at {x},15");
            Assert.Equal(Overworld.HeightAt(370, 14), Overworld.HeightAt(x, 14));
        }
        // Steven stands on the deck, where the player crossing the bridge speaks to him
        var steven = Overworld.Everyone.Single(n => n.Key == "route120_steven");
        Assert.Equal(Overworld.DeckAt(373, 15), steven.Level);
        Assert.Same(steven, Overworld.NpcIn(373, 15, Overworld.DeckAt(373, 16)!.Value));
        // Route 128's islet, where Archie, Maxie and Steven stand, is a sandbar among the deep water, not open sea
        Assert.Equal(TileType.Sand, Overworld.GetGroundTile(677, 242));
        Assert.Equal(TileBehavior.ShallowWater, Overworld.BehaviourAt(677, 242));
    }

    /// <summary>The weather and songs a map's zones hold by the story (Map.ApplyPresence does it in the game), without moving its people.</summary>
    private static void Follow(Map map, Func<string, bool> has)
    {
        foreach (var area in map.Areas)
            foreach (var zone in area.Zones)
                zone.Follow(has);
    }

    [Fact]
    public void TheTitansWeatherTakesTurnsOverTheEasternSeaWhileTheStoryKeepsIt()
    {
        // Emerald's abnormal weather: the downpour first, then the drought, ten seconds of the field's clock each
        Assert.Equal(FieldWeather.HeavyRain, Weathers.Alternating(FieldWeather.Abnormal, 0));
        Assert.Equal(FieldWeather.Drought, Weathers.Alternating(FieldWeather.Abnormal, Weathers.AbnormalTurn + 0.5));
        Assert.Equal(FieldWeather.HeavyRain, Weathers.Alternating(FieldWeather.Abnormal, 2 * Weathers.AbnormalTurn + 0.5));
        Assert.Equal(FieldWeather.Rain, Weathers.Alternating(FieldWeather.Rain, Weathers.AbnormalTurn + 0.5));
        // A drought opens a battle in the sun, and lights the field harder and warmer
        Assert.Equal(PokemonPlatinumEngine.Battle.Sim.BattleWeather.Sun, Weathers.InBattle(FieldWeather.Drought));
        Assert.False(Weathers.IsRain(FieldWeather.Drought));
        var day = PokemonPlatinumEngine.Graphics.ArtLook.FieldRigFor(TimeOfDay.Day);
        var dry = PokemonPlatinumEngine.Graphics.ArtLook.Weathered(day, FieldWeather.Drought);
        Assert.True(dry.Light.SunColor.X > day.Light.SunColor.X, "a drought's sun should be stronger");

        // Over Route 128 the weather is the area's own until the story takes it over, and its own again after
        var sea = Overworld;
        var (x, y) = (700, 245);
        Assert.Equal("route_128", sea.AreaAt(x, y)?.Key);
        try
        {
            Follow(sea, _ => false);
            Assert.Equal(FieldWeather.Clear, sea.WeatherAt(x, y));
            Assert.Equal("hoenn/route120", sea.BgmTrackAt(x, y));
            Follow(sea, flag => flag == "FLAG_SYS_WEATHER_CTRL");
            sea.WeatherClock = 0;
            Assert.Equal(FieldWeather.HeavyRain, sea.WeatherAt(x, y));
            sea.WeatherClock = Weathers.AbnormalTurn + 1;
            Assert.Equal(FieldWeather.Drought, sea.WeatherAt(x, y));
            Assert.Equal("hoenn/abnormal_weather", sea.BgmTrackAt(x, y));
            // Mossdeep City has the weather and keeps its own song
            Assert.Equal(FieldWeather.Drought, sea.WeatherAt(668, 117));
            Assert.Equal("hoenn/rustboro", sea.BgmTrackAt(668, 117));
            // Away from the eastern sea nothing changes
            Assert.Equal(FieldWeather.Clear, sea.WeatherAt(125, 291));
        }
        finally
        {
            Follow(sea, _ => false);
            sea.WeatherClock = 0;
        }

        // Sootopolis: the downpour from the titans' coming, the two by turns once their clash is seen
        var soot = BuiltMaps.Value["SootopolisCity"];
        try
        {
            Follow(soot, flag => flag == "FLAG_SYS_WEATHER_CTRL");
            soot.WeatherClock = Weathers.AbnormalTurn + 1;
            Assert.Equal(FieldWeather.HeavyRain, soot.WeatherAt(43, 30));
            Follow(soot, flag => flag is "FLAG_SYS_WEATHER_CTRL" or "FLAG_HOENN_SOOTOPOLIS_CLASH_SEEN");
            Assert.Equal(FieldWeather.Drought, soot.WeatherAt(43, 30));
            Assert.Equal("hoenn/abnormal_weather", soot.BgmTrackAt(43, 30));
        }
        finally
        {
            Follow(soot, _ => false);
            soot.WeatherClock = 0;
        }
    }
}
