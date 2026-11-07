using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
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

    private static readonly string[] Areas =
        { "vaniville_town", "kalos_route_1", "aquacorde_town", "kalos_route_2", "santalune_forest", "kalos_route_3", "santalune_city" };

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
    public void ACaveDrawnAsAMapOfItsOwnIsItsOwnMatrixWithChunksAfterTheOverworlds()
    {
        // Plan 23 · Z3, second part: a layout's maps are drawn beside the overworld, each its own matrix, its rock where
        // nothing is drawn, its floor wild ground and its way in and out told by the tiles' behaviours
        var layout = new MapDraw.Layout
        {
            Region = "Test", Width = 1, Height = 1,
            Areas = { new MapDraw.AreaRect { Key = "here", X = 0, Z = 0, Width = 32, Depth = 32 } },
            Maps = { new MapDraw.MapDrawing { Matrix = 1, Width = 1, Height = 1, Area = "cave", Cell = "0,0" } }
        };
        var overworld = new MapDraw.Piece { File = "o.txt", X = 0, Z = 0 };
        var cave = new MapDraw.Piece { File = "c.txt", X = 1, Z = 1 };
        var world = MapDraw.Drawing.Build(layout, new[] { (overworld, new[] { "M." }) },
            new IReadOnlyList<(MapDraw.Piece, string[])>[] { new[] { (cave, new[] { "rcr", "cmc" }) } });
        Assert.Equal(2, world.Chunks.Count);
        Assert.Equal(1, world.Maps!.Single().Id);
        Assert.Equal(1, world.Maps!.Single().ChunkAt(0, 0));
        var outside = world.Chunks[0];
        Assert.Equal(TileBehavior.EntranceNorth, outside.BehaviourAt(0, 0));
        Assert.Equal(TerrainCover.CaveMouth, outside.CoverAt(0, 0));
        var inside = world.Chunks[1];
        Assert.True(inside.SolidAt(1, 1));                                   // rock
        Assert.False(inside.SolidAt(2, 1));
        Assert.Equal(TileBehavior.CaveFloor, inside.BehaviourAt(2, 1));
        Assert.Equal(TileBehavior.EntranceSouth, inside.BehaviourAt(2, 2));  // the way out is taken walking south
        Assert.True(inside.SolidAt(0, 0));                                   // where nothing is drawn, rock
        Assert.Equal(TerrainCover.Rock, inside.CoverAt(0, 0));
        Assert.Equal("0,0", world.Cells!["cave"]);
    }

    [Fact]
    public void VanivilleRouteOneAndAquacordeAreOpenWithTheirHousesSongsAndSigns()
    {
        Assert.Equal(Areas, Kalos.Index.Areas.Take(Areas.Length));
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

    [Fact]
    public void Route2TheForestRoute3AndSantaluneAreOpenWithTheirWildPokemonCenterAndGym()
    {
        Assert.Equal("Route 2", Overworld.DisplayNameAt(255, 620));
        Assert.Equal("Santalune Forest", Overworld.DisplayNameAt(255, 590));
        Assert.Equal("Route 3", Overworld.DisplayNameAt(290, 527));
        Assert.Equal("Santalune City", Overworld.DisplayNameAt(340, 528));
        // The first wild Pokémon of Kalos live in Route 2's grass, Pikachu among the forest's, and nobody on the trails
        Assert.Contains(Kalos.Area("kalos_route_2")!.Land!, e => e.Species == "Fletchling");
        Assert.Contains(Kalos.Area("santalune_forest")!.Land!, e => e.Species == "Pikachu");
        Assert.Equal(12, Kalos.Area("kalos_route_3")!.Land!.Count);
        Assert.Equal(TileBehavior.TallGrass, Overworld.BehaviourAt(248, 608));
        Assert.Equal(TileBehavior.None, Overworld.BehaviourAt(255, 620));
        // Every Pokémon met there is one Kalos's own Pokédex lists, at the region's first levels
        foreach (string key in new[] { "kalos_route_2", "santalune_forest", "kalos_route_3" })
            foreach (var slot in Kalos.Area(key)!.Land!.Concat(Kalos.Area(key)!.Water ?? new()))
            {
                Assert.True(PokemonDatabase.Get(slot.Species)!.DexNumbers.ContainsKey("kalos-central"), $"{key}: {slot.Species} is no Kalos-Central species");
                Assert.InRange(slot.Level, 2, 9);
            }
        // Santalune's Pokémon Center (with the Mart's counter inside), its Gym and four houses, found at their doors
        var center = Overworld.PlacedBuildings.Single(b => b.Kind == BuildingKind.PokemonCenter && b.X0 >= 316 && b.Y0 >= 492);
        Assert.Equal("SantaluneCity_PokemonCenter_1F", Overworld.GetWarpAt(325, 519)?.TargetMap);
        Assert.True(center.X0 <= 325 && center.X1 >= 325);
        Assert.Contains(Overworld.PlacedBuildings, b => b.Kind == BuildingKind.Gym && b.X0 >= 316 && b.Y0 >= 492);
        Assert.True(Overworld.PlacedBuildings.Count(b => b.Kind == BuildingKind.House && b.X0 >= 316 && b.Y0 >= 492) >= 4, "Santalune's four houses");
        var room = MapNamed("SantaluneCity_PokemonCenter_1F");
        Assert.Contains(room.NPCs, n => n.IsHealingNurse);
        Assert.Contains(room.NPCs, n => n.IsPokeMartClerk);
        var gym = MapNamed("SantaluneCity_Gym");
        Assert.Contains(gym.NPCs, n => n.Name == "Viola");
        Assert.Equal("Leader", TrainerDatabase.Get("kalos_viola_1")!.Class);
        // Fly reaches Kalos: its Town Map lands before Santalune's Pokémon Center once the city has been come to
        var townMap = RegionMap.Of(RegionDatabase.Kalos)!;
        Assert.Equal("Kalos", townMap.Map);
        Assert.Contains(townMap.FlySpots, f => f.Spawn == 2 && f.Cells.Contains(RegionMap.CellOf(325, 520)));
        Assert.Contains("FLAG_FIRST_ARRIVAL_SANTALUNE_CITY", townMap.ArrivalFlags("santalune_city"));
    }

    [Fact]
    public void FromTheBedroomTheWayRunsThroughTheForestToSantalunesGym()
    {
        var walked = Walked.Value;
        bool Reached(string map, int x, int y) => walked.Any(p => p.Key.Name == map && p.Value.Contains((x, y)));
        Assert.True(Reached("Kalos", 255, 630), "Route 2");
        Assert.True(Reached("Kalos", 255, 600), "the forest's way in");
        Assert.True(Reached("Kalos", 259, 583), "the forest's grove");
        Assert.True(Reached("Kalos", 268, 540), "Route 3");
        Assert.True(Reached("Kalos", 341, 508), "before Santalune's Gym");
        Assert.True(Reached("SantaluneCity_Gym", 6, 5), "Viola's place");
        Assert.True(Reached("SantaluneCity_PokemonCenter_1F", 8, 6), "the Pokémon Center's counter");
    }

    [Fact]
    public void Route4AndLumioseAreOpenWithTheirGardenBoulevardsTowerAndRooms()
    {
        Assert.Equal("Route 4", Overworld.DisplayNameAt(358, 450));
        Assert.Equal("Lumiose City", Overworld.DisplayNameAt(360, 380));
        Assert.Equal(Architecture.Lumiose, Overworld.ArchitectureAt(360, 380));
        // Route 4's garden: tall grass among the beds, flower Pokémon of Kalos-Central at the region's early levels
        Assert.Equal(TileBehavior.TallGrass, Overworld.BehaviourAt(345, 450));
        foreach (var slot in Kalos.Area("kalos_route_4")!.Land!)
        {
            Assert.True(PokemonDatabase.Get(slot.Species)!.DexNumbers.ContainsKey("kalos-central"), $"{slot.Species} is no Kalos-Central species");
            Assert.InRange(slot.Level, 6, 10);
        }
        Assert.Contains(Kalos.Area("kalos_route_4")!.Land!, e => e.Species == "Flabébé");
        // The tower of light stands in the middle of the central plaza and blocks its tiles; the plaza walks round it
        var tower = Assert.Single(Overworld.Props, p => p.Type == PropType.PrismTower);
        Assert.Equal((358, 358, 4, 4), (tower.X, tower.Y, tower.Width, tower.Depth));
        Assert.False(Overworld.IsWalkable(359, 359));
        Assert.True(Overworld.IsWalkable(359, 363));
        // Every building of the city is found at its door, a block no wider than a building found at its door may be;
        // its Pokémon Center, the professor's laboratory and the cafés open, the rest shut until later sessions
        var city = Overworld.PlacedBuildings.Where(b => b.X0 >= 300 && b.X1 < 420 && b.Y0 >= 300 && b.Y1 < 420).ToList();
        Assert.True(city.Count >= 36, $"Lumiose has {city.Count} buildings");
        Assert.Single(city, b => b.Kind == BuildingKind.PokemonCenter);
        Assert.Single(city, b => b.Kind == BuildingKind.Lab);
        Assert.Equal("LumioseCity_PokemonCenter_1F", Overworld.GetWarpAt(349, 407)?.TargetMap);
        Assert.Equal("LumioseCity_SycamoreLab", Overworld.GetWarpAt(368, 387)?.TargetMap);
        Assert.Equal("LumioseCity_LysandreCafe", Overworld.GetWarpAt(367, 337)?.TargetMap);
        var center = MapNamed("LumioseCity_PokemonCenter_1F");
        Assert.Contains(center.NPCs, n => n.IsHealingNurse);
        Assert.Contains(center.NPCs, n => n.IsPokeMartClerk);
        Assert.Contains(MapNamed("LumioseCity_SycamoreLab").Everyone, n => n.Name == "Prof. Sycamore");
        Assert.Contains(MapNamed("LumioseCity_LysandreCafe").Everyone, n => n.Name == "Lysandre");
        Assert.Equal("Pokémon Professor", TrainerDatabase.Get("kalos_sycamore_1")!.Class);
        // Fly lands before Lumiose's Pokémon Center once the city has been come to
        var townMap = RegionMap.Of(RegionDatabase.Kalos)!;
        Assert.Contains(townMap.FlySpots, f => f.Spawn == 3 && f.Cells.Contains(RegionMap.CellOf(349, 408)));
        Assert.Contains("FLAG_FIRST_ARRIVAL_LUMIOSE_CITY", townMap.ArrivalFlags("lumiose_city"));
        var (cx, cy) = RegionMap.CellOf(360, 360);
        Assert.Equal("Lumiose City", townMap.BlockAt(cx, cy)?.Name);
    }

    [Fact]
    public void FromTheBedroomTheWayRunsUpRoute4AndRoundLumiosesRings()
    {
        var walked = Walked.Value;
        bool Reached(string map, int x, int y) => walked.Any(p => p.Key.Name == map && p.Value.Contains((x, y)));
        Assert.True(Reached("Kalos", 358, 480), "Route 4's promenade");
        Assert.True(Reached("Kalos", 359, 415), "Lumiose's south gate");
        // The ring of boulevard round the tower, the ring half way out and the ring along the city's edge, each walked all round
        foreach (var (x, y, what) in new[]
                 {
                     (359, 346, "the boulevard north of the tower"), (373, 359, "the boulevard east"), (359, 373, "the boulevard south"), (346, 359, "the boulevard west"),
                     (359, 326, "the middle ring north"), (392, 359, "the middle ring east"), (327, 359, "the middle ring west"),
                     (359, 305, "the outer ring north"), (414, 359, "the outer ring east"), (304, 359, "the outer ring west"),
                 })
            Assert.True(Reached("Kalos", x, y), what);
        // The five plazas
        foreach (var (x, y, what) in new[] { (355, 313, "Jaune"), (309, 356, "Vert"), (401, 356, "Rouge"), (388, 324, "Magenta"), (388, 390, "Bleu") })
            Assert.True(Reached("Kalos", x, y), $"{what} Plaza");
        Assert.True(Reached("LumioseCity_SycamoreLab", 7, 5), "the professor's laboratory");
        Assert.True(Reached("LumioseCity_LysandreCafe", 6, 7), "Lysandre's café");
        Assert.True(Reached("LumioseCity_PokemonCenter_1F", 8, 6), "Lumiose's Pokémon Center");
    }

    [Fact]
    public void Route5ToCyllageAreOpenWithTheirWildPokemonRoomsAndGym()
    {
        foreach (var (x, y, name) in new[]
                 {
                     (260, 359, "Route 5"), (201, 360, "Camphrier Town"), (201, 300, "Route 6"),
                     (160, 398, "Route 7"), (103, 390, "Route 8"), (103, 330, "Cyllage City")
                 })
            Assert.Equal(name, Overworld.DisplayNameAt(x, y));
        Assert.Equal(Architecture.OldTimber, Overworld.ArchitectureAt(201, 360));
        Assert.Equal(Architecture.Cyllage, Overworld.ArchitectureAt(103, 330));
        foreach (string key in new[] { "kalos_route_5", "camphrier_town", "kalos_route_6", "kalos_route_7", "kalos_route_8", "cyllage_city" })
            Assert.False(string.IsNullOrEmpty(Kalos.Overlay(key)?.BgmTrack), $"{key} has no song");
        // Every wild Pokémon of the new routes is one of Kalos's own Pokédexes' (the coast's are Kalos-Coastal's), at the
        // levels between the Bug Badge and the Cliff Badge
        foreach (string key in new[] { "kalos_route_5", "kalos_route_6", "kalos_route_7", "kalos_route_8" })
        {
            Assert.Equal(12, Kalos.Area(key)!.Land!.Count);
            foreach (var slot in Kalos.Area(key)!.Land!)
            {
                var dex = PokemonDatabase.Get(slot.Species)!.DexNumbers;
                Assert.True(dex.ContainsKey("kalos-central") || dex.ContainsKey("kalos-coastal"), $"{key}: {slot.Species} is no Kalos species");
                Assert.InRange(slot.Level, 9, 15);
            }
        }
        // Camphrier's Pokémon Center and castle and Cyllage's Pokémon Center and Gym, found at their doors
        Assert.Equal("CamphrierTown_PokemonCenter_1F", Overworld.GetWarpAt(186, 355)?.TargetMap);
        Assert.Equal("CamphrierTown_Castle", Overworld.GetWarpAt(214, 345)?.TargetMap);
        Assert.Equal("CyllageCity_PokemonCenter_1F", Overworld.GetWarpAt(112, 347)?.TargetMap);
        Assert.Equal("CyllageCity_Gym", Overworld.GetWarpAt(103, 311)?.TargetMap);
        Assert.Contains(Overworld.PlacedBuildings, b => b.Kind == BuildingKind.PokemonCenter && b.X0 >= 176 && b.X1 < 228 && b.Y0 >= 328);
        Assert.Contains(Overworld.PlacedBuildings, b => b.Kind == BuildingKind.Gym && b.X0 >= 72 && b.X1 < 128 && b.Y0 >= 296 && b.Y1 < 360);
        foreach (string pc in new[] { "CamphrierTown_PokemonCenter_1F", "CyllageCity_PokemonCenter_1F" })
        {
            Assert.Contains(MapNamed(pc).NPCs, n => n.IsHealingNurse);
            Assert.Contains(MapNamed(pc).NPCs, n => n.IsPokeMartClerk);
        }
        Assert.Contains(MapNamed("CyllageCity_Gym").NPCs, n => n.Name == "Grant");
        var grant = TrainerDatabase.Get("kalos_grant_1")!;
        Assert.Equal("Leader", grant.Class);
        Assert.Equal(new[] { "Amaura", "Tyrunt" }, grant.Party.Select(p => p.Species));
        Assert.Equal("Cliff", Region.BadgeNames[1]);
        // The sleeping Snorlax blocks the lane south of Camphrier between its rocks until the palace's flute wakes it
        var snorlax = Overworld.Everyone.Single(n => n.Name == "Snorlax");
        Assert.Equal((202, 392), (snorlax.GridX, snorlax.GridY));
        Assert.Equal("FLAG_KALOS_HIDE_SNORLAX_ROUTE_7", snorlax.HiddenBy);
        Assert.False(Overworld.IsWalkable(201, 392));
        Assert.False(Overworld.IsWalkable(203, 392));
        // Lumiose's boutique sells its own clothes, and four more of its doors are open
        Assert.Equal("LumioseCity_Boutique", Overworld.GetWarpAt(353, 337)?.TargetMap);
        Assert.Contains(ClothingDatabase.Stock("lumiose"), g => g.Id == "trench_coat");
        Assert.All(ClothingDatabase.Stock("lumiose"), g => Assert.True(g.Price > 0));
        foreach (var (x, y, room) in new[] { (379, 357, "LumioseCity_StoneEmporium"), (340, 407, "LumioseCity_SouthwestHouse"), (379, 407, "LumioseCity_SoutheastHouse") })
            Assert.Equal(room, Overworld.GetWarpAt(x, y)?.TargetMap);
        // Fly lands before the two new Pokémon Centers once their towns have been come to
        var townMap = RegionMap.Of(RegionDatabase.Kalos)!;
        Assert.Contains(townMap.FlySpots, f => f.Spawn == 4 && f.Cells.Contains(RegionMap.CellOf(186, 356)));
        Assert.Contains(townMap.FlySpots, f => f.Spawn == 5 && f.Cells.Contains(RegionMap.CellOf(112, 348)));
        Assert.Contains("FLAG_FIRST_ARRIVAL_CAMPHRIER_TOWN", townMap.ArrivalFlags("camphrier_town"));
        Assert.Contains("FLAG_FIRST_ARRIVAL_CYLLAGE_CITY", townMap.ArrivalFlags("cyllage_city"));
    }

    [Fact]
    public void FromTheBedroomTheWayRunsWestThroughCamphrierAndAlongTheCoastToCyllagesGym()
    {
        var walked = Walked.Value;
        bool Reached(string map, int x, int y) => walked.Any(p => p.Key.Name == map && p.Value.Contains((x, y)));
        Assert.True(Reached("Kalos", 299, 359), "Lumiose's west gate onto Route 5");
        Assert.True(Reached("Kalos", 260, 359), "Route 5's skaters' square");
        Assert.True(Reached("Kalos", 201, 360), "Camphrier's crossroads");
        Assert.True(Reached("Kalos", 214, 346), "the castle's bridge");
        Assert.True(Reached("Kalos", 201, 269), "the palace gate on Route 6");
        Assert.True(Reached("Kalos", 202, 396), "Route 7 past the sleeping Snorlax");
        Assert.True(Reached("Kalos", 121, 398), "Route 7's west end");
        Assert.True(Reached("Kalos", 103, 380), "Route 8's cliff path");
        Assert.True(Reached("Kalos", 103, 357), "the foot of Cyllage's main street");
        Assert.True(Reached("Kalos", 103, 312), "before Cyllage's Gym");
        Assert.True(Reached("CyllageCity_Gym", 6, 5), "Grant's place");
        Assert.True(Reached("CamphrierTown_Castle", 7, 6), "the castle's hall");
        Assert.True(Reached("LumioseCity_Boutique", 7, 7), "the boutique's counter");
        // Route 8's coast path south to Ambrette was shut by a rockfall until plan 23 · Z3, second part
        Assert.True(Reached("Kalos", 103, 423), "Route 8 past the old rockfall");
    }

    [Fact]
    public void KalosStoryRunsFromLysandresCafeToTheCliffBadgeInOrder()
    {
        // The objectives follow VAR_KALOS_STORY on past the café, and "nothing presses" is still the last
        var story = new StoryState();
        string? Next() => Objectives.Current(story, RegionDatabase.Kalos)?.Name;
        story.SetVar("VAR_KALOS_STORY", 12);
        Assert.Equal("kalosRoute5", Next());
        foreach (var (value, name) in new[] { (13, "kalosCamphrier"), (14, "kalosPalace"), (15, "kalosSnorlax"), (16, "kalosCyllage"), (17, "kalosGrant"), (18, "kalosRoute10") })
        {
            story.SetVar("VAR_KALOS_STORY", value);
            Assert.Equal(name, Next());
        }
        Assert.Equal(599, Objectives.Find("kalosFree")!.Id);
        // The scenes move the count on one step each, and the Gym's script gives the Cliff Badge
        var library = ScriptLibrary.Default;
        foreach (var (file, script) in new[]
                 {
                     ("kalos_route_5", "Friends"), ("camphrier_town", "Castellan"), ("kalos_route_6", "Butler"),
                     ("kalos_route_7", "Snorlax"), ("cyllage_city", "Neighbour"), ("CyllageCity_Gym", "Grant")
                 })
            Assert.NotNull(library.Find(script, file));
    }

    [Fact]
    public void Route10ToShalourAreOpenWithTheirWildPokemonRoomsGymAndTower()
    {
        foreach (var (x, y, name) in new[]
                 {
                     (119, 270, "Route 10"), (120, 210, "Geosenge Town"), (165, 190, "Route 11"), (165, 130, "Shalour City")
                 })
            Assert.Equal(name, Overworld.DisplayNameAt(x, y));
        Assert.Equal(Architecture.OldTimber, Overworld.ArchitectureAt(120, 210));
        Assert.Equal(Architecture.Cyllage, Overworld.ArchitectureAt(165, 130));
        foreach (string key in new[] { "kalos_route_10", "geosenge_town", "kalos_route_11", "shalour_city" })
            Assert.False(string.IsNullOrEmpty(Kalos.Overlay(key)?.BgmTrack), $"{key} has no song");
        // Every wild Pokémon of the two routes is one of Kalos's own Pokédexes', at the levels between the Cliff Badge
        // and the Rumble Badge
        foreach (string key in new[] { "kalos_route_10", "kalos_route_11" })
        {
            Assert.Equal(12, Kalos.Area(key)!.Land!.Count);
            foreach (var slot in Kalos.Area(key)!.Land!)
            {
                var dex = PokemonDatabase.Get(slot.Species)!.DexNumbers;
                Assert.True(dex.ContainsKey("kalos-central") || dex.ContainsKey("kalos-coastal"), $"{key}: {slot.Species} is no Kalos species");
                Assert.InRange(slot.Level, 17, 21);
            }
        }
        // The road north out of Cyllage is open, and the two towns' Pokémon Centers, Shalour's Gym and its tower are
        // found at their doors
        Assert.True(Overworld.IsWalkable(119, 297));
        Assert.Equal("GeosengeTown_PokemonCenter_1F", Overworld.GetWarpAt(127, 214)?.TargetMap);
        Assert.Equal("ShalourCity_PokemonCenter_1F", Overworld.GetWarpAt(155, 122)?.TargetMap);
        Assert.Equal("ShalourCity_Gym", Overworld.GetWarpAt(181, 123)?.TargetMap);
        Assert.Equal("ShalourCity_TowerOfMastery", Overworld.GetWarpAt(165, 109)?.TargetMap);
        Assert.Contains(Overworld.PlacedBuildings, b => b.Kind == BuildingKind.Tower && b.X0 >= 136 && b.X1 < 196 && b.Y1 < 150);
        Assert.Contains(Overworld.PlacedBuildings, b => b.Kind == BuildingKind.Gym && b.X0 >= 136 && b.X1 < 196 && b.Y1 < 150);
        foreach (string pc in new[] { "GeosengeTown_PokemonCenter_1F", "ShalourCity_PokemonCenter_1F" })
        {
            Assert.Contains(MapNamed(pc).NPCs, n => n.IsHealingNurse);
            Assert.Contains(MapNamed(pc).NPCs, n => n.IsPokeMartClerk);
        }
        Assert.Contains(MapNamed("ShalourCity_Gym").NPCs, n => n.Name == "Korrina");
        Assert.Contains(MapNamed("ShalourCity_TowerOfMastery").NPCs, n => n.Name == "Gurkinn");
        var korrina = TrainerDatabase.Get("kalos_korrina_1")!;
        Assert.Equal("Leader", korrina.Class);
        Assert.Equal("Rumble", Region.BadgeNames[2]);
        // At the tower Korrina battles with a Key Stone of her own and a Lucario holding its stone
        var tower = TrainerDatabase.Get("kalos_korrina_tower")!;
        Assert.True(tower.KeyStone);
        Assert.Contains(tower.Party, p => p.Species == "Lucario" && p.Item == "Lucarionite");
        // Fly lands before the two new Pokémon Centers once their towns have been come to
        var townMap = RegionMap.Of(RegionDatabase.Kalos)!;
        Assert.Contains(townMap.FlySpots, f => f.Spawn == 6 && f.Cells.Contains(RegionMap.CellOf(127, 215)));
        Assert.Contains(townMap.FlySpots, f => f.Spawn == 7 && f.Cells.Contains(RegionMap.CellOf(155, 123)));
        Assert.Contains("FLAG_FIRST_ARRIVAL_GEOSENGE_TOWN", townMap.ArrivalFlags("geosenge_town"));
        Assert.Contains("FLAG_FIRST_ARRIVAL_SHALOUR_CITY", townMap.ArrivalFlags("shalour_city"));
    }

    [Fact]
    public void FromTheBedroomTheWayRunsNorthFromCyllageToShalourAndItsTower()
    {
        var walked = Walked.Value;
        bool Reached(string map, int x, int y) => walked.Any(p => p.Key.Name == map && p.Value.Contains((x, y)));
        Assert.True(Reached("Kalos", 119, 296), "Cyllage's road north");
        Assert.True(Reached("Kalos", 119, 260), "Route 10 between the stones");
        Assert.True(Reached("Kalos", 120, 210), "Geosenge's street");
        Assert.True(Reached("Kalos", 150, 205), "Route 11 out of Geosenge");
        Assert.True(Reached("Kalos", 165, 155), "Route 11's north end");
        Assert.True(Reached("Kalos", 165, 110), "before the Tower of Mastery");
        Assert.True(Reached("ShalourCity_TowerOfMastery", 7, 8), "the tower's hall");
        Assert.True(Reached("ShalourCity_Gym", 6, 5), "Korrina's place");
        Assert.True(Reached("GeosengeTown_PokemonCenter_1F", 8, 6), "Geosenge's Pokémon Center");
    }

    [Fact]
    public void RouteEightRunsOnSouthToAmbretteTownAndRouteNineEastWithTheirRoomsTrainersAndPokemon()
    {
        foreach (var (x, y, name) in new[] { (103, 440, "Route 8"), (103, 478, "Ambrette Town"), (170, 483, "Route 9"), (220, 483, "Route 12"), (250, 483, "Coumarine City") })
            Assert.Equal(name, Overworld.DisplayNameAt(x, y));
        Assert.Equal(Architecture.Cyllage, Overworld.ArchitectureAt(103, 478));
        // The rockfall that shut Route 8 is gone: its path runs on south through the town to the pier
        for (int z = 415; z < 496; z++) Assert.True(Overworld.IsWalkable(103, z), $"the way south is shut at {z}");
        Assert.Equal("AmbretteTown_WestHouse", Overworld.GetWarpAt(89, 474)?.TargetMap);
        Assert.Equal("AmbretteTown_PokemonCenter_1F", Overworld.GetWarpAt(97, 474)?.TargetMap);
        Assert.Equal("AmbretteTown_Aquarium", Overworld.GetWarpAt(114, 474)?.TargetMap);
        Assert.Equal("AmbretteTown_Cottage", Overworld.GetWarpAt(115, 491)?.TargetMap);
        // Coumarine City: its Pokémon Center, two houses and, since plan 23 · Z4, its Gym open
        Assert.Equal("CoumarineCity_PokemonCenter_1F", Overworld.GetWarpAt(243, 472)?.TargetMap);
        Assert.Equal("CoumarineCity_GardenHouse", Overworld.GetWarpAt(253, 472)?.TargetMap);
        Assert.Equal("CoumarineCity_EastHouse", Overworld.GetWarpAt(280, 472)?.TargetMap);
        Assert.Equal("CoumarineCity_Gym", Overworld.GetWarpAt(268, 472)?.TargetMap);
        Assert.Contains(MapNamed("CoumarineCity_PokemonCenter_1F").NPCs, n => n.IsHealingNurse);
        Assert.Contains(MapNamed("CoumarineCity_PokemonCenter_1F").NPCs, n => n.IsPokeMartClerk);
        Assert.Equal(4, Overworld.PlacedBuildings.Count(b => b.X0 >= 72 && b.X1 < 128 && b.Y0 >= 462 && b.Y1 < 506));
        Assert.Contains(MapNamed("AmbretteTown_PokemonCenter_1F").NPCs, n => n.IsHealingNurse);
        Assert.Contains(MapNamed("AmbretteTown_PokemonCenter_1F").NPCs, n => n.IsPokeMartClerk);
        Assert.False(string.IsNullOrEmpty(Kalos.Overlay("ambrette_town")?.BgmTrack));
        // Route 9's wild Pokémon are Kalos's own, a step above Route 11's
        var route9 = Kalos.Area("kalos_route_9")!;
        Assert.Equal(12, route9.Land!.Count);
        foreach (var slot in route9.Land)
        {
            var dex = PokemonDatabase.Get(slot.Species)!.DexNumbers;
            Assert.True(dex.ContainsKey("kalos-central") || dex.ContainsKey("kalos-coastal"), $"{slot.Species} is no Kalos species");
            Assert.InRange(slot.Level, 17, 19);
        }
        // Every trainer of the new stretches is a record of Kalos's table, in the class the overlay's look says
        foreach (string key in new[] { "kalos_route_8", "kalos_route_9", "kalos_route_12", "glittering_cave", "reflection_cave" })
            foreach (var obj in Kalos.Area(key)!.Objects.Where(o => o.Script.StartsWith("TRAINER_", StringComparison.Ordinal)))
                Assert.NotNull(TrainerDatabase.Get("kalos_" + obj.Script["TRAINER_".Length..].ToLowerInvariant()));
        Assert.Equal("Hiker", TrainerDatabase.Get("kalos_hiker_aurelien")!.Class);
        Assert.Equal("Backpacker", TrainerDatabase.Get("kalos_backpacker_solene")!.Class);
        var townMap = RegionMap.Of(RegionDatabase.Kalos)!;
        Assert.Contains(townMap.FlySpots, f => f.Spawn == 8 && f.Cells.Contains(RegionMap.CellOf(97, 475)));
        Assert.Contains("FLAG_FIRST_ARRIVAL_AMBRETTE_TOWN", townMap.ArrivalFlags("ambrette_town"));
        Assert.Contains(townMap.FlySpots, f => f.Spawn == 9 && f.Cells.Contains(RegionMap.CellOf(243, 473)));
        Assert.Contains("FLAG_FIRST_ARRIVAL_COUMARINE_CITY", townMap.ArrivalFlags("coumarine_city"));
        // Route 12's wild Pokémon are Kalos's own, at 20 to 22
        foreach (var slot in Kalos.Area("kalos_route_12")!.Land!)
        {
            var dex = PokemonDatabase.Get(slot.Species)!.DexNumbers;
            Assert.True(dex.ContainsKey("kalos-central") || dex.ContainsKey("kalos-coastal"), $"{slot.Species} is no Kalos species");
            Assert.InRange(slot.Level, 20, 22);
        }
    }

    [Fact]
    public void ReflectionCaveAndGlitteringCaveAreMapsOfTheirOwnJoinedToTheRoadsByTheirMouths()
    {
        foreach (var (mouthX, mouthY, cave) in new[] { (172, 174, "ReflectionCave"), (168, 474, "GlitteringCave") })
        {
            // The mouth is a tile of the road that is taken walking north, and comes out of the cave one step inside its way out
            Assert.Equal(TileBehavior.EntranceNorth, Overworld.BehaviourAt(mouthX, mouthY));
            var inWarp = Overworld.GetWarpAt(mouthX, mouthY);
            Assert.Equal(cave, inWarp?.TargetMap);
            var map = MapNamed(cave);
            Assert.True(map.IsWalkable(inWarp!.TargetX, inWarp.TargetY), $"{cave}: nobody can stand where the mouth leads");
            Assert.Equal(TileBehavior.CaveFloor, map.BehaviourAt(inWarp.TargetX, inWarp.TargetY));
            var outWarp = map.GetWarpAt(inWarp.TargetX, inWarp.TargetY + 1);
            Assert.Equal("Kalos", outWarp?.TargetMap);
            Assert.Equal(mouthX, outWarp!.TargetX);
            Assert.Equal(mouthY + 1, outWarp.TargetY);
            Assert.True(Overworld.IsWalkable(outWarp.TargetX, outWarp.TargetY));
            Assert.True(map.IsStreamed);
            Assert.False(map.IsDark);
        }
        Assert.Equal("Glittering Cave", MapNamed("GlitteringCave").DisplayNameAt(23, 34));
        Assert.Equal("Reflection Cave", MapNamed("ReflectionCave").DisplayNameAt(21, 30));
        // A wild Pokémon is met on the cave's floor, of the level its caves' tables give
        foreach (var (key, low, high) in new[] { ("reflection_cave", 18, 20), ("glittering_cave", 20, 22) })
        {
            Assert.Equal(12, Kalos.Area(key)!.Land!.Count);
            foreach (var slot in Kalos.Area(key)!.Land!) Assert.InRange(slot.Level, low, high);
        }
        Assert.True(TileBehaviors.HasEncounters(TileBehavior.CaveFloor));
        Assert.Equal(2, Kalos.Area("glittering_cave")!.Objects.Count(o => o.Script.StartsWith("TRAINER_", StringComparison.Ordinal)));
        Assert.Equal(2, Kalos.Area("reflection_cave")!.Objects.Count(o => o.Script.StartsWith("TRAINER_", StringComparison.Ordinal)));
    }

    [Fact]
    public void FromTheBedroomTheCoastRoadReachesAmbretteRouteNineAndBothCaves()
    {
        var walked = Walked.Value;
        bool Reached(string map, int x, int y) => walked.Any(p => p.Key.Name == map && p.Value.Contains((x, y)));
        Assert.True(Reached("Kalos", 103, 440), "Route 8 south of the old rockfall");
        Assert.True(Reached("Kalos", 103, 484), "Ambrette's street");
        Assert.True(Reached("Kalos", 103, 497), "Ambrette's pier");
        Assert.True(Reached("AmbretteTown_PokemonCenter_1F", 8, 6), "Ambrette's Pokémon Center");
        Assert.True(Reached("AmbretteTown_Aquarium", 4, 8), "the aquarium");
        Assert.True(Reached("Kalos", 140, 483), "Route 9 outside the town");
        Assert.True(Reached("Kalos", 200, 483), "Route 9's far end");
        Assert.True(Reached("Kalos", 220, 483), "Route 12 under its flowers");
        Assert.True(Reached("Kalos", 250, 483), "Coumarine's street");
        Assert.True(Reached("Kalos", 262, 495), "Coumarine's garden");
        Assert.True(Reached("CoumarineCity_PokemonCenter_1F", 8, 6), "Coumarine's Pokémon Center");
        Assert.True(Reached("CoumarineCity_EastHouse", 4, 8), "the east house");
        // Reflection Cave, off Route 11: its corridor, its long hall and its north chamber
        Assert.True(Reached("ReflectionCave", 21, 28), "Reflection Cave's corridor");
        Assert.True(Reached("ReflectionCave", 12, 18), "Reflection Cave's hall");
        Assert.True(Reached("ReflectionCave", 28, 10), "Reflection Cave's north chamber");
        Assert.True(Reached("ReflectionCave", 6, 14), "Reflection Cave's west pocket");
        // Glittering Cave, off Route 9
        Assert.True(Reached("GlitteringCave", 23, 30), "Glittering Cave's corridor");
        Assert.True(Reached("GlitteringCave", 20, 22), "Glittering Cave's chamber");
        Assert.True(Reached("GlitteringCave", 28, 10), "Glittering Cave's north chamber");
        Assert.True(Reached("GlitteringCave", 8, 10), "Glittering Cave's west pocket");
    }

    [Fact]
    public void KalosStoryRunsFromTheCliffBadgeToTheKeyStoneInOrder()
    {
        var story = new StoryState();
        string? Next() => Objectives.Current(story, RegionDatabase.Kalos)?.Name;
        story.SetVar("VAR_KALOS_STORY", 18);
        Assert.Equal("kalosRoute10", Next());
        foreach (var (value, name) in new[] { (19, "kalosShalour"), (20, "kalosKorrina"), (21, "kalosMastery"), (22, "kalosRamos") })
        {
            story.SetVar("VAR_KALOS_STORY", value);
            Assert.Equal(name, Next());
        }
        var library = ScriptLibrary.Default;
        foreach (var (file, script) in new[]
                 {
                     ("geosenge_town", "Trevor"), ("ShalourCity_TowerOfMastery", "Guru"), ("ShalourCity_Gym", "Korrina")
                 })
            Assert.NotNull(library.Find(script, file));
    }

    /// <summary>The guru's scene after the Rumble Badge, played on a game with no screen: the bag and the team afterwards.</summary>
    private static HeadlessScriptHost PlayTheMastery(StoryState story, Inventory bag, Party party)
    {
        story.SetVar("VAR_KALOS_STORY", 21);
        var tower = MapNamed("ShalourCity_TowerOfMastery");
        var host = new HeadlessScriptHost(story, party, bag) { Map = tower };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Guru", "ShalourCity_TowerOfMastery")!, tower.NPCs.Single(n => n.Name == "Gurkinn"));
        runner.RunToEnd();
        return host;
    }

    [Fact]
    public void TheTowerGivesAKeyStoneOnlyToABagWithoutOneAndItsLucarioOnce()
    {
        var keyStone = ItemDatabase.Get(BattleCore.KeyStoneItem)!;
        // A bag with no Key Stone is given one, and the team a Lucario holding its Lucarionite
        var story = new StoryState();
        var bag = new Inventory();
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Froakie")!, 30));
        PlayTheMastery(story, bag, party);
        Assert.Equal(1, bag.GetQuantity(keyStone));
        var lucario = party.Members.Single(p => p.Species.Name == "Lucario");
        Assert.Equal("Lucarionite", lucario.HeldItem?.Name);
        Assert.Equal(22, story.Var("VAR_KALOS_STORY"));
        Assert.True(story.Has("FLAG_KALOS_RECEIVED_LUCARIO"));
        // The scene played again gives neither a second stone nor a second Lucario
        PlayTheMastery(story, bag, party);
        Assert.Equal(1, bag.GetQuantity(keyStone));
        Assert.Single(party.Members, p => p.Species.Name == "Lucario");
        // A bag that already holds a Key Stone (a Champion from Hoenn's crossing, say) is given none
        var other = new Inventory();
        other.AddItem(keyStone, 1);
        var team = new Party();
        team.Add(new Pokemon(PokemonDatabase.Get("Froakie")!, 30));
        PlayTheMastery(new StoryState(), other, team);
        Assert.Equal(1, other.GetQuantity(keyStone));
        Assert.Contains(team.Members, p => p.Species.Name == "Lucario");
    }

    [Fact]
    public void TheTowersLucarioMegaEvolvesWithTheKeyStoneItGave()
    {
        var bag = new Inventory();
        var party = new Party();
        PlayTheMastery(new StoryState(), bag, party);
        var lucario = party.Members.Single(p => p.Species.Name == "Lucario");
        var core = new PokemonPlatinumEngine.Battle.Sim.BattleCore(new PokemonPlatinumEngine.Battle.Sim.CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { new(PokemonDatabase.Get("Magikarp")!, 20) }, Random = CoreScenario.Calm(),
            Rules = Ruleset.Modern, PlayerController = null, EnemyController = null, PlayerName = "Kalos",
            KeyStone = bag.AllItems.Any(s => s.Name == BattleCore.KeyStoneItem)
        });
        core.Start();
        core.Submit(BattleChoice.Fight(CoreScenario.Mine, 0) with { Mega = true }, BattleChoice.Fight(CoreScenario.Foe, 0));
        // (the battle may be over within the turn, which gives the form back, so the line says it)
        Assert.Contains("Lucario has Mega Evolved into Mega Lucario!", CoreScenario.Lines(core.TakeLog()));
    }

    // ------------------------------------------------------------------ plan 23 · Z4

    /// <summary>The places of plan 23 · Z4, with the levels their wild Pokémon are met at.</summary>
    private static readonly (string Key, int Low, int High)[] LastPlaces =
    {
        ("kalos_route_13", 26, 28), ("kalos_route_14", 30, 32), ("laverre_city", 0, 0), ("kalos_route_15", 33, 35),
        ("dendemille_town", 0, 0), ("kalos_route_17", 36, 38), ("anistar_city", 0, 0), ("kalos_route_18", 40, 42),
        ("couriway_town", 0, 0), ("kalos_route_19", 42, 44), ("snowbelle_city", 0, 0), ("kalos_route_20", 44, 46),
        ("pokemon_village", 46, 48), ("kalos_route_21", 48, 50), ("kalos_victory_road", 50, 58), ("kalos_pokemon_league", 0, 0)
    };

    [Fact]
    public void TheMountainTheEastTheValleysAndTheLeagueAreOpenWithTheirWildPokemonRoomsAndGyms()
    {
        foreach (var (x, y, name) in new[]
                 {
                     (259, 420, "Route 13"), (359, 270, "Route 14"), (360, 205, "Laverre City"), (430, 206, "Route 15"),
                     (500, 206, "Dendemille Town"), (560, 206, "Route 17"), (630, 206, "Anistar City"), (633, 280, "Route 18"),
                     (634, 357, "Couriway Town"), (687, 340, "Route 19"), (687, 270, "Snowbelle City"), (688, 160, "Route 20"),
                     (688, 130, "Pokémon Village"), (630, 127, "Route 21"), (608, 70, "Pokémon League")
                 })
            Assert.Equal(name, Overworld.DisplayNameAt(x, y));
        foreach (var (key, low, high) in LastPlaces)
        {
            Assert.Contains(key, Kalos.Index.Areas);
            Assert.False(string.IsNullOrEmpty(Kalos.Overlay(key)?.BgmTrack), $"{key} has no song");
            if (low == 0) { Assert.Null(Kalos.Area(key)!.Land); continue; }
            Assert.Equal(12, Kalos.Area(key)!.Land!.Count);
            foreach (var slot in Kalos.Area(key)!.Land!)
            {
                Assert.True(PokemonDatabase.Get(slot.Species)!.DexNumbers.Keys.Any(k => k.StartsWith("kalos-", StringComparison.Ordinal)), $"{key}: {slot.Species} is no Kalos species");
                Assert.InRange(slot.Level, low, high);
            }
        }
        // The rooms found at their doors: the Gyms, the Pokémon Centers (each with the Mart's counter inside), the
        // Power Plant and the League's hall
        foreach (var (x, y, room) in new[]
                 {
                     (268, 472, "CoumarineCity_Gym"), (288, 392, "KalosPowerPlant"), (360, 193, "LaverreCity_Gym"),
                     (335, 200, "LaverreCity_PokemonCenter_1F"), (487, 198, "DendemilleTown_PokemonCenter_1F"),
                     (636, 189, "AnistarCity_Gym"), (615, 198, "AnistarCity_PokemonCenter_1F"), (613, 350, "CouriwayTown_PokemonCenter_1F"),
                     (673, 255, "SnowbelleCity_Gym"), (699, 270, "SnowbelleCity_PokemonCenter_1F"), (608, 59, "KalosLeague_Hall")
                 })
            Assert.Equal(room, Overworld.GetWarpAt(x, y)?.TargetMap);
        foreach (string pc in new[] { "LaverreCity_PokemonCenter_1F", "DendemilleTown_PokemonCenter_1F", "AnistarCity_PokemonCenter_1F", "CouriwayTown_PokemonCenter_1F", "SnowbelleCity_PokemonCenter_1F", "KalosLeague_Hall" })
        {
            Assert.Contains(MapNamed(pc).NPCs, n => n.IsHealingNurse);
            Assert.Contains(MapNamed(pc).NPCs, n => n.IsPokeMartClerk);
        }
        // The last five Leaders, the Elite Four and the Champion, each of the table, in their rooms
        foreach (var (room, who, id, cls) in new[]
                 {
                     ("CoumarineCity_Gym", "Ramos", "kalos_ramos_1", "Leader"), ("LumioseCity_PrismTowerGym", "Clemont", "kalos_clemont_1", "Leader"),
                     ("LaverreCity_Gym", "Valerie", "kalos_valerie_1", "Leader"), ("AnistarCity_Gym", "Olympia", "kalos_olympia_1", "Leader"),
                     ("SnowbelleCity_Gym", "Wulfric", "kalos_wulfric_1", "Leader"), ("KalosLeague_MalvaRoom", "Malva", "kalos_malva_1", "Elite Four"),
                     ("KalosLeague_SieboldRoom", "Siebold", "kalos_siebold_1", "Elite Four"), ("KalosLeague_WikstromRoom", "Wikstrom", "kalos_wikstrom_1", "Elite Four"),
                     ("KalosLeague_DrasnaRoom", "Drasna", "kalos_drasna_1", "Elite Four"), ("KalosLeague_ChampionRoom", "Diantha", "kalos_diantha_1", "Champion")
                 })
        {
            Assert.Contains(MapNamed(room).NPCs, n => n.Name == who);
            Assert.Equal(cls, TrainerDatabase.Get(id)!.Class);
        }
        Assert.Equal(new[] { "Plant", "Voltage", "Fairy", "Psychic", "Iceberg" }, Region.BadgeNames.Skip(3));
        var diantha = TrainerDatabase.Get("kalos_diantha_1")!;
        Assert.True(diantha.KeyStone);
        Assert.Contains(diantha.Party, p => p.Species == "Gardevoir" && p.Item == "Gardevoirite");
        // Victory Road: a cave of its own, its south mouth on Route 21 and its north mouth on the League's terrace
        var road = MapNamed("KalosVictoryRoad");
        Assert.True(road.IsStreamed);
        var south = Overworld.GetWarpAt(605, 120);
        Assert.Equal("KalosVictoryRoad", south?.TargetMap);
        Assert.True(road.IsWalkable(south!.TargetX, south.TargetY));
        var north = Overworld.GetWarpAt(606, 95);
        Assert.Equal("KalosVictoryRoad", north?.TargetMap);
        Assert.True(road.IsWalkable(north!.TargetX, north.TargetY));
        // Fly lands before the five new Pokémon Centers and the League's hall once their places have been come to
        var townMap = RegionMap.Of(RegionDatabase.Kalos)!;
        foreach (var (spawn, x, y, flag) in new[]
                 {
                     (10, 335, 201, "LAVERRE_CITY"), (11, 487, 199, "DENDEMILLE_TOWN"), (12, 615, 199, "ANISTAR_CITY"),
                     (13, 613, 351, "COURIWAY_TOWN"), (14, 699, 271, "SNOWBELLE_CITY"), (15, 608, 60, "KALOS_POKEMON_LEAGUE")
                 })
        {
            Assert.Contains(townMap.FlySpots, f => f.Spawn == spawn && f.Cells.Contains(RegionMap.CellOf(x, y)));
            Assert.Contains("FLAG_FIRST_ARRIVAL_" + flag, townMap.ArrivalFlags(flag.ToLowerInvariant()));
        }
    }

    [Fact]
    public void FromTheBedroomTheWayRunsOnToTheLeagueAndEveryRoomOfTheLastChapters()
    {
        var walked = Walked.Value;
        bool Reached(string map, int x, int y) => walked.Any(p => p.Key.Name == map && p.Value.Contains((x, y)));
        foreach (var (x, y, what) in new[]
                 {
                     (276, 376, "Route 5's way south onto Route 13"), (259, 450, "Route 13's south end"), (259, 461, "Coumarine's way north"),
                     (359, 299, "Lumiose's north gate"), (359, 250, "Route 14"), (360, 205, "Laverre's avenue"), (430, 206, "Route 15's bridge road"),
                     (500, 206, "Dendemille"), (560, 206, "Route 17's snow"), (630, 206, "Anistar's avenue"), (633, 300, "Route 18"),
                     (634, 357, "Couriway"), (700, 357, "Route 19's west road"), (687, 310, "Route 19's north end"), (687, 270, "Snowbelle's street"),
                     (687, 180, "Route 20's woods"), (687, 130, "the Pokémon Village"), (620, 127, "Route 21"), (606, 94, "the League's terrace"),
                     (608, 60, "before the League's door"), (108, 220, "Geosenge's ring of stones")
                 })
            Assert.True(Reached("Kalos", x, y), what);
        foreach (var (map, x, y) in new[]
                 {
                     ("KalosVictoryRoad", 30, 20), ("KalosLeague_Hall", 10, 8), ("KalosLeague_ChampionRoom", 7, 6), ("KalosLeague_HallOfFame", 6, 8),
                     ("LumioseCity_PrismTowerGym", 6, 5), ("LumioseCity_LysandreLabs", 8, 6), ("GeosengeTown_FlareHQ", 10, 8), ("KalosPowerPlant", 8, 6)
                 })
            Assert.True(Reached(map, x, y), $"{map} {x},{y}");
    }

    [Fact]
    public void KalosStoryRunsFromThePlantBadgeToTheHallOfFameInOrder()
    {
        var story = new StoryState();
        string? Next() => Objectives.Current(story, RegionDatabase.Kalos)?.Name;
        foreach (var (value, name) in new[]
                 {
                     (22, "kalosRamos"), (23, "kalosPowerPlant"), (24, "kalosClemont"), (25, "kalosValerie"), (26, "kalosOlympia"),
                     // (the Hall of Fame leads on to the time after it, plan 23 · Z4b: KalosPostGameTests follows it to the end)
                     (27, "kalosLabs"), (28, "kalosFlareHQ"), (29, "kalosWulfric"), (30, "kalosLeague"), (31, "kalosStation")
                 })
        {
            story.SetVar("VAR_KALOS_STORY", value);
            Assert.Equal(name, Next());
        }
    }

    [Fact]
    public void KalossLastChaptersPlayHeadlessFromThePlantBadgeToTheHallOfFameAndTheFlightOnToAlola()
    {
        // A game at the Tower of Mastery's end (VAR_KALOS_STORY 22, three Badges), every battle won
        var story = new StoryState();
        story.SetVar("VAR_KALOS_STORY", 22);
        foreach (string badge in new[] { "Bug", "Cliff", "Rumble" }) story.GiveBadge(RegionDatabase.Kalos, badge);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Froakie")!, 60));
        var bag = new Inventory();
        // Fresh copies of the maps, since the scripts hide and show their people
        var built = Kalos.BuildMaps().ToDictionary(m => m.Name);
        var rooms = MapDatabase.Fresh();
        var cache = new Dictionary<string, Map>();
        Map Fresh(string name) => cache.TryGetValue(name, out var m) ? m : cache[name] = built.TryGetValue(name, out var b) ? b : rooms(name);
        var library = ScriptLibrary.Default;
        HeadlessScriptHost Play(string file, string script, string map, string? person = null)
        {
            var at = Fresh(map);
            var host = new HeadlessScriptHost(story, party, bag) { Map = at, MapNamed = Fresh, Region = RegionDatabase.Kalos };
            var runner = new ScriptRunner(library, host);
            runner.Start(library.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"),
                person == null ? null : at.Everyone.Single(n => n.Name == person));
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
        int Story() => story.Var("VAR_KALOS_STORY");

        Play("CoumarineCity_Gym", "Ramos", "CoumarineCity_Gym", "Ramos");
        Assert.True(story.HasBadge(RegionDatabase.Kalos, "Plant"));
        Assert.Equal(23, Story());
        Play("KalosPowerPlant", "Aliana", "KalosPowerPlant", "Aliana");
        Assert.Equal(24, Story());
        Assert.Equal(2, story.Var("VAR_KALOS_LUMIOSE_ROADS"));
        Assert.Contains("warp LumioseCity_PrismTowerGym 6 16", Play("lumiose_city", "PrismTower", "Kalos").Log);
        Play("LumioseCity_PrismTowerGym", "Clemont", "LumioseCity_PrismTowerGym", "Clemont");
        Assert.Equal(25, Story());
        Play("kalos_route_14", "Neighbour", "Kalos");
        Assert.Equal(1, story.Var("VAR_KALOS_RIVAL_NORTH"));
        Play("LaverreCity_Gym", "Valerie", "LaverreCity_Gym", "Valerie");
        Assert.Equal(26, Story());
        Play("AnistarCity_Gym", "Olympia", "AnistarCity_Gym", "Olympia");
        Assert.Equal(27, Story());
        Assert.True(story.Has("FLAG_KALOS_HIDE_LYSANDRE_CAFE"));
        Assert.Contains("warp LumioseCity_LysandreLabs 8 14", Play("LumioseCity_LysandreCafe", "Waiter", "LumioseCity_LysandreCafe", "Waiter").Log);
        Play("LumioseCity_LysandreLabs", "Lysandre", "LumioseCity_LysandreLabs", "Lysandre");
        Assert.Equal(28, Story());
        Assert.Contains("warp GeosengeTown_FlareHQ 10 20", Play("geosenge_town", "HiddenDoor", "Kalos").Log);
        Play("GeosengeTown_FlareHQ", "Lysandre", "GeosengeTown_FlareHQ", "Lysandre");
        Play("GeosengeTown_FlareHQ", "Xerneas", "GeosengeTown_FlareHQ", "Xerneas");
        Assert.Equal(29, Story());
        Assert.Equal(1, story.Var("VAR_KALOS_ROUTE_18"));
        Play("SnowbelleCity_Gym", "Wulfric", "SnowbelleCity_Gym", "Wulfric");
        Assert.Equal(30, Story());
        Assert.Equal(8, story.BadgeCount(RegionDatabase.Kalos));
        Play("kalos_route_21", "Gate", "Kalos");
        Assert.Equal(1, story.Var("VAR_KALOS_VICTORY_ROAD"));
        foreach (var (room, who) in new[] { ("KalosLeague_MalvaRoom", "Malva"), ("KalosLeague_SieboldRoom", "Siebold"), ("KalosLeague_WikstromRoom", "Wikstrom"), ("KalosLeague_DrasnaRoom", "Drasna") })
            Play(room, who, room, who);
        // The Champion's door is there once all four are beaten
        Assert.True(story.Has("FLAG_KALOS_LEAGUE_FOUR"));
        Assert.Contains("warp KalosLeague_HallOfFame 6 10", Play("KalosLeague_ChampionRoom", "Diantha", "KalosLeague_ChampionRoom", "Diantha").Log);
        var hall = Play("KalosLeague_HallOfFame", "OnEnter", "KalosLeague_HallOfFame");
        Assert.Contains("warp VanivilleTown_PlayersHouse_2F 4 6", hall.Log);
        Assert.True(story.Has("KalosHallOfFame"));
        Assert.Equal(31, Story());
        Assert.False(story.Has("FLAG_KALOS_DEFEATED_DIANTHA"));
        Assert.False(story.Has("FLAG_KALOS_LEAGUE_FOUR"));
        // The Hall of Fame opens the flight on to Alola from Lumiose City's Pokémon Center, where its attendant now stands
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(RegionDatabase.LinkFrom(RegionDatabase.Kalos)!, story));
        var center = Fresh("LumioseCity_PokemonCenter_1F");
        center.ApplyPresence(story.Has);
        Assert.Contains(center.NPCs, n => n.IsTransportAttendant || n.Script == "Attendant");
    }

    [Fact]
    public void TheGatesOfTheLastChaptersTurnThePlayerBackUntilTheirTime()
    {
        var story = new StoryState();
        var map = Kalos.BuildMaps().Single(m => m.Name == "Kalos");
        HeadlessScriptHost Play(string file, string script)
        {
            var host = new HeadlessScriptHost(story) { Map = map, Region = RegionDatabase.Kalos };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(script, file)!);
            runner.RunToEnd();
            return host;
        }
        // Victory Road's guard: seven Badges are not enough
        foreach (string badge in Region.BadgeNames.Take(7)) story.GiveBadge(RegionDatabase.Kalos, badge);
        Play("kalos_route_21", "Gate");
        Assert.Equal(0, story.Var("VAR_KALOS_VICTORY_ROAD"));
        // The valley road south of Anistar City stays shut until Team Flare's base
        Play("kalos_route_18", "NotYet");
        Assert.Equal(0, story.Var("VAR_KALOS_ROUTE_18"));
        // The Prism Tower is dark until the power is back
        Assert.DoesNotContain(Play("lumiose_city", "PrismTower").Log, l => l.StartsWith("warp", StringComparison.Ordinal));
        // The hidden door under Geosenge's stones is only there once Lysandre has gone down to it
        var geosenge = Overworld.TriggersAt(108, 219).Single(t => t.Script == "HiddenDoor");
        Assert.Equal(28, geosenge.Value);
    }
}
