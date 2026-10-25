using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Unova as Black 2's maps make it (plan 22 · U2): what <c>Data/world/unova</c> opens, the overworld and its rooms built
/// from the third DS reader's files, where a new game there begins, Route 19's wild Pokémon and Route 20's seasons, the
/// trainers, item balls and hidden items, and the catalogue of models. Floors, not counts, wherever a later session
/// opens more (plan 16 · T16).
/// </summary>
public class UnovaTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Unova"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Unova)!.Start!));

    private static World Unova => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Unova"];
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);

    private static readonly string[] FirstAreas = { "aspertia_city", "unova_route_19", "floccesy_town", "unova_route_20", "floccesy_ranch" };

    [Fact]
    public void TheIndexOpensAspertiaCityToFloccesyRanchWithTheirRooms()
    {
        Assert.Equal("Unova", Unova.Index.Region);
        foreach (string key in FirstAreas.Append("floccesy_ranch_2")) Assert.Contains(key, Unova.Index.Areas);
        var unova = RegionDatabase.Get(RegionDatabase.Unova)!;
        foreach (var entry in Unova.Index.Maps)
        {
            Assert.Contains(entry.Name, unova.Maps);
            Assert.True(BuiltMaps.Value.ContainsKey(entry.Name), $"{entry.Name} isn't built");
            if (entry.Setting == MapSetting.Indoors) Assert.True(BuiltMaps.Value[entry.Name].IsIndoors, $"{entry.Name} is no room");
            // a map's name is no other region's
            Assert.Equal(1, RegionDatabase.All.Count(r => r.Maps.Contains(entry.Name)));
        }
        // Every open area has its file and its overlay, and the overlay a song (Sinnoh's and the common ones stand in
        // until Unova has songs of its own)
        foreach (string key in Unova.Index.Areas)
        {
            Assert.NotNull(Unova.Area(key));
            Assert.False(string.IsNullOrEmpty(Unova.Overlay(key)?.BgmTrack), $"{key} has no song");
        }
        // Black 2's keys meet no other region's: the routes take the region's name
        var others = World.LoadAll().Where(w => w.Index.Region != "Unova").SelectMany(w => w.Index.Areas).ToHashSet();
        Assert.DoesNotContain(Unova.Index.Areas, others.Contains);
        Assert.All(Unova.Index.Areas.Where(k => k.Contains("route")), k => Assert.StartsWith("unova_", k));
    }

    [Fact]
    public void ANewGameInUnovaBeginsInThePlayersRoomInAspertiaCity()
    {
        var unova = RegionDatabase.Get(RegionDatabase.Unova)!;
        var start = unova.Start!;
        Assert.Equal("AspertiaCity_PlayersHouse", start.Map);
        var home = BuiltMaps.Value[start.Map];
        Assert.True(home.IsIndoors);
        Assert.True(home.IsWalkable(start.X, start.Y));
        // Mom is at the table (the room's own person 0, plan 22 · U5), and sees to the team as a nurse would
        Assert.Contains(home.NPCs, n => n.Name == "Mom" && n.Key == "0" && n.IsHealingNurse);

        // A Champion who comes over steps out before the house
        var arrival = unova.ArrivalSpot!;
        Assert.Equal("Unova", arrival.Map);
        Assert.True(Overworld.IsWalkable(arrival.X, arrival.Y));
        Assert.Equal("aspertia_city", Overworld.AreaAt(arrival.X, arrival.Y)?.Key);
        Assert.Equal("Aspertia City", Overworld.DisplayNameAt(arrival.X, arrival.Y));
        Assert.Equal("AspertiaCity_PlayersHouse", Overworld.GetWarpAt(arrival.X, arrival.Y - 1)?.TargetMap);
        // and the record's spawns stand on open ground before their rooms' doors
        foreach (var spawn in unova.File!.Spawns)
        {
            Assert.True(Overworld.IsWalkable(spawn.X, spawn.Y), $"spawn {spawn.Id} stands on a blocked tile");
            Assert.Equal(spawn.Room, Overworld.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
        }
        // The region's story is written (plan 22 · U5), its starters are Black 2's, and it alone changes with the seasons
        Assert.True(unova.HasStory);
        Assert.True(unova.HasSeasons);
    }

    [Fact]
    public void TheWalkFromTheStartReachesEveryOpenAreaAndEveryRoom()
    {
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Unova").Value;
        foreach (string key in FirstAreas)
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        foreach (var entry in Unova.Index.Maps)
            Assert.True(walked.Keys.Any(m => m.Name == entry.Name), $"nobody walks into {entry.Name}");
        // The Gym is behind the Trainers' School, and Route 19's tall grass is walked through
        Assert.Contains(walked.Keys, m => m.Name == "AspertiaCity_Gym");
        Assert.True(outside.Count(t => Overworld.AreaAt(t.X, t.Y)?.Key == "unova_route_19"
            && Overworld.BehaviourAt(t.X, t.Y) == TileBehavior.TallGrass) >= 50, "Route 19's grass isn't walked");
    }

    [Fact]
    public void RouteNineteenMeetsPatratAndPurrloinAtTheStartOfTheJourney()
    {
        var route19 = Overworld.FindArea("unova_route_19")!;
        Assert.Equal(12, route19.WildEncounters.Count);
        Assert.Equal(new[] { "Patrat", "Purrloin" }, route19.WildEncounters.Select(e => e.SpeciesName).Distinct().OrderBy(s => s).ToArray());
        Assert.All(route19.WildEncounters, e => Assert.InRange(e.MinLevel, 2, 4));
        // Its shaking grass is read and kept for plan 22 · U4; it has no dark grass and is the same all year
        var file = Unova.Area("unova_route_19")!;
        Assert.Equal(13, file.LandRate);
        Assert.Equal(12, file.ShakingGrass!.Count);
        Assert.Null(file.DarkGrass);
        Assert.Null(file.Seasons);
        // The ranch's field meets Lillipup and the ranch's Mareep
        var ranch = MapNamed("FloccesyRanch").FindArea("floccesy_ranch_2")!;
        Assert.Contains(ranch.WildEncounters, e => e.SpeciesName == "Lillipup");
        Assert.Contains(ranch.WildEncounters, e => e.SpeciesName == "Mareep");
    }

    [Fact]
    public void RouteTwentysPokemonChangeWithTheSeasons()
    {
        // Spring's tables are the area's own, and each season after it that differs is kept whole
        var file = Unova.Area("unova_route_20")!;
        Assert.Equal(13, file.LandRate);
        Assert.Equal("Pidove", file.Land![0].Species);
        Assert.NotNull(file.DarkGrass);
        Assert.Equal(new[] { "Summer", "Autumn", "Winter" }, file.Seasons!.Select(s => s.Season).ToArray());
        var summer = file.Seasons![0];
        Assert.Equal(19, summer.LandRate);
        Assert.Equal("Sunkern", summer.Land![0].Species);
        // and no area of any other region has seasons
        foreach (var world in World.LoadAll().Where(w => w.Index.Region != "Unova"))
            foreach (string key in world.Index.Areas)
                Assert.Null(world.Area(key)?.Seasons);
    }

    [Fact]
    public void UnovasTrainersBattleWithBlack2sTeamsUnderTheRegionsName()
    {
        var molly = TrainerDatabase.Get("TRAINER_LASS_MOLLY", "unova")!;
        Assert.Equal("unova_lass_molly", molly.Id);
        Assert.Equal(new[] { ("Purrloin", 6), ("Sewaddle", 6) }, molly.Party.Select(p => (p.Species, p.Level)).ToArray());
        Assert.True(TrainerDatabase.Get("unova_twins_lia_lily", "unova")!.DoubleBattle);
        // They stand on Route 20 and the ranch's field with those teams
        Assert.Contains(Overworld.Everyone, n => n.IsTrainer && n.TrainerData?.Id == "unova_youngster_petey");
        Assert.Equal(2, Overworld.Everyone.Count(n => n.TrainerData?.Id == "unova_twins_lia_lily"));
        Assert.Contains(MapNamed("FloccesyRanch").Everyone, n => n.TrainerData?.Id == "unova_lass_molly");
        // Every trainer the overworld places is Unova's own, and none of Platinum's table
        Assert.All(Overworld.Everyone.Where(n => n.IsTrainer), n => Assert.StartsWith("unova_", n.TrainerData!.Id));
    }

    [Fact]
    public void ItemBallsAndHiddenItemsHoldWhatBlack2sScriptsSet()
    {
        var waterStone = Overworld.Everyone.Single(n => n.IsItemBall && n.GridX == 61 && n.GridY == 676);
        Assert.Equal("Water Stone", waterStone.Item);
        Assert.StartsWith("FLAG_UNOVA_", waterStone.HiddenBy);
        Assert.Contains(Overworld.Everyone, n => n.IsItemBall && n.Item == "TM53");
        var belt = Overworld.HiddenItems[(93, 691)];
        Assert.Equal(("Expert Belt", "FLAG_UNOVA_0x0bc4"), (belt.Item, belt.Flag));
        Assert.Equal("Potion", Overworld.HiddenItems[(127, 655)].Item);
    }

    [Fact]
    public void TheRoomsAreFurnished()
    {
        var center = BuiltMaps.Value["AspertiaCity_PokemonCenter"];
        Assert.Equal(InteriorStyle.PokemonCenter, center.Interior);
        Assert.Contains(center.NPCs, n => n.Name == "Nurse" && n.IsHealingNurse);
        Assert.Contains(center.NPCs, n => n.Name == "Clerk");
        Assert.Contains(BuiltMaps.Value["AspertiaCity_PlayersHouse"].Props, p => p.Type == PropType.Bookshelf);
        Assert.True(BuiltMaps.Value["AspertiaCity_Gym"].Props.Count > 0);
    }

    [Fact]
    public void Route20sAutumnChunksAreWrittenBesideItsOwn()
    {
        // Plan 22 · U2, part b: Black 2's autumn04_20 to autumn05_21 lie over Route 20 in autumn; the matrix names them
        // and their files are there, while spring's (the matrix's own) stay where they were
        var over = Unova.Matrix(0);
        Assert.NotNull(over.Seasons);
        Assert.Equal(new[] { "Summer", "Autumn", "Winter" }, over.Seasons!.Select(s => s.Season));
        int changed = 0;
        for (int y = 0; y < over.Height; y++)
            for (int x = 0; x < over.Width; x++)
            {
                int spring = over.ChunkAt(x, y);
                Assert.Equal(spring, over.ChunkAt(x, y, "Spring"));
                Assert.Equal(spring, over.ChunkAt(x, y, null));
                foreach (var season in over.Seasons)
                {
                    int own = over.ChunkAt(x, y, season.Season);
                    if (own == spring) continue;
                    changed++;
                    Assert.NotEqual(WorldMatrixFile.NoChunk, spring);
                    // a season's chunk in view of an open area is written, as its spring's is
                    if (Unova.Chunk(spring) != null) Assert.NotNull(Unova.Chunk(own));
                }
            }
        Assert.True(changed >= 4, $"only {changed} cells change with the season");
        string route20 = FirstAreas[3];
        bool autumnOnRoute20 = false;
        for (int y = 0; y < over.Height; y++)
            for (int x = 0; x < over.Width; x++)
                if (over.AreaAt(x, y) == route20 && over.ChunkAt(x, y, "Autumn") != over.ChunkAt(x, y)) autumnOnRoute20 = true;
        Assert.True(autumnOnRoute20, "Route 20 has no autumn chunk");
        // No other region's matrix has seasons
        foreach (var world in World.LoadAll().Where(w => w.Index.Region != "Unova"))
            foreach (int id in world.Index.Maps.Select(m => m.Matrix).Distinct())
                Assert.Null(world.Matrix(id).Seasons);
    }

    [Fact]
    public void CasteliasStreetsMeetTheAvenueAndThePlazaOfOurs()
    {
        // The avenue along the bay stands on Black 2's rails, which the reader leaves out: a map of ours stands there
        // (plan 22 · U5, part c), each street's mouth leading into its street and the street's south end back onto it
        var avenue = MapNamed("CasteliaCity");
        Assert.Equal("CasteliaCity", avenue.Name);
        Assert.Contains("CasteliaCity", RegionDatabase.Get(RegionDatabase.Unova)!.Maps);
        Assert.Equal(Architecture.Metropolis, avenue.Architecture);
        foreach (var (street, x) in new[] { ("CasteliaCity_CasteliaStreet", 16), ("CasteliaCity_ModeStreet", 38), ("CasteliaCity_NarrowStreet", 60), ("CasteliaCity_GymStreet", 82) })
        {
            var way = avenue.GetWarpAt(x, 0);
            Assert.Equal(street, way?.TargetMap);
            var into = MapNamed(street);
            Assert.True(into.IsWalkable(way!.TargetX, way.TargetY), $"{street}'s way in from the avenue is blocked");
            Assert.Contains(into.Warps, w => w.TargetMap == "CasteliaCity" && avenue.IsWalkable(w.TargetX, w.TargetY));
            // and its north end opens onto the plaza, whose rim leads back
            Assert.Contains(into.Warps, w => w.TargetMap == "CasteliaCity_Plaza" && MapNamed("CasteliaCity_Plaza").IsWalkable(w.TargetX, w.TargetY));
            Assert.Contains(MapNamed("CasteliaCity_Plaza").Warps, w => w.TargetMap == street);
        }
        // The piers' mouths and the Pokémon Center's door
        Assert.Equal("CasteliaCity_PrimePier", avenue.GetWarpAt(38, 13)?.TargetMap);
        Assert.Equal("CasteliaCity_ThumbPier", avenue.GetWarpAt(4, 13)?.TargetMap);
        Assert.Equal("CasteliaCity_PokemonCenter", avenue.GetWarpAt(28, 5)?.TargetMap);
        Assert.Contains(MapNamed("CasteliaCity_PokemonCenter").Warps, w => w.TargetMap == "CasteliaCity");
        // Castelia's towers are props of ours on the streets' chunks, built as towers of the town's own way of building
        var gymStreet = MapNamed("CasteliaCity_GymStreet");
        Assert.Contains(gymStreet.PlacedBuildings, b => b.Kind == BuildingKind.Tower && b.Town == Architecture.Metropolis);
        Assert.Contains(gymStreet.PlacedBuildings, b => b.Kind == BuildingKind.Gym);
        Assert.Equal(PokemonType.Bug, WorldModels.GymTheme("castelia_city_3"));
        Assert.Contains(gymStreet.Warps, w => w.TargetMap == "CasteliaCity_Gym");
        // The ship from Virbank comes in at Prime Pier, and beyond the gate Route 4 is open with its row of Crustle
        Assert.True(MapNamed("CasteliaCity_PrimePier").IsWalkable(12, 10));
        Assert.Contains("unova_route_4", Unova.Index.Areas);
        Assert.Equal(11, Overworld.Everyone.Count(n => n.Species == "Crustle" && Overworld.AreaAt(n.GridX, n.GridY)?.Key == "unova_route_4"));
        Assert.Contains(MapNamed("CasteliaGate").Warps, w => w.TargetMap == "Unova");
    }

    [Fact]
    public void RouteFourLeadsThroughJoinAvenueToNimbasaAndToTheDesertInItsStorm()
    {
        // Plan 22 · U6: Route 4's north end leads into Join Avenue, whose north end comes out in Nimbasa City
        var avenue = MapNamed("JoinAvenue");
        Assert.True(avenue.IsIndoors);
        Assert.Contains(Overworld.Warps, w => w.TargetMap == "JoinAvenue" && Overworld.AreaAt(w.SourceX, w.SourceY)?.Key == "unova_route_4");
        Assert.Contains(avenue.Warps, w => w.TargetMap == "Unova" && Overworld.AreaAt(w.TargetX, w.TargetY)?.Key == "nimbasa_city");
        Assert.Contains(avenue.Warps, w => w.TargetMap == "Unova" && Overworld.AreaAt(w.TargetX, w.TargetY)?.Key == "unova_route_4");
        // Route 4's trainers stand on the road, the Hooligans two people of one trainer
        Assert.Equal(2, Overworld.Everyone.Count(n => n.TrainerData?.Id == "unova_hooligans_rob_sal"));
        Assert.Contains(Overworld.Everyone, n => n.TrainerData?.Id == "unova_policeman_dell");
        // The Desert Resort's edge and the desert beyond its gate house are in their sandstorm
        Assert.Equal(FieldWeather.Sandstorm, Overworld.WeatherAt(374, 495));
        Assert.Equal(FieldWeather.Sandstorm, MapNamed("DesertResort").WeatherAt(50, 50));
        Assert.Equal(FieldWeather.Clear, Overworld.WeatherAt(430, 560));
        Assert.Contains(MapNamed("DesertResortGate").Warps, w => w.TargetMap == "DesertResort");
        Assert.Contains(MapNamed("DesertResort").Everyone, n => n.TrainerData?.Id == "unova_backpacker_walt");
        // the Relic Castle's door stays shut until plan 22 · U6, part b
        Assert.DoesNotContain(MapNamed("DesertResort").Warps, w => w.TargetMap.StartsWith("Relic", StringComparison.Ordinal));
        // Nimbasa City builds its own way, and Elesa's Gym in the amusement park is Electric's
        Assert.Equal(Architecture.Funfair, Overworld.ArchitectureAt(420, 450));
        var park = MapNamed("NimbasaCity_AmusementPark");
        Assert.Contains(park.PlacedBuildings, b => b.Kind == BuildingKind.Gym);
        Assert.Equal(PokemonType.Electric, WorldModels.GymTheme("nimbasa_city_2"));
        Assert.Contains(park.Warps, w => w.TargetMap == "NimbasaCity_Gym");
        Assert.Contains(Overworld.Warps, w => w.TargetMap == "NimbasaCity_AmusementPark");
        Assert.Contains(Overworld.Warps, w => w.TargetMap == "NimbasaCity_PokemonCenter");
        // Elesa waits on her stage, the three models on the runway below her
        var gym = MapNamed("NimbasaCity_Gym");
        Assert.Contains(gym.NPCs, n => n.Name == "Elesa");
        Assert.Equal(3, gym.NPCs.Count(n => n.NpcType == "beauty"));
        // and since part b the gate west to Route 5 is open, while the one east to Route 16 still leads nowhere
        Assert.Contains(Overworld.Warps, w => w.SourceX == 384 && w.SourceY is >= 437 and <= 439 && w.TargetMap == "NimbasaGate_Route5");
        Assert.DoesNotContain(Overworld.Warps, w => w.SourceX == 447 && w.SourceY is >= 437 and <= 439);
    }

    [Fact]
    public void NimbasaIsPavedAndItsBattlesAreFoughtInTheStreet()
    {
        // Plan 22, wave 48: Nimbasa's floor (out7yuk01) was read as snow, which made the city snow country, its light
        // blue and the Subway Bosses' battle a snowfield; its paving is a street now, and a battle on it the City stage
        int snow = 0, paving = 0;
        (int X, int Y)? street = null;
        for (int y = 400; y < 512; y++)
            for (int x = 368; x < 464; x++)
            {
                if (Overworld.AreaAt(x, y)?.Key != "nimbasa_city") continue;
                var tile = Overworld.GetGroundTile(x, y);
                if (tile == TileType.Snow) snow++;
                if (tile == TileType.Paving && !Overworld.IsSolid(x, y)) { paving++; street ??= (x, y); }
            }
        Assert.Equal(0, snow);
        Assert.True(paving > 200, $"{paving} tiles of paving");
        var area = Overworld.AreaAt(street!.Value.X, street.Value.Y)!;
        Assert.False(area.Snowbound);
        Assert.Equal("City", area.BattleBackground);
        Assert.Equal(BattleArena.City, Overworld.ArenaAt(street.Value.X, street.Value.Y));
        Assert.Equal(PokemonPlatinumEngine.Battle.Sim.BattleTerrain.Building, GameEngine.TerrainAt(Overworld, street.Value.X, street.Value.Y));
        Assert.True(PokemonPlatinumEngine.Graphics.ArenaSpec.For(Overworld, street.Value.X, street.Value.Y).Outdoors);
    }

    [Fact]
    public void JoinAvenueIsACoveredStreetAndElesasRunwayStandsOverTheDark()
    {
        // Plan 22, wave 48: Join Avenue's floor is the street's paving between shopfronts, not a house's boards
        var avenue = MapNamed("JoinAvenue");
        Assert.Equal(InteriorStyle.Street, avenue.Interior);
        int paved = 0, fronts = 0;
        for (int y = 0; y < avenue.Height; y++)
            for (int x = 0; x < avenue.Width; x++)
            {
                if (avenue.GetGroundTile(x, y) == TileType.Paving && !avenue.IsSolid(x, y)) paved++;
                if (avenue.GetGroundTile(x, y) == TileType.Wall && x > 0 && x < avenue.Width - 1 && avenue.InBounds(x - 1, y) && avenue.GetGroundTile(x - 1, y) == TileType.Paving) fronts++;
            }
        Assert.True(paved > 300, $"{paved} tiles of the avenue's paving");
        Assert.True(fronts > 10, $"{fronts} shopfronts on the street");
        // The Nimbasa Gym's runway is an Electric hall over nothing: no wall but the void round its paving
        var gym = MapNamed("NimbasaCity_Gym");
        Assert.Equal(InteriorStyle.Gym, gym.Interior);
        Assert.Equal(PokemonType.Electric, gym.ArenaType);
        int voids = 0, walls = 0;
        for (int y = 0; y < gym.Height; y++)
            for (int x = 0; x < gym.Width; x++)
            {
                if (gym.GetGroundTile(x, y) == TileType.Void) voids++;
                if (gym.GetGroundTile(x, y) == TileType.Wall) walls++;
            }
        Assert.True(voids > 1000, $"{voids} tiles of the dark");
        Assert.Equal(0, walls);
        Assert.Contains(Walked.Value.Keys, m => m.Name == "NimbasaCity_Gym");
    }

    [Fact]
    public void NimbasasRoomsAreOpenAndItsPokemonCenterIsASpawn()
    {
        // Plan 22 · U6, part b: the stadiums' lobbies and halls, two houses, the Battle Institute, the Musical Theater and
        // the old roller coaster, each reached from the city or the park
        foreach (string room in new[] { "NimbasaCity_BigStadium", "NimbasaCity_SmallCourt", "NimbasaCity_House1_1F", "NimbasaCity_House2_1F",
                     "NimbasaCity_BattleInstitute", "MusicalTheater" })
            Assert.Contains(Overworld.Warps, w => w.TargetMap == room);
        Assert.Contains(MapNamed("NimbasaCity_BigStadium").Warps, w => w.TargetMap == "NimbasaCity_BigStadiumStands");
        Assert.Contains(MapNamed("NimbasaCity_SmallCourt").Warps, w => w.TargetMap == "NimbasaCity_SmallCourtStands");
        Assert.Contains(MapNamed("NimbasaCity_AmusementPark").Warps, w => w.TargetMap == "NimbasaCity_RollerCoaster");
        // the Ferris wheel's attendant stands before its queue lane
        Assert.Contains(MapNamed("NimbasaCity_AmusementPark").NPCs, n => n.Key == "wheel" && n.Script == "FerrisWheel");
        Assert.True(MapNamed("MusicalTheater").NPCs.Count >= 10);
        // Nimbasa's and Driftveil's Pokémon Centers are spawns, each on the tile before its door
        var spawns = RegionDatabase.Get(RegionDatabase.Unova)!.File!.Spawns;
        Assert.Contains(spawns, s => s.Room == "NimbasaCity_PokemonCenter");
        Assert.Contains(spawns, s => s.Room == "DriftveilCity_PokemonCenter");
        Assert.Equal(spawns.Count, spawns.Select(s => s.Id).Distinct().Count());
    }

    [Fact]
    public void RouteFiveCrossesTheDrawbridgeOverTheSeaToDriftveil()
    {
        // Route 5 leads from Nimbasa's west gate to the drawbridge, a map of its own seen with its own camera
        Assert.Contains(MapNamed("NimbasaGate_Route5").Warps, w => w.TargetMap == "Unova");
        Assert.Contains(Overworld.Warps, w => w.TargetMap == "DriftveilDrawbridge");
        var bridge = MapNamed("DriftveilDrawbridge");
        Assert.Equal(FieldCamera.Drawbridge, bridge.Camera);
        Assert.Equal(MapScene.DrawbridgePitchDeg, MapScene.ViewOf(bridge).PitchDeg);
        Assert.True(MapScene.ViewOf(bridge).PitchDeg < MapScene.ViewOf(Overworld).PitchDeg);

        // Its middle is a deck of planks over the sea, five tiles down, walked on the deck
        var deck = bridge.DeckAt(120, 46);
        Assert.NotNull(deck);
        Assert.False(bridge.IsSolid(120, 46));
        Assert.Equal(TileType.Planks, bridge.GetGroundTile(120, 46));
        Assert.Equal(deck!.Value - 5f, bridge.HeightAt(120, 46), 3);
        Assert.True(bridge.IsDeepWater(120, 46));
        Assert.Equal((deck.Value, true), bridge.SurfaceAt(120, 46, deck.Value));
        // round it is the sea, which nobody walks or surfs onto from the deck
        Assert.Equal(TileType.Water, bridge.GetGroundTile(120, 30));
        Assert.True(bridge.IsSolid(120, 30));
        Assert.Equal(deck.Value - 5f, bridge.HeightAt(120, 30), 3);
        // and its ends, where the gates' ways come in, stand on ground at the deck's height
        Assert.Null(bridge.DeckAt(44, 46));
        Assert.Equal(deck.Value, bridge.HeightAt(44, 46), 3);
        // nothing of the original's models stands in the sea
        Assert.Empty(bridge.PlacedBuildings);
        // the walkers on the bridge stand on its deck
        Assert.All(bridge.NPCs.Where(n => n.GridX is > 47 and < 208), n => Assert.Equal(deck.Value, n.Level));

        // The whole bridge is walked from Route 5 to Driftveil City, over the deck
        var walked = Walked.Value;
        var across = walked.Single(kv => kv.Key.Name == "DriftveilDrawbridge").Value;
        Assert.Contains((120, 46), across);
        Assert.Contains((50, 46), across);
        Assert.Contains((190, 47), across);
        var outside = walked.Single(kv => kv.Key.Name == "Unova").Value;
        foreach (string key in new[] { "unova_route_5", "driftveil_city" })
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
    }

    [Fact]
    public void DriftveilBuildsItsOwnWayAndClaysGymIsAMineOfOurs()
    {
        // Driftveil's warehouses, market and church are of its own way of building, and Clay's Gym is Ground's
        Assert.Equal(Architecture.Storehouse, WorldModels.Of("c5_rbuild", "unova")!.Town);
        Assert.Equal(BuildingKind.Chapel, WorldModels.Of("c5_church", "unova")!.Kind);
        Assert.Equal(BuildingKind.Gym, WorldModels.Of("c5_build", "unova")!.Kind);
        Assert.Equal(PokemonType.Ground, WorldModels.GymTheme("driftveil_city"));
        Assert.Equal(Architecture.Storehouse, Overworld.ArchitectureAt(190, 420));
        foreach (string room in new[] { "DriftveilCity_PokemonCenter", "DriftveilCity_House1", "DriftveilCity_PlasmaHouse",
                     "DriftveilCity_Market", "DriftveilCity_GymLobby", "DriftveilCity_TournamentGate" })
            Assert.Contains(Overworld.Warps, w => w.TargetMap == room);
        // the tournament beyond the gate south of the city is not open yet
        Assert.DoesNotContain(MapNamed("DriftveilCity_TournamentGate").Warps, w => w.TargetMap == "Unova" && w.SourceY > 10);

        // The lobby's lift goes down to the Gym's floor, a map of ours, and its door comes back up
        var lobby = MapNamed("DriftveilCity_GymLobby");
        Assert.Contains(lobby.Warps, w => w.TargetMap == "DriftveilCity_Gym");
        var gym = MapDatabase.Get("DriftveilCity_Gym");
        Assert.Equal("DriftveilCity_Gym", gym.Name);
        Assert.Equal(PokemonType.Ground, gym.ArenaType);
        Assert.Contains(gym.Warps, w => w.TargetMap == "DriftveilCity_GymLobby");
        Assert.Contains(gym.NPCs, n => n.Key == "clay");
        Assert.Equal(7, gym.NPCs.Count(n => n.IsTrainer && n.TrainerData!.TrainerClass == "Worker"));
        Assert.Contains(Walked.Value.Keys, m => m.Name == "DriftveilCity_Gym");
    }

    [Fact]
    public void EveryModelOnUnovasChunksIsInItsCatalogue()
    {
        var unknown = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.GetFiles(GameDataFiles.PathOf(Path.Combine(World.Folder, "unova", "chunks")), "*.json"))
        {
            var chunk = GameDataFiles.Load<WorldChunkFile>(Path.Combine(World.Folder, "unova", "chunks", Path.GetFileName(file)));
            foreach (var prop in chunk.Props)
                if (WorldModels.Of(prop.Name, "unova") == null) unknown.Add(prop.Name);
        }
        Assert.True(unknown.Count == 0, "the catalogue doesn't know " + string.Join(", ", unknown));
        Assert.Same(WorldModels.Unova, WorldModels.Catalogue("unova"));
        // Aspertia's and Floccesy's buildings are built in ways of their own since plan 22 · U5 (the Pokémon Center and
        // the gate as every region's are), and Virbank's since its chapter (plan 22 · U5, part b); a town still to come
        // is a stand-in until its own
        foreach (var m in WorldModels.Unova.Where(m => m.Role == ModelRole.Building))
        {
            if (m.Name.StartsWith("c12_", StringComparison.Ordinal) || m.Name.StartsWith("t06_", StringComparison.Ordinal)
                || m.Name.StartsWith("c13_", StringComparison.Ordinal) || m.Name.StartsWith("c03_", StringComparison.Ordinal)
                || m.Name.StartsWith("c3_", StringComparison.Ordinal) || m.Name is "pc" or "gate" or "d22_farm" or "nschool"
                // Route 4's flats, Join Avenue's gate and Nimbasa's since plan 22 · U6 (its Ferris wheel a hall until part b)
                || m.Name.StartsWith("c04", StringComparison.Ordinal) && m.Name != "c04_fwheel"
                || m.Name is "r4_build" or "buill" or "jagate" or "c4_gate" or "thouse"
                // and Driftveil's since part b, the gates at the drawbridge's foot and toward the tournament with them
                || m.Name.StartsWith("c5_", StringComparison.Ordinal) || m.Name is "rgate" or "wbtgate")
                Assert.True(string.IsNullOrEmpty(m.StandInUntil), $"{m.Name} is still a stand-in");
            else Assert.False(string.IsNullOrEmpty(m.StandInUntil), $"{m.Name} is no stand-in");
        }
        Assert.Equal(Architecture.Hillside, WorldModels.Of("c12_house", "unova")!.Town);
        Assert.Equal(Architecture.Bellfield, WorldModels.Of("t06_house", "unova")!.Town);
        Assert.Equal(Architecture.Dockyard, WorldModels.Of("c13_house", "unova")!.Town);
        Assert.Equal(BuildingKind.Gym, WorldModels.Of("c13_gym", "unova")!.Kind);
        Assert.Equal(PokemonType.Poison, WorldModels.GymTheme("virbank_city"));
        Assert.Equal((ModelRole.Scenery, PropType.Boat), (WorldModels.Of("c13_ship", "unova")!.Role, WorldModels.Of("c13_ship", "unova")!.Prop));
        Assert.Equal(Architecture.Hillside, Overworld.ArchitectureAt(47, 762));
        Assert.Equal(Architecture.Bellfield, Overworld.ArchitectureAt(110, 690));
        // Unova's names are its own: Sinnoh never looks in its table
        Assert.Null(WorldModels.Of("c12_house"));
    }
}
