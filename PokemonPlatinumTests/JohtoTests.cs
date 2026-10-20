using MapImporter.Hg;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Johto as HeartGold's maps make it (plan 20 · J2): what <c>Data/world/johto</c> opens, the overworld and its rooms
/// built from the second DS reader's files, where a new game there begins, its wild Pokémon by the time of day, its
/// trainers and its catalogue of models. Floors, not counts, wherever a later session opens more (plan 16 · T16).
/// </summary>
public class JohtoTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Johto"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Johto)!.Start!));

    private static World Johto => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Johto"];
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);

    private static readonly string[] FirstAreas = { "new_bark", "route_29", "cherrygrove", "route_30", "route_31", "violet" };

    [Fact]
    public void TheIndexOpensNewBarkTownToVioletCityWithTheirRooms()
    {
        Assert.Equal("Johto", Johto.Index.Region);
        Assert.False(Johto.Index.BuildingsAtDoors);
        foreach (string key in FirstAreas) Assert.Contains(key, Johto.Index.Areas);
        Assert.True(Johto.Index.Areas.Count >= 30, $"only {Johto.Index.Areas.Count} areas open in Johto");
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        foreach (var entry in Johto.Index.Maps)
        {
            Assert.Contains(entry.Name, johto.Maps);
            Assert.True(BuiltMaps.Value.ContainsKey(entry.Name), $"{entry.Name} isn't built");
            if (entry.Setting == MapSetting.Indoors) Assert.True(BuiltMaps.Value[entry.Name].IsIndoors, $"{entry.Name} is no room");
            // a map's name is no other region's
            Assert.Equal(1, RegionDatabase.All.Count(r => r.Maps.Contains(entry.Name)));
        }
        // Every open area has its file and its overlay, and the overlay a song of Johto's own
        foreach (string key in Johto.Index.Areas)
        {
            Assert.NotNull(Johto.Area(key));
            Assert.StartsWith("johto/", Johto.Overlay(key)!.BgmTrack);
        }
        // HeartGold's keys meet neither Sinnoh's nor Kanto's
        var others = World.LoadAll().Where(w => w.Index.Region != "Johto").SelectMany(w => w.Index.Areas).ToHashSet();
        Assert.DoesNotContain(Johto.Index.Areas, others.Contains);
    }

    [Fact]
    public void ANewGameInJohtoBeginsBeforeThePlayersHouseInNewBarkTown()
    {
        // Since plan 20 · J4 a new game wakes in the bedroom upstairs (JohtoStoryTests); a Champion of Kanto who sails
        // over steps out before the house
        Assert.Equal("NewBark_PlayersHouse_2F", RegionDatabase.Get(RegionDatabase.Johto)!.Start!.Map);
        var start = RegionDatabase.Get(RegionDatabase.Johto)!.ArrivalSpot!;
        Assert.Equal("Johto", start.Map);
        Assert.True(Overworld.IsWalkable(start.X, start.Y));
        Assert.Equal("new_bark", Overworld.AreaAt(start.X, start.Y)?.Key);
        Assert.Equal("New Bark Town", Overworld.DisplayNameAt(start.X, start.Y));
        Assert.Equal("johto/new_bark", Overworld.BgmTrackAt(start.X, start.Y));
        // The door right behind is the player's house
        Assert.Equal("NewBark_PlayersHouse_1F", Overworld.GetWarpAt(start.X, start.Y - 1)?.TargetMap);
        // and the record's two spawns stand on open ground before their rooms' doors
        foreach (var spawn in RegionDatabase.Get(RegionDatabase.Johto)!.File!.Spawns)
        {
            Assert.True(Overworld.IsWalkable(spawn.X, spawn.Y), $"spawn {spawn.Id} stands on a blocked tile");
            Assert.Equal(spawn.Room, Overworld.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
        }
    }

    [Fact]
    public void TheWalkFromTheStartReachesEveryOpenAreaAndEveryRoom()
    {
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Johto").Value;
        foreach (string key in FirstAreas)
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        foreach (var entry in Johto.Index.Maps)
            Assert.True(walked.Keys.Any(m => m.Name == entry.Name), $"nobody walks into {entry.Name}");
        // Route 31's north end, by the Dark Cave's mouth, is reached, and since plan 20 · J10 the cave beyond it
        Assert.Contains(outside, t => Overworld.AreaAt(t.X, t.Y)?.Key == "route_31" && t.X > 555);
        var darkCave = Johto.Area("route_31")!.Warps[2];
        Assert.Contains(Overworld.WarpsAt(darkCave.X, darkCave.Z), w => w.TargetMap == "DarkCave_Route31Side");
        // Since plan 20 · J4 Violet's Gym and Sprout Tower are open, and Route 32 runs south to Union Cave's mouth, open
        // since J5 with the Ruins of Alph beyond their gate house
        var gymDoor = Johto.Area("violet")!.Warps[2];
        Assert.Equal("violet_gym", gymDoor.To);
        Assert.Contains(Overworld.WarpsAt(gymDoor.X, gymDoor.Z), w => w.TargetMap == "Violet_Gym");
        Assert.Contains(walked.Keys, m => m.Name == "SproutTower_3F");
        Assert.Contains(outside, t => Overworld.AreaAt(t.X, t.Y)?.Key == "route_32" && t.Y > 425);
        var caveMouth = Johto.Area("route_32")!.Warps[2];
        Assert.Contains(Overworld.WarpsAt(caveMouth.X, caveMouth.Z), w => w.TargetMap == "UnionCave_1F");
    }

    private static readonly string[] CentreAreas = { "ruins_of_alph", "route_33", "azalea" };

    [Fact]
    public void TheCentreOpensTheRuinsUnionCaveRouteThirtyThreeAndAzaleaTown()
    {
        // Plan 20 · J5: the Ruins of Alph's grounds, Route 33 and Azalea Town are walked to from the start, through Union
        // Cave's three floors, and every room of theirs is come into
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Johto").Value;
        foreach (string key in CentreAreas)
        {
            Assert.Contains(key, Johto.Index.Areas);
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        }
        foreach (string name in new[] { "UnionCave_1F", "UnionCave_B1F", "UnionCave_B2F", "RuinsOfAlph_ResearchCenter",
                     "RuinsOfAlph_NortheastChamber", "Azalea_KurtsHouse", "Azalea_CharcoalKiln", "Azalea_PokemonCenter_B1F" })
            Assert.True(walked.Keys.Any(m => m.Name == name), $"nobody walks into {name}");

        // The Slowpoke Well and the Gym are open since plan 20 · J7; what waits for its chapter stays shut: the Ruins'
        // hall and the rooms behind the chambers' back walls
        var azalea = Johto.Area("azalea")!;
        Assert.Contains(Overworld.WarpsAt(azalea.Warps[2].X, azalea.Warps[2].Z), w => w.TargetMap == "Azalea_GymEntrance");
        Assert.Contains(Overworld.WarpsAt(azalea.Warps[7].X, azalea.Warps[7].Z), w => w.TargetMap == "SlowpokeWell_Entrance");
        var hall = Johto.Area("ruins_of_alph")!.Warps[8];
        Assert.Empty(Overworld.WarpsAt(hall.X, hall.Z));
        Assert.Null(MapNamed("RuinsOfAlph_NortheastChamber").GetWarpAt(6, 2));
        // ...and Ilex Forest beyond Azalea's gate house is open since J5's part b
        Assert.Equal("IlexForest", MapNamed("Azalea_IlexForestGate").GetWarpAt(1, 7)?.TargetMap);

        // Azalea is built in logs (Woodland), its music its own, and Route 33 meets Hoppip by day and Zubat at night
        Assert.Equal(Architecture.Woodland, Overworld.ArchitectureAt(azalea.Warps[6].X, azalea.Warps[6].Z + 2));
        Assert.Equal(Architecture.Woodland, WorldModels.Of("hw_h01", "johto")!.Town);
        Assert.Equal("johto/azalea", Overworld.BgmTrackAt(azalea.Warps[3].X, azalea.Warps[3].Z + 1));
        Assert.Equal("johto/union_cave", MapNamed("UnionCave_B1F").BgmTrackAt(10, 10));
        var route33 = Overworld.FindArea("route_33")!;
        Assert.Contains(EncounterSlots.Grass(route33, new EncounterMoment { Time = TimeOfDay.Day }), s => s.SpeciesName == "Hoppip");
        Assert.Contains(EncounterSlots.Grass(route33, new EncounterMoment { Time = TimeOfDay.Night }), s => s.SpeciesName == "Zubat");
        // The chambers' trainers and Union Cave's battle by HeartGold's records
        Assert.Contains(MapNamed("UnionCave_1F").Everyone, n => n.Id == "johto_hiker_daniel");
        Assert.Contains(Overworld.Everyone, n => n.Id == "johto_psychic_m_nathan");
    }

    private static readonly string[] NorthOfAzalea = { "ilex_forest", "route_34", "goldenrod", "route_35", "national_park", "route_36", "route_37", "ecruteak" };

    [Fact]
    public void IlexForestToEcruteakCityIsWalkedThroughGoldenrodAndTheNationalPark()
    {
        // Plan 20 · J5, part b: from Azalea Town through Ilex Forest (its small tree cut), up Route 34 to Goldenrod City,
        // north by Route 35 and the National Park to Route 36 and up Route 37 to Ecruteak City, every room come into
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Johto").Value;
        foreach (string key in NorthOfAzalea)
        {
            Assert.Contains(key, Johto.Index.Areas);
            if (key is "ilex_forest" or "national_park") Assert.Contains(walked.Keys, m => m.Name == (key == "ilex_forest" ? "IlexForest" : "NationalPark"));
            else Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        }
        foreach (string name in new[] { "Route34_DayCare", "Goldenrod_DeptStore_6F", "Goldenrod_Gym", "Goldenrod_RadioTower_1F",
                     "Goldenrod_Underground", "Goldenrod_GameCorner", "Ecruteak_DanceTheater", "BurnedTower_B1F", "Ecruteak_BellTowerGate" })
            Assert.True(walked.Keys.Any(m => m.Name == name), $"nobody walks into {name}");

        // What waits stays shut: the Magnet Train and the Global Terminal, the Bell Tower beyond its gate (Ecruteak's
        // Gym is open since plan 20 · J7, the road west since J8, the road east to Route 42 since J10 and the Radio
        // Tower's upper floors since J11, part b)
        var goldenrod = Johto.Area("goldenrod")!;
        foreach (int shut in new[] { 9, 16 }) Assert.Empty(Overworld.WarpsAt(goldenrod.Warps[shut].X, goldenrod.Warps[shut].Z));
        var ecruteak = Johto.Area("ecruteak")!;
        Assert.Contains(Overworld.WarpsAt(ecruteak.Warps[10].X, ecruteak.Warps[10].Z), w => w.TargetMap == "Route42_EcruteakGate");
        Assert.Contains(Overworld.WarpsAt(ecruteak.Warps[7].X, ecruteak.Warps[7].Z), w => w.TargetMap == "Ecruteak_Gym");
        Assert.DoesNotContain(walked.Keys, m => m.Name.StartsWith("BellTower", StringComparison.Ordinal));

        // The two cities are built their own ways, with songs of their own
        Assert.Equal(Architecture.Boulevard, WorldModels.Of("ko_h01", "johto")!.Town);
        Assert.Equal(Architecture.Heritage, WorldModels.Of("en_h01a", "johto")!.Town);
        Assert.Equal("johto/goldenrod", Overworld.BgmTrackAt(goldenrod.Warps[5].X, goldenrod.Warps[5].Z + 1));
        Assert.Equal("johto/ecruteak", Overworld.BgmTrackAt(ecruteak.Warps[5].X, ecruteak.Warps[5].Z + 1));
        Assert.Equal("johto/union_cave", MapNamed("IlexForest").BgmTrackAt(15, 70));

        // The Day Care is the common one, the strange tree stands in the way since plan 20 · J7 (JohtoStoryTests), and
        // the trainers battle by HeartGold's records, the twins of Route 37 as one
        Assert.Contains(MapNamed("Route34_DayCare").Everyone, n => n.Script == "DayCareLady");
        Assert.Contains(Overworld.Everyone, n => n.Species == "Sudowoodo" && n.GridX == 415 && n.GridY == 246);
        Assert.Equal(2, Overworld.Everyone.Count(n => n.Id == "johto_twins_tori_and_til"));
        Assert.Contains(MapNamed("Goldenrod_Gym").Everyone, n => n.Id == "johto_beauty_samantha");
        Assert.Contains(MapNamed("BurnedTower_1F").Everyone, n => n.Id == "johto_firebreather_ned");
    }

    [Fact]
    public void TheStrangeTreeAloneKeepsEcruteakCityFromBothSidesOfRouteThirtySix()
    {
        // Plan 20 · J7 (docs/mechanics/rulings.md, "Johto's second chapter"): the tree stands on HeartGold's own tile in
        // the one-tile way between Violet City's side of Route 36 and Route 37's turning, and the ledges of the way from
        // the National Park drop a walker east of it. A walk of the whole game with every field move, in which the tree
        // stays where it is, reaches neither Route 37 nor Ecruteak City; the walk in which its scene has been played
        // reaches both (IlexForestToEcruteakCity...)
        var maps = Johto.BuildMaps().ToDictionary(m => m.Name);
        Map Named(string name) => maps.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);
        var tree = maps["Johto"].Everyone.Single(n => n.Species == "Sudowoodo");
        Assert.Equal((415, 246), (tree.GridX, tree.GridY));
        tree.HiddenBy = null;
        var outside = WorldWalk.From(Named, RegionDatabase.Get(RegionDatabase.Johto)!.Start!).Single(kv => kv.Key.Name == "Johto").Value;
        Assert.Contains(outside, t => Overworld.AreaAt(t.X, t.Y)?.Key == "national_park" || Overworld.AreaAt(t.X, t.Y)?.Key == "route_36");
        Assert.DoesNotContain(outside, t => Overworld.AreaAt(t.X, t.Y)?.Key is "route_37" or "ecruteak");
    }

    [Fact]
    public void TheSecondChaptersPlacesAreWalkedTo()
    {
        // Plan 20 · J7: the Slowpoke Well's three floors, the two Gyms (the Azalea Gym's islands by its carts) and the
        // Department Store's lift are walked into, and every tile Ilex Forest's two Farfetch'd run to can be stood beside
        var walked = Walked.Value;
        foreach (string name in new[] { "SlowpokeWell_Entrance", "SlowpokeWell_B1F", "Azalea_GymEntrance", "Azalea_Gym", "Ecruteak_Gym", "Goldenrod_DeptStore_Elevator" })
            Assert.True(walked.Keys.Any(m => m.Name == name), $"nobody walks into {name}");
        var gym = walked.Single(kv => kv.Key.Name == "Azalea_Gym").Value;
        Assert.Contains((9, 6), gym);
        var ilex = MapNamed("IlexForest");
        var forest = walked.Single(kv => kv.Key.Name == "IlexForest").Value;
        foreach (var (x, y) in new[] { (25, 62), (32, 62), (32, 52), (41, 54), (46, 65), (51, 50) })
            Assert.True(new[] { (x, y + 1), (x, y - 1), (x - 1, y), (x + 1, y) }.Any(forest.Contains), $"nobody can stand beside {x},{y} of Ilex Forest");
        // The Department Store's counters sell their own stock
        foreach (string counter in new[] { "goldenrod_2f_medicine", "goldenrod_2f_balls", "goldenrod_3f", "goldenrod_4f", "goldenrod_5f" })
            Assert.True(MartDatabase.Specialties[counter].All(i => ItemDatabase.Get(i) != null), counter);
    }

    [Fact]
    public void RouteTwentyNineMeetsPidgeyByDayAndHoothootAtNight()
    {
        // HeartGold gives each slot of the grass a species for each time of day (plan 20 · J2)
        var area = Overworld.FindArea("route_29")!;
        Assert.Equal(12, area.WildEncounters.Count);
        var morning = EncounterSlots.Grass(area, new EncounterMoment { Time = TimeOfDay.Morning });
        var day = EncounterSlots.Grass(area, new EncounterMoment { Time = TimeOfDay.Day });
        var night = EncounterSlots.Grass(area, new EncounterMoment { Time = TimeOfDay.Night });
        Assert.Contains(day, s => s.SpeciesName == "Pidgey");
        Assert.DoesNotContain(day, s => s.SpeciesName == "Hoothoot");
        Assert.Contains(night, s => s.SpeciesName == "Hoothoot");
        Assert.DoesNotContain(night, s => s.SpeciesName == "Pidgey");
        Assert.Contains(morning, s => s.SpeciesName == "Pidgey");
        // Levels and weights stay the slot's own whatever the hour
        Assert.Equal(day.Select(s => (s.MinLevel, s.Weight)), night.Select(s => (s.MinLevel, s.Weight)));
        // With no moment the table is its own: the morning's
        Assert.Equal(morning.Select(s => s.SpeciesName), area.WildEncounters.Select(s => s.SpeciesName));
    }

    [Fact]
    public void SinnohsSlotsAreTheSameAllDay()
    {
        // No slot of Platinum's carries a species of its own for the day or the night: its time of day is the area's two
        foreach (var world in World.LoadAll().Where(w => w.Index.Region == "Sinnoh"))
            foreach (string key in world.Index.Areas)
                Assert.All(world.Area(key)?.Land ?? new(), s => Assert.True(s.Day == null && s.Night == null, key));
    }

    [Fact]
    public void TheRoomsAreCutToWhatTheirWaysInReachAndFurnished()
    {
        var home = BuiltMaps.Value["NewBark_PlayersHouse_1F"];
        Assert.Equal((13, 12), (home.Width, home.Height));
        // The room's floor is joined to both its ways out, and the rest of its chunk, which nobody can reach, is wall
        Assert.True(home.IsWalkable(3, 9));
        Assert.Equal(TileType.Wall, home.GetGroundTile(12, 11));
        Assert.Contains(home.Props, p => p.Type == PropType.Television);
        Assert.Contains(home.Props, p => p.Type == PropType.Fridge);
        Assert.Contains(home.Props, p => p.Type == PropType.Table);
        Assert.Contains(home.NPCs, n => n.Name == "Mom" && n.IsHealingNurse);

        var bedroom = BuiltMaps.Value["NewBark_PlayersHouse_2F"];
        Assert.Contains(bedroom.Props, p => p.Type == PropType.Bed);
        Assert.Contains(bedroom.Props, p => p.Type == PropType.Clock);

        var lab = BuiltMaps.Value["NewBark_ElmsLab"];
        Assert.Equal(InteriorStyle.Lab, lab.Interior);
        Assert.Contains(lab.Props, p => p.Type == PropType.HealingMachine);
        Assert.True(lab.Props.Count(p => p.Type == PropType.Bookshelf) >= 8);

        var center = BuiltMaps.Value["Cherrygrove_PokemonCenter_1F"];
        Assert.Equal(InteriorStyle.PokemonCenter, center.Interior);
        Assert.Contains(center.Props, p => p.Type == PropType.HealingMachine);
        Assert.Contains(center.NPCs, n => n.Name == "Nurse");
        Assert.Equal(InteriorStyle.PokeMart, BuiltMaps.Value["Cherrygrove_Mart"].Interior);
        Assert.Contains(BuiltMaps.Value["Cherrygrove_Mart"].Props, p => p.Type == PropType.StoreShelf);
    }

    [Fact]
    public void EveryModelOnJohtosChunksIsInItsCatalogue()
    {
        var unknown = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.GetFiles(GameDataFiles.PathOf(Path.Combine(World.Folder, "johto", "chunks")), "*.json"))
        {
            var chunk = GameDataFiles.Load<WorldChunkFile>(Path.Combine(World.Folder, "johto", "chunks", Path.GetFileName(file)));
            foreach (var prop in chunk.Props)
                if (WorldModels.Of(prop.Name, "johto") == null) unknown.Add(prop.Name);
        }
        Assert.True(unknown.Count == 0, "the catalogue doesn't know " + string.Join(", ", unknown));
        Assert.Same(WorldModels.Johto, WorldModels.Catalogue("johto"));
        // New Bark Town's houses are built as the town builds them, and its lab as a lab
        Assert.Equal((BuildingKind.Lab, (Architecture?)Architecture.Breeze), (WorldModels.Of("wk_labo", "johto")!.Kind, WorldModels.Of("wk_labo", "johto")!.Town));
        Assert.Equal(Architecture.Bayside, WorldModels.Of("yo_h01", "johto")!.Town);
        Assert.Equal(Architecture.Breeze, Overworld.ArchitectureAt(687, 402));
        // A name both games use is Sinnoh's model, and Sinnoh never looks in Johto's table
        Assert.Same(WorldModels.Of("pc"), WorldModels.Of("pc", "johto"));
        Assert.Null(WorldModels.Of("wk_labo"));
    }

    [Fact]
    public void HeartGoldsOwnBehavioursHaveTheirNames()
    {
        Assert.Equal((byte)TileBehavior.HeadbuttTree, HgBehaviours.Headbutt);
        Assert.Equal((byte)TileBehavior.Whirlpool, HgBehaviours.Whirlpool);
        Assert.Equal((byte)TileBehavior.StopSliding, HgBehaviours.StopSliding);
        Assert.Equal((byte)TileBehavior.Magma, HgBehaviours.Magma);
        Assert.Equal((byte)TileBehavior.LadderNorth, HgBehaviours.LadderNorth);
        Assert.Equal((byte)TileBehavior.LadderSouth, HgBehaviours.LadderSouth);
        Assert.Equal((byte)TileBehavior.LadderDown, HgBehaviours.LadderDown);
        // Each is told of, and waits for the session of plan 20 that opens a place using it
        foreach (var b in new[] { TileBehavior.HeadbuttTree, TileBehavior.Magma, TileBehavior.LadderDown })
            Assert.Equal(BehaviourSupport.Waiting, TileBehaviors.InGame(b).Support);
        // ...but the whirlpool, whose rule plan 20 · J8 wrote
        Assert.Equal(BehaviourSupport.Ruled, TileBehaviors.InGame(TileBehavior.Whirlpool).Support);
    }

    private static readonly string[] WestAndSea = { "route_38", "route_39", "olivine", "route_40", "route_41", "cianwood" };

    [Fact]
    public void TheWestAndTheSeaAreWalkedToFromEcruteakCity()
    {
        // Plan 20 · J8: west out of Ecruteak by Route 38, south past the Moomoo Farm by Route 39 to Olivine City, up the
        // Glitter Lighthouse to its light room, and over the sea of Routes 40 and 41 to Cianwood City; the Whirl Islands'
        // floors through their whirlpools. Every room of theirs is come into
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Johto").Value;
        foreach (string key in WestAndSea)
        {
            Assert.Contains(key, Johto.Index.Areas);
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        }
        foreach (string name in new[] { "Route38_EcruteakGate", "Route39_MoomooFarmStable", "Olivine_Gym", "Olivine_Cafe",
                     "Olivine_PortHall", "OlivineLighthouse_5F", "OlivineLighthouse_LightRoom", "OlivineLighthouse_Ledge",
                     "Route40_FrontierGate", "Cianwood_Gym", "Cianwood_Pharmacy", "Cianwood_PokemonCenter_B1F",
                     "WhirlIslands_1F", "WhirlIslands_B2F" })
            Assert.True(walked.Keys.Any(m => m.Name == name), $"nobody walks into {name}");

        // What waits stays shut: the S.S. Aqua's quay, the Battle Frontier's road and Lugia's cave (the road east of
        // Ecruteak to Route 42 is open since plan 20 · J10); the lighthouse's lift waits for Jasmine to step aside (plan
        // 20 · J9), its door a wall until then. The cliff path to Route 47 is open since plan 20 · J8, part b
        Assert.Null(MapNamed("Olivine_PortHall").GetWarpAt(7, 11));
        Assert.Null(MapNamed("Route40_FrontierGate").GetWarpAt(5, 2));
        Assert.Equal("FLAG_UNK_1D8", MapNamed("OlivineLighthouse_1F").GetWarpAt(8, 4)?.OpenedBy);
        Assert.Equal("FLAG_UNK_1D8", MapNamed("OlivineLighthouse_LightRoom").GetWarpAt(8, 4)?.OpenedBy);
        Assert.DoesNotContain(walked.Keys, m => m.Name.Contains("Lugia", StringComparison.Ordinal));
        var cianwood = Johto.Area("cianwood")!;
        Assert.Contains(Overworld.WarpsAt(cianwood.Warps[6].X, cianwood.Warps[6].Z), w => w.TargetMap == "CliffEdgeGate");

        // The whirlpools of Route 41 are the original's blocks of three tiles by three, each with its ring on the water,
        // and the islands' ways in lie behind them
        var whirlpools = Overworld.Props.Where(p => p.Type == PropType.Whirlpool).ToList();
        Assert.True(whirlpools.Count >= 4, $"only {whirlpools.Count} whirlpools on Route 41");
        foreach (var ring in whirlpools)
            Assert.Contains(Enumerable.Range(ring.X, ring.Width).SelectMany(x => Enumerable.Range(ring.Y, ring.Depth).Select(y => (x, y))),
                t => Overworld.BehaviourAt(t.x, t.y) == TileBehavior.Whirlpool);

        // Olivine and Cianwood are built their own ways, with songs of their own; the lighthouse and the farm have theirs
        Assert.Equal(Architecture.Seaport, WorldModels.Of("as_h01", "johto")!.Town);
        Assert.Equal(Architecture.Cliffside, WorldModels.Of("tn_h01", "johto")!.Town);
        var olivine = Johto.Area("olivine")!;
        Assert.Equal("johto/olivine", Overworld.BgmTrackAt(olivine.Warps[1].X, olivine.Warps[1].Z + 1));
        Assert.Equal("johto/cianwood", Overworld.BgmTrackAt(cianwood.Warps[1].X, cianwood.Warps[1].Z + 1));
        Assert.Equal("johto/route38", MapNamed("Route38_EcruteakGate").BgmTrackAt(5, 6));
        Assert.Equal("johto/lighthouse", MapNamed("OlivineLighthouse_3F").BgmTrackAt(8, 8));

        // The trainers battle by HeartGold's records; the Miltank graze on Route 39 and Amphy lies sick by the lamp
        Assert.Contains(Overworld.Everyone, n => n.Id == "johto_sailor_harry");
        Assert.Contains(Overworld.Everyone, n => n.Id == "johto_swimmer_f_wendy");
        Assert.Contains(MapNamed("OlivineLighthouse_4F").NPCs, n => n.Id == "johto_lass_connie");
        Assert.Contains(MapNamed("Cianwood_Gym").Everyone, n => n.Id == "johto_black_belt_lung");
        Assert.True(Overworld.NPCs.Count(n => n.Species == "Miltank") >= 4);
        Assert.Contains(MapNamed("OlivineLighthouse_LightRoom").NPCs, n => n.Species == "Ampharos");
        // ...and since plan 20 · J9 Jasmine sits by Amphy, her place in the Gym waits for its cure (the Gym's own
        // arrival hides her: JohtoStoryTests), and Chuck trains under his waterfall
        Assert.Contains(MapNamed("OlivineLighthouse_LightRoom").NPCs, n => n.Name == "Jasmine");
        Assert.Contains(MapNamed("Olivine_Gym").Everyone, n => n.Name == "Jasmine");
        Assert.Contains(MapNamed("Cianwood_Gym").NPCs, n => n.Name == "Chuck");
    }

    [Fact]
    public void TheCliffPathLeadsFromCianwoodCityToTheSafariZonesTwelveAreas()
    {
        // Plan 20 · J8, part b: up through the gate in Cianwood's cliff, along Route 47 past Cliff Cave, over Route 48 to
        // the Safari Zone's gate and its hall, and from its counter into the zone, whose twelve areas lie side by side
        // (the rulings' "The Johto Safari Zone")
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Johto").Value;
        foreach (string key in new[] { "route_47", "route_48", "safari_zone_gate" })
        {
            Assert.Contains(key, Johto.Index.Areas);
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        }
        foreach (string name in new[] { "CliffEdgeGate", "CliffCave", "SafariZoneGate_PokemonCenter_1F", "SafariZoneGate_PokemonCenter_B1F",
                     "SafariZone_Gate", "SafariZone" })
            Assert.True(walked.Keys.Any(m => m.Name == name), $"nobody walks into {name}");

        var (zone, inZone) = walked.Single(kv => kv.Key.Name == "SafariZone");
        for (int area = 1; area <= 12; area++)
        {
            string key = $"safari_zone_{area:00}";
            Assert.True(inZone.Any(t => zone.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the way in");
            Assert.Equal("Safari Zone", Johto.Area(key)!.Name);
            Assert.False(Johto.Area(key)!.Fly);
        }
        // The tests' two: Geodude in the Peak Area, Sandshrew in the Desert Area
        Assert.Contains(Johto.Area("safari_zone_04")!.Land!, s => s.Species == "Geodude");
        Assert.Contains(Johto.Area("safari_zone_12")!.Land!, s => s.Species == "Sandshrew");

        // The zone's door leads back behind the hall's counter, whose arrival asks whether the game is over
        var door = Johto.Area("safari_zone_entrance_exterior")!.Warps[0];
        var back = zone.GetWarpAt(door.X, door.Z);
        Assert.Equal(("SafariZone_Gate", 5, 3), (back?.TargetMap, back?.TargetX, back?.TargetY));
        Assert.Equal("johto/national_park", zone.BgmTrackAt(door.X, door.Z));
        Assert.Equal("johto/union_cave", MapNamed("CliffCave").BgmTrackAt(Johto.Area("cliff_cave")!.Warps[0].X, Johto.Area("cliff_cave")!.Warps[0].Z));

        // The Embedded Tower behind Route 47 waits
        var route47 = Johto.Area("route_47")!;
        foreach (int warp in new[] { 4, 5, 6 })
            Assert.Empty(Overworld.WarpsAt(route47.Warps[warp].X, route47.Warps[warp].Z));
    }

    private static readonly string[] North = { "route_42", "mahogany", "route_43", "lake_of_rage", "route_44", "blackthorn", "route_45", "route_46" };

    [Fact]
    public void TheNorthIsWalkedToFromEcruteakCityThroughMtMortarAndTheIcePath()
    {
        // Plan 20 · J10, part a: east out of Ecruteak by Route 42 and through Mt. Mortar's floors to Mahogany Town, north
        // by Route 43 and its gates to the Lake of Rage, east along Route 44 into the Ice Path, down its four floors and
        // out into Blackthorn City, behind the Gym into the Dragon's Den and over its water to the shrine, and south by
        // Route 45, the Dark Cave and Route 46 to Route 29's gate. Every room of theirs is come into
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Johto").Value;
        foreach (string key in North)
        {
            Assert.Contains(key, Johto.Index.Areas);
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        }
        foreach (string name in new[] { "Route42_EcruteakGate", "MtMortar_1F", "MtMortar_1F_Back", "MtMortar_2F", "MtMortar_B1F",
                     "Mahogany_PokemonCenter_1F", "Mahogany_PokemonCenter_B1F", "Mahogany_SouvenirShop", "Mahogany_EastHouse",
                     "Mahogany_Gym", "Mahogany_Gym_2", "Mahogany_Gym_3", "Route43_MahoganyGate", "Route43_Gate",
                     "LakeOfRage_HiddenPowerHouse", "LakeOfRage_FishingGuruHouse", "IcePath_1F", "IcePath_B1F", "IcePath_B2F",
                     "IcePath_B3F", "Blackthorn_PokemonCenter_1F", "Blackthorn_PokemonCenter_B1F", "Blackthorn_Mart",
                     "Blackthorn_Gym", "Blackthorn_WestHouse", "Blackthorn_EastHouse", "Blackthorn_MoveTutorHouse",
                     "DragonsDen_Entrance", "DragonsDen", "DragonsDen_Shrine", "DarkCave_Route31Side", "DarkCave_Route45Side" })
            Assert.True(walked.Keys.Any(m => m.Name == name), $"nobody walks into {name}");

        // The Ice Path joins Route 44 to Blackthorn City: its way in from the route and its way out into the city
        var icePath = Johto.Area("route_44")!.Warps[0];
        Assert.Contains(Overworld.WarpsAt(icePath.X, icePath.Z), w => w.TargetMap == "IcePath_1F");
        var outOfIt = Johto.Area("blackthorn")!.Warps[0];
        Assert.Contains(Overworld.WarpsAt(outOfIt.X, outOfIt.Z), w => w.TargetMap == "IcePath_1F");

        // Plan 20 · J11, part a opened the Team Rocket Hideout under the souvenir shop (its stairs behind the salesman,
        // a way of the story's), Pryce in his Gym and the red Gyarados on the lake; what waits for part b stays off the
        // map: Clair, and Lance in the Dragon's Den
        foreach (string floor in new[] { "TeamRocketHQ_B1F", "TeamRocketHQ_B2F", "TeamRocketHQ_B3F" })
            Assert.True(walked.Keys.Any(m => m.Name == floor), $"nobody walks into {floor}");
        Assert.Contains(MapNamed("Mahogany_Gym_3").Everyone, n => n.Name == "Pryce");
        Assert.Contains(Overworld.Everyone, n => n.Species == "Gyarados");
        Assert.DoesNotContain(MapNamed("Blackthorn_Gym").Everyone, n => n.Name == "Clair");
        Assert.DoesNotContain(MapNamed("DragonsDen").Everyone, n => n.Name is "Clair" or "Lance");

        // Mahogany and Blackthorn are built their own ways, with songs of their own, as are the roads, the Ice Path and
        // the Dragon's Den; Mt. Mortar is a dungeon of the Union Cave's song and Routes 45 and 46 Route 34's
        Assert.Equal(Architecture.Upland, WorldModels.Of("ch_h01", "johto")!.Town);
        Assert.Equal(Architecture.Dragonstone, WorldModels.Of("fu_h01", "johto")!.Town);
        var mahogany = Johto.Area("mahogany")!;
        var blackthorn = Johto.Area("blackthorn")!;
        Assert.Equal("johto/mahogany", Overworld.BgmTrackAt(mahogany.Warps[3].X, mahogany.Warps[3].Z + 1));
        Assert.Equal("johto/blackthorn", Overworld.BgmTrackAt(blackthorn.Warps[6].X, blackthorn.Warps[6].Z + 1));
        var route42 = Johto.Area("route_42")!.Warps[2];
        Assert.Equal("johto/route42", Overworld.BgmTrackAt(route42.X, route42.Z + 1));
        Assert.Equal("johto/ice_path", MapNamed("IcePath_1F").BgmTrackAt(Johto.Area("ice_path_1f")!.Warps[0].X, Johto.Area("ice_path_1f")!.Warps[0].Z));
        var shrine = Johto.Area("dragons_den_shrine")!.Warps[0];
        Assert.Equal("johto/dragons_den", MapNamed("DragonsDen_Shrine").BgmTrackAt(shrine.X, shrine.Z));
        Assert.Equal("johto/union_cave", MapNamed("MtMortar_1F").BgmTrackAt(Johto.Area("mount_mortar_1f_entrance")!.Warps[0].X, Johto.Area("mount_mortar_1f_entrance")!.Warps[0].Z));

        // The trainers battle by HeartGold's records, the Dragon's Den's twins as one
        Assert.Contains(Overworld.Everyone, n => n.Id == "johto_hiker_benjamin");
        Assert.Contains(Overworld.Everyone, n => n.Id == "johto_hiker_erik");
        Assert.Contains(MapNamed("Mahogany_Gym_3").Everyone, n => n.Id == "johto_boarder_deandre");
        Assert.Contains(MapNamed("Blackthorn_Gym").Everyone, n => n.Id == "johto_ace_trainer_m_paulo");
        Assert.Contains(MapNamed("DragonsDen").Everyone, n => n.Id == "johto_ace_trainer_m_kobe");
        Assert.Equal(2, MapNamed("DragonsDen").Everyone.Count(n => n.Id == "johto_twins_clea_and_gil"));

        // Fly lands before the two new towns' Pokémon Centers
        foreach (string town in new[] { "mahogany", "blackthorn" })
            Assert.NotNull(SpawnLocations.ArrivedIn(RegionDatabase.Johto, town));
    }

    [Fact]
    public void TheTakeoversPlacesAreWalkedToAndTheirTrainersAreHeartGolds()
    {
        // Plan 20 · J11, part b opened the Radio Tower's upper floors, its lift and observation deck, and the passages
        // under Goldenrod City down to the warehouse and the Department Store's basement: each is walked into, the
        // shutters and the locked door counting as the story opens them
        var walked = Walked.Value;
        foreach (string name in new[] { "Goldenrod_RadioTower_2F", "Goldenrod_RadioTower_3F", "Goldenrod_RadioTower_4F",
                     "Goldenrod_RadioTower_5F", "Goldenrod_RadioTower_Elevator", "Goldenrod_RadioTower_ObservationDeck",
                     "Goldenrod_Underground_B1F", "Goldenrod_Underground_B2F", "Goldenrod_Underground_Warehouse",
                     "Goldenrod_DeptStore_Basement" })
            Assert.True(walked.Keys.Any(m => m.Name == name), $"nobody walks into {name}");

        // Petrel and Archer are battled by the scripts, Ariana, Proton (HeartGold's second record of him) and the floors' grunts by HeartGold's records
        Assert.Contains(MapNamed("Goldenrod_RadioTower_5F").Everyone, n => n.Id == "johto_executive_ariana_ariana");
        Assert.Contains(MapNamed("Goldenrod_RadioTower_4F").Everyone, n => n.Id == "johto_executive_proton_proton_2");
        Assert.Contains(MapNamed("Goldenrod_Underground_B2F").Everyone, n => n.Id == "johto_burglar_orson");
        Assert.Contains(MapNamed("Goldenrod_Underground_B1F").Everyone, n => n.Id == "johto_poke_maniac_donald");
        // The shutters of the second basement are ten, over twenty tiles, and the purple one three more
        Assert.Equal(20, MapNamed("Goldenrod_Underground_B2F").Everyone.Count(n => n.Key?.StartsWith("b2f_gate_", StringComparison.Ordinal) == true));
        Assert.Equal(3, MapNamed("Goldenrod_Underground_B2F").Everyone.Count(n => n.Key?.StartsWith("b2f_purple_gate_", StringComparison.Ordinal) == true));
        // The passages are a dungeon of the Union Cave's song; the tower keeps the city's
        Assert.Equal("johto/union_cave", MapNamed("Goldenrod_Underground_B2F").BgmTrackAt(27, 4));
        Assert.Equal("johto/goldenrod", MapNamed("Goldenrod_RadioTower_5F").BgmTrackAt(13, 3));
    }

    private static readonly string[] TheWayToTheLeague = { "route_27", "route_26", "johto_route_22", "indigo_plateau" };

    [Fact]
    public void TheLeagueIsWalkedToFromNewBarkTownByTohjoFallsAndVictoryRoad()
    {
        // Plan 20 · J10, part b: east of New Bark Town over the water of Route 27, through Tohjo Falls and up its falls,
        // north along Route 26 to the League's reception gate, through Victory Road's three floors and out onto the
        // Indigo Plateau, into the League's hall and on through the Elite Four's rooms, the Champion's and the Hall of Fame
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Johto").Value;
        foreach (string key in TheWayToTheLeague)
        {
            Assert.Contains(key, Johto.Index.Areas);
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        }
        string[] rooms = { "Route27_House", "TohjoFalls", "TohjoFalls_HiddenRoom", "Route26_NorthHouse",
            "Route26_WeekSiblingsHouse", "Route22_LeagueReceptionGate", "IndigoVictoryRoad_1F", "IndigoVictoryRoad_2F",
            "IndigoVictoryRoad_3F", "IndigoLeague_PokemonCenter_1F", "IndigoLeague_PokemonCenter_B1F", "IndigoLeague_WillRoom",
            "IndigoLeague_KogaRoom", "IndigoLeague_BrunoRoom", "IndigoLeague_KarenRoom", "IndigoLeague_LanceRoom",
            "IndigoLeague_HallOfFame" };
        foreach (string name in rooms)
            Assert.True(walked.Keys.Any(m => m.Name == name), $"nobody walks into {name}");

        // HeartGold's Kanto shares its keys with FireRed's and Platinum's: those that meet another region's take
        // Johto's prefix (HgWorld.Clashes), and the rest are Johto's own
        Assert.True(HgWorld.Clashes("victory_road_1f"));
        Assert.True(HgWorld.Clashes("route_22"));
        Assert.Equal("johto_pokemon_league_hall_of_fame", HgWorld.KeyOf("MAP_POKEMON_LEAGUE_HALL_OF_FAME"));
        Assert.Equal("route_26", HgWorld.KeyOf("MAP_ROUTE_26"));

        // The league's places are places only for now: nobody of the Elite Four, the Champion or the Hall of Fame stands
        // in them (plan 20 · J12), and every door between them stands open
        foreach (string name in new[] { "IndigoLeague_WillRoom", "IndigoLeague_KogaRoom", "IndigoLeague_BrunoRoom",
                     "IndigoLeague_KarenRoom", "IndigoLeague_LanceRoom", "IndigoLeague_HallOfFame" })
            Assert.Empty(MapNamed(name).Everyone);
        var hall = MapNamed("IndigoLeague_PokemonCenter_1F");
        Assert.Equal(InteriorStyle.PokemonCenter, hall.Interior);
        Assert.Contains(hall.Everyone, n => n.Script == "Nurse");

        // The reception gate: Route 28's door is shut, the guard turns back anyone short of eight Badges, and the door to
        // Kanto's Route 22 is held until the Champion has been faced
        var gate = MapNamed("Route22_LeagueReceptionGate");
        var route28 = Johto.Area("route_22_pokemon_league_reception_gate")!.Warps[3];
        Assert.Null(gate.GetWarpAt(route28.X, route28.Z));
        Assert.Contains(gate.Triggers, t => t.Script == "BadgeCheck" && t.Variable == "VAR_UNK_4110" && t.Value == 0);
        Assert.Contains(gate.Triggers, t => t.Script == "KantoShut");

        // The roads, Victory Road and the Plateau have songs of their own; Tohjo Falls is a cave of Union Cave's song
        var route26 = Johto.Area("route_26")!.Warps[0];
        Assert.Equal("johto/route26", Overworld.BgmTrackAt(route26.X, route26.Z + 1));
        var plateau = Johto.Area("indigo_plateau")!.Warps[1];
        Assert.Equal("johto/indigo_plateau", Overworld.BgmTrackAt(plateau.X, plateau.Z + 1));
        Assert.Equal("Indigo Plateau", Overworld.DisplayNameAt(plateau.X, plateau.Z + 1));
        var road = Johto.Area("johto_victory_road_1f")!.Warps[0];
        Assert.Equal("johto/victory_road", MapNamed("IndigoVictoryRoad_1F").BgmTrackAt(road.X, road.Z));
        var falls = Johto.Area("tohjo_falls")!.Warps[0];
        Assert.Equal("johto/union_cave", MapNamed("TohjoFalls").BgmTrackAt(falls.X, falls.Z));

        // The houses of the two routes are Viridian's, and the League one building of its own
        Assert.Equal(Architecture.Evergreen, WorldModels.Of("kn_h01", "johto")!.Town);
        Assert.Equal(BuildingKind.League, WorldModels.Of("pke_league", "johto")!.Kind);

        // The trainers battle by HeartGold's records
        foreach (string id in new[] { "johto_ace_trainer_f_megan", "johto_psychic_m_eli", "johto_fisherman_scott" })
            Assert.Contains(Overworld.Everyone, n => n.Id == id);

        // Fly lands before the League's door once the Plateau has been come to
        var spawn = SpawnLocations.ArrivedIn(RegionDatabase.Johto, "indigo_plateau")!;
        Assert.Equal("IndigoLeague_PokemonCenter_1F", spawn.Room);
        Assert.Equal(spawn.Room, Overworld.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
    }

    [Fact]
    public void JohtosTrainersBattleWithHeartGoldsPrizeAndPersonalities()
    {
        // Youngster Joey on Route 30: trainer 8 of trainers.h, of class 2 (TRAINERCLASS_YOUNGSTER, a man's), one Rattata
        // (19) at level 4 with a difficulty of 0. TrainerData_BuildParty seeds the generator with 0 + 4 + 19 + 8 = 31 and
        // runs it twice (the class's number): 31 × 1103515245 + 24691 = 4144226214 (mod 2^32), whose high half is 63235;
        // 4144226214 × 1103515245 + 24691 = 2066689569 (mod 2^32), whose high half is 31535; 31535 << 8 + 0x88 = 8073096.
        // CalcPrizeMoney: the last Pokémon's level 4 × 4 × the Youngster's 4 of sPrizeMoneyTbl = 64
        var joey = TrainerDatabase.Get("TRAINER_YOUNGSTER_JOEY", "johto")!;
        Assert.Equal("johto_youngster_joey", joey.Id);
        Assert.Equal(64, joey.PrizeMoney);
        var rattata = Assert.Single(joey.Party);
        Assert.Equal(("Rattata", 4, 8073096u), (rattata.Species, rattata.Level, rattata.Personality));
        // and he stands on Route 30 with that team
        var placed = Overworld.Everyone.Single(n => n.Key == "r30_gsboy2");
        Assert.Equal("johto_youngster_joey", placed.TrainerData?.Id);
        Assert.True(placed.IsTrainer);

        // A woman's Pokémon end in 0x78 (TRAINER_FEMALE): Lass, class 3
        Assert.Equal(new[] { false, true, false, true }, HgssTrainers.ClassWomen("static const u8 sTrainerGenders[] = { TRAINER_MALE, TRAINER_FEMALE, TRAINER_DOUBLE, TRAINER_FEMALE, };"));
        uint gender = 0x78;
        Assert.Equal(unchecked((31535u << 8) + 0x78u), HgssTrainers.Personality(0, 4, 19, 8, 2, ref gender));
        // An ability asked for sets the lowest bit, and keeps it for the Pokémon after
        HgssTrainers.Override("TRPOKE_GENDER_OVERRIDE_OFF", "TRPOKE_ABILITY_OVERRIDE_SECOND", 0, ref gender);
        Assert.Equal(0x79u, gender);
    }

    [Fact]
    public void AHiddenItemHoldsWhatHeartGoldsTableSays()
    {
        // HIDDENITEM_R29_R30_T21_NUGGET (1) is the row of sHiddenItemParam whose flag is 1: a Nugget
        var nugget = Overworld.HiddenItems[(572, 390)];
        Assert.Equal(("Nugget", 1, "FLAG_HIDDEN_ITEM_R29_R30_T21_NUGGET"), (nugget.Item, nugget.Count, nugget.Flag));
        Assert.Equal("Potion", Overworld.HiddenItems[(682, 391)].Item);
    }
}
