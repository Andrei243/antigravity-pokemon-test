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
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Hoenn)!.Start!));

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
        // The story is plan 21 · H2's: until then a new game begins with the first of the three
        Assert.False(hoenn.HasStory);
    }

    [Fact]
    public void ANewGameInHoennBeginsBeforeTheFamilysHouseInLittlerootTown()
    {
        var start = RegionDatabase.Get(RegionDatabase.Hoenn)!.Start!;
        Assert.Equal("Hoenn", start.Map);
        Assert.True(Overworld.IsWalkable(start.X, start.Y));
        Assert.Equal("littleroot_town", Overworld.AreaAt(start.X, start.Y)?.Key);
        Assert.Equal("Littleroot Town", Overworld.DisplayNameAt(start.X, start.Y));
        Assert.Equal("LittlerootTown_BrendansHouse_1F", Overworld.GetWarpAt(start.X, start.Y - 1)?.TargetMap);
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
    public void EveryWayIntoARoomLandsOnOpenGroundAndLeadsBack()
    {
        foreach (var room in BuiltMaps.Value.Values.Where(m => m.IsIndoors))
            foreach (var warp in room.Warps.Where(w => w.TargetMap == "Hoenn"))
            {
                Assert.True(Overworld.IsWalkable(warp.TargetX, warp.TargetY) || Overworld.GetWarpAt(warp.TargetX, warp.TargetY) != null,
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
}
