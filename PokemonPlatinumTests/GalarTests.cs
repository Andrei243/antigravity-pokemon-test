using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Galar on the open world's engine (plan 33 · GL1; plan 32): the region's record, its open map of Postwick, the
/// Slumbering Weald, Route 1, Wedgehurst and Route 2 described in <c>Data/world3d/galar</c> and baked by
/// <c>tools/OpenWorldBake</c>, its eight grid rooms behind their doors, where a new game there begins, the walk of the
/// whole region through its doors and its navigation mesh, and the first chapter played headless from the bedroom to
/// the train. Floors, not counts, wherever a later session builds more.
/// </summary>
public class GalarTests
{
    private static Region Region => RegionDatabase.Get(RegionDatabase.Galar)!;

    // The open map read afresh for this class alone, and the rooms as fresh copies
    private static readonly Lazy<OpenMap> Open = new(() => OpenRegions.Load(Region).Single());
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    // The walk of the region from its start, through the rooms' ways out into the open map and its doors back in
    private static readonly Lazy<(Dictionary<Map, HashSet<(int X, int Y)>> Grid, Dictionary<string, OpenWalk> Open)> Walked = new(() =>
    {
        var open = new Dictionary<string, OpenWalk>(StringComparer.OrdinalIgnoreCase);
        var grid = WorldWalk.From(Rooms.Value, open, Region.Start!);
        return (grid, open);
    });

    private static readonly string[] Areas = { "postwick", "slumbering_weald", "galar_route_1", "wedgehurst", "galar_route_2",
        // The Wild Area's southern half (plan 33 · GL2; GalarWildAreaTests)
        "wild_area_meetup_spot", "wild_area_rolling_fields", "wild_area_dappled_grove", "wild_area_watchtower_ruins", "wild_area_lake_axewell",
        "wild_area_giants_seat", "wild_area_lake_miloch", "wild_area_motostoke_riverbank",
        // Motostoke, Route 3, Route 4 and Turffield (plan 33 · GL4)
        "motostoke", "galar_route_3", "galar_route_4", "turffield",
        // Route 5 and Hulbury (plan 33 · GL4, its second session)
        "galar_route_5", "hulbury",
        // Hammerlocke Hills, Hammerlocke, Route 6, Stow-on-Side and Ballonlea (plan 33 · GL5, its first session)
        "wild_area_hammerlocke_hills", "hammerlocke", "galar_route_6", "stow_on_side", "ballonlea",
        // The rest of the Wild Area's north, Routes 7 to 9, Circhester and Spikemuth (plan 33 · GL5, its second session)
        "wild_area_dusty_bowl", "wild_area_stony_wilderness", "wild_area_giants_cap", "wild_area_lake_of_outrage",
        "galar_route_7", "galar_route_8", "circhester", "galar_route_9", "spikemuth",
        // Wyndon (plan 33 · GL6)
        "wyndon" };

    [Fact]
    public void GalarIsBuiltOnTheOpenWorldWithItsFileAStartAndRoomsOfItsOwn()
    {
        Assert.True(Region.IsBuilt);
        Assert.Equal(8, Region.Generation);
        Assert.Equal(FieldKind.Open, Region.Field);
        Assert.Equal(new[] { "Galar" }, Region.OpenMaps);
        Assert.Equal(new[] { "Grookey", "Scorbunny", "Sobble" }, Region.Starters);
        Assert.Equal(8, Region.BadgeNames.Count);
        Assert.Equal("Grass", Region.BadgeNames[0]);
        Assert.Equal("Prof. Magnolia", Region.Professor!.Name);
        Assert.Equal("Hop", Region.Rival);
        Assert.Equal("galar", Region.Dex);
        Assert.True(Region.HasStory);
        Assert.Equal(MovementDirections.Free, Region.Movement.Directions);
        Assert.Equal(MovementSpeed.Analog, Region.Movement.Speed);
        Assert.True(Region.Movement.Offers(RegionMode.RotomBike));
        Assert.Equal(CharacterKit.Grown, Region.Kit);
        // Its levels for a crossed team climb from under Alola's post-game to its own Champion's, and its caps are Sword's
        Assert.Equal(224, Region.Levels!.Map(2));
        Assert.Equal(242, Region.Levels!.Map(65));
        Assert.Equal(new[] { 20, 24, 27, 36, 38, 42, 46, 48, 65 }, Region.File!.Caps);
        // Every room it makes is its own and no other region's, and none of them is an open map's name
        foreach (string name in Region.Maps)
        {
            Assert.Equal(1, RegionDatabase.All.Count(r => r.Maps.Contains(name)));
            Assert.NotNull(Rooms.Value(name));
            Assert.False(OpenRegions.IsOpenMap(name));
        }
        Assert.DoesNotContain("Galar", MapDatabase.MapNames);
        // The way from Alola lands here now that Galar is built, from its attendant in Hau'oli City's Pokémon Center
        var story = new StoryState();
        story.CompleteRegion(RegionDatabase.Alola);
        var link = RegionDatabase.LinkFrom(RegionDatabase.Alola)!;
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(link, story));
        Assert.Equal("HauoliCity_PokemonCenter_1F", link.DepartureMap);
        Assert.Contains(Rooms.Value(link.DepartureMap!).Everyone, n => n.IsTransportAttendant);
    }

    [Fact]
    public void TheBakeIsWhatTheDescriptionsGive()
    {
        // tools/OpenWorldBake's --check, held here: a description changed without baking it again fails
        string folder = Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "world3d", "galar");
        var description = OpenWorldBake.RegionDescription.Read(folder);
        var files = OpenWorldBake.RegionBake.Files(description);
        var differences = OpenWorldBake.RegionBake.Differences(Path.Combine(folder, "baked"), files);
        Assert.True(differences.Count == 0, "run `dotnet run --project tools/OpenWorldBake -- galar`:\n" + string.Join("\n", differences.Take(10)));
    }

    [Fact]
    public void TheOpenMapHasItsAreasTheirSongsAndTheirPlaces()
    {
        var map = Open.Value;
        Assert.Equal(Areas, map.Areas.Select(a => a.Key));
        foreach (var area in map.Areas) Assert.False(string.IsNullOrEmpty(area.Music), $"{area.Key} has no song");
        Assert.Equal("Fog", map.Areas.Single(a => a.Key == "slumbering_weald").Weather);
        // The keys meet no other region's: a route takes the region's name
        var others = World.LoadAll().SelectMany(w => w.Index.Areas).ToHashSet();
        Assert.DoesNotContain(Areas, others.Contains);
        Assert.NotNull(map.Land);
        Assert.NotNull(map.Land!.Nav);
        // A door to each room, and each room's way out leads back through it to its marker
        // (but the rooms come to only from another room: upstairs, and a stadium's mission and pitch; the Galar Mine has two)
        var behind = new[] { "Postwick_PlayersHouse_2F", "Motostoke_StadiumPitch", "Turffield_StadiumMission", "Turffield_StadiumPitch",
            "Hulbury_StadiumMission", "Hulbury_StadiumPitch", "Motostoke_StadiumMission",
            "StowOnSide_StadiumMission", "StowOnSide_StadiumPitch", "Ballonlea_StadiumMission", "Ballonlea_StadiumPitch",
            "Circhester_StadiumMission", "Circhester_StadiumPitch", "Hammerlocke_StadiumMission", "Hammerlocke_StadiumPitch",
            "Wyndon_StadiumPitch", "Wyndon_EnergyPlant" };
        Assert.Equal(Region.Maps.Where(m => !behind.Contains(m)).OrderBy(m => m), map.Doors.Select(d => d.Room).Distinct().OrderBy(m => m));
        foreach (var door in map.Doors)
        {
            Assert.NotNull(map.Marker(door.Out));
            var room = Rooms.Value(door.Room);
            Assert.True(room.IsWalkable(door.RoomX, door.RoomY), $"{door.Name} lands on a blocked tile of {door.Room}");
            Assert.Contains(room.Warps.Where(w => w.TargetMap == "Galar"), way => Equals(door, map.DoorBackFrom(room.Name, way.SourceX, way.SourceY)));
        }
        // Wild Pokémon live in the grass of the two routes and the Wild Area, every row a species of the data, the routes'
        // at their first levels (the Wild Area's are GalarWildAreaTests')
        Assert.Equal(new[] { "route1_west", "route1_east", "route2_south", "route2_north" }, map.Zones.Where(z => z.Place is "galar_route_1" or "galar_route_2").Select(z => z.Name));
        foreach (var slot in map.Zones.Where(z => z.Place is "galar_route_1" or "galar_route_2").SelectMany(z => z.Slots))
        {
            Assert.True(PokemonDatabase.Get(slot.Species) != null || PokemonDatabase.SpeciesOfForm(slot.Species) != null, slot.Species);
            Assert.InRange(slot.MinLevel, 2, 7);
            Assert.InRange(slot.MaxLevel, slot.MinLevel, 7);
        }
        // Route 2's three trainers are Galar's own, and Hop's nine teams answer the three starters
        foreach (var person in map.People.Where(p => p.TrainerId != null))
            Assert.NotNull(TrainerDatabase.Get(person.TrainerId, "galar"));
        foreach (string id in new[] { "1a", "1b", "1c", "2a", "2b", "2c", "3a", "3b", "3c" })
            Assert.NotNull(TrainerDatabase.Get("galar_hop_" + id, "galar"));
        Assert.Equal("Sobble", TrainerDatabase.Get("galar_hop_1a", "galar")!.Party[^1].Species);
        Assert.Equal("Grookey", TrainerDatabase.Get("galar_hop_1b", "galar")!.Party[^1].Species);
        Assert.Equal("Scorbunny", TrainerDatabase.Get("galar_hop_1c", "galar")!.Party[^1].Species);
    }

    [Fact]
    public void ANewGameInGalarBeginsInThePlayersRoomAndAChampionArrivesInsideTheDoor()
    {
        var start = Region.Start!;
        Assert.Equal("Postwick_PlayersHouse_2F", start.Map);
        var bedroom = Rooms.Value(start.Map);
        Assert.True(bedroom.IsIndoors);
        Assert.True(bedroom.IsWalkable(start.X, start.Y));
        var downstairs = Rooms.Value("Postwick_PlayersHouse_1F");
        Assert.Contains(downstairs.NPCs, n => n.Name == "Mum" && n.IsHealingNurse);
        var arrival = Region.ArrivalSpot!;
        Assert.Equal("Postwick_PlayersHouse_1F", arrival.Map);
        Assert.True(downstairs.IsWalkable(arrival.X, arrival.Y));
        Assert.Equal("Galar", downstairs.GetWarpAt(arrival.X, arrival.Y + 1)?.TargetMap);
        // A white-out goes home, then to Wedgehurst's Pokémon Center
        Assert.Equal(new[] { "Postwick_PlayersHouse_1F", "Wedgehurst_PokemonCenter" }, Region.File!.Spawns.Select(s => s.Room));
    }

    [Fact]
    public void FromTheBedroomEveryRoomAndEveryPlaceOfTheOpenMapIsReachedOnFoot()
    {
        var (grid, open) = Walked.Value;
        foreach (string room in Region.Maps)
            Assert.True(grid.Keys.Any(m => m.Name == room), $"{room} is never reached");
        Assert.True(open.TryGetValue("Galar", out var walk), "the walk never came out into Galar");
        var map = Open.Value;
        // Every marker, every door's own marker, every trigger's middle and every person's feet
        Assert.Empty(walk!.Unreached(map.Markers).Select(m => m.Name));
        foreach (var trigger in map.Triggers)
        {
            // (the ground at the trigger's middle, within its heights)
            var at = trigger.Shape.Base;
            (double Y, int Triangle)? ground;
            lock (OpenRegions.Reading) ground = map.Ground.Footing(at.MetresX, at.MetresZ, trigger.Shape.Max.MetresY, trigger.Shape.Min.MetresY, walkableOnly: true);
            Assert.True(ground != null, $"the trigger {trigger.Name} has no ground in it");
            Assert.True(walk.Reaches(OpenPos.FromMetres(at.MetresX, ground!.Value.Y, at.MetresZ)), $"the trigger {trigger.Name} can't be come to");
        }
        foreach (var person in map.People)
            Assert.True(walk.Reaches(person.Home), $"{person.Key} stands where nobody can come");
        // And the open map isn't one piece with the banks round it: their tops are never come to
        Assert.False(walk.Reaches(OpenPos.FromMetres(-80, 9, -150)));
        // The steps open into Motostoke (plan 33 · GL4), but the moor east of Route 3 and the ridge over the Galar Mine are
        // banks nobody walks: Turffield is come to through the mine alone
        Assert.True(walk.Reaches(OpenPos.FromMetres(0, 16, -660)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(60, 40, -820)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(-90, 40, -869)));
        // Plan 33 · GL4, its second session: Route 5 through the gap in Turffield's north bank and Hulbury beyond it are
        // come to, but Route 5's north bank and the east bank past Hulbury are not
        Assert.True(walk.Reaches(OpenPos.FromMetres(0, 27, -1010)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(100, 26, -1000)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(10, 40, -1050)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(155, 40, -1000)));
        // Plan 33 · GL5, its first session: through the gap in Route 5's north bank over Hammerlocke Hills, into Hammerlocke,
        // out of its west gate along Route 6 to Stow-on-Side, and through Glimwood Tangle to Ballonlea; the mass the land is cut
        // from (Hammerlocke's walls, the wall between the city and Ballonlea, the region's west edge) is never come to
        Assert.True(walk.Reaches(OpenPos.FromMetres(10, 26, -1100)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(12, 26, -1220)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(-100, 26, -1230)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(-96, 26, -1340)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(40, 26, -1340)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(30, 40, -1285)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(-40, 40, -1180)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(-80, 40, -1155)));
        // Plan 33 · GL5, its second session: the Wild Area's north either side of the hills, out of Hammerlocke's east gate
        // up Routes 7 and 8 to Circhester, along Route 9's shore and over the bay (the walk swims) to Spikemuth; the walls
        // between Route 7 and the Wild Area, between Route 8 and Ballonlea, and the mass north of Stow-on-Side never come to
        Assert.True(walk.Reaches(OpenPos.FromMetres(-120, 23, -1106)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(-66, 26, -1110)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(120, 26, -1140)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(130, 26, -1250)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(130, 26, -1380)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(100, 26, -1448)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(-84, 26, -1469)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(-134, 26, -1450)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(130, 40, -1155)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(103, 40, -1340)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(-100, 40, -1406)));
        // Plan 33 · GL6: Wyndon behind the mass north of Circhester, come to by the train from Hammerlocke alone: its
        // avenue and the way to Rose Tower's door are reached, the wall between it and Circhester never is
        Assert.True(walk.Reaches(OpenPos.FromMetres(0, 26, -1567)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(96, 26, -1588)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(60, 40, -1537)));
    }

    [Fact]
    public void TheFirstChapterRunsFromTheBedroomToTheTrainHeadless()
    {
        var story = new StoryState();
        var party = new Party();
        var bag = new Inventory();
        var library = ScriptLibrary.Default;
        var open = OpenRegions.Load(Region).Single();
        HeadlessScriptHost Play(string file, string script, string? room = null, Func<PokemonPlatinumEngine.Overworld.NPC?, BattleOutcome>? fight = null)
        {
            var host = new HeadlessScriptHost(story, party, bag)
            {
                Fight = fight,
                Map = room != null ? Rooms.Value(room) : null,
                OpenMap = room == null ? open : null,
                OpenMapNamed = name => name == open.Name ? open : null,
                Region = RegionDatabase.Galar
            };
            var runner = new ScriptRunner(library, host);
            runner.Start(library.Find(script, file)!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
        string? Next() => Objectives.Current(story, RegionDatabase.Galar)?.Name;

        StoryMigration.BeginNewGame(story, library);
        Assert.Equal("galarDownstairs", Next());
        Assert.True(story.Has("FLAG_GALAR_HIDE_LEON_STATION"));
        Assert.False(story.Has("FLAG_GALAR_HIDE_HOP_POSTWICK"));
        // The first morning: the scarf's crest is the version (the first answer, the sword)
        Play("Postwick_PlayersHouse_1F", "OnEnter", "Postwick_PlayersHouse_1F");
        Assert.Equal((1, 1), (story.Var("VAR_GALAR_STORY"), story.Var("VAR_GALAR_VERSION")));
        Assert.Equal("galarHop", Next());
        Play("postwick", "HopAtTheDoor");
        Assert.Equal(2, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.Has("FLAG_GALAR_HIDE_HOP_POSTWICK"));
        Assert.Equal("galarStation", Next());
        // The gate into the Weald is latched until the Wooloo slips through
        var latched = Play("postwick", "WealdGate");
        Assert.Equal("weald_back", latched.OnMarker["player"]);
        Assert.Equal(2, story.Var("VAR_GALAR_STORY"));
        // Leon home, back to Postwick, the three, Hop's first battle
        var home = Play("wedgehurst", "LeonArrives");
        Assert.Equal(3, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("hops_garden", home.OnMarker["player"]);
        Assert.Single(party.Members);
        Assert.Contains(party.Members[0].Species.Name, Region.Starters);
        Assert.False(story.Has("FLAG_GALAR_HIDE_HOP_GATE"));
        Assert.Equal("galarGate", Next());
        Play("postwick", "WealdGate");
        Assert.Equal(4, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarWooloo", Next());
        // The shadow in the fog is the version's legendary, seen and never battled; home again
        Play("slumbering_weald", "Shadow");
        Assert.Equal(5, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarMum", Next());
        Play("Postwick_PlayersHouse_1F", "OnEnter", "Postwick_PlayersHouse_1F");
        Assert.Equal(6, story.Var("VAR_GALAR_STORY"));
        Assert.Equal(5, bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.Equal("galarWedgehurst", Next());
        Play("wedgehurst", "HopWaits");
        Assert.Equal(7, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarMagnolia", Next());
        Play("Route2_MagnoliasHouse", "OnEnter", "Route2_MagnoliasHouse");
        Assert.Equal(8, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.Has("FLAG_HAS_POKEDEX"));
        Assert.Equal("galarGarden", Next());
        Play("galar_route_2", "HopsBattle");
        Assert.Equal(9, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarLake", Next());
        Play("galar_route_2", "WishingStars");
        Assert.Equal(10, story.Var("VAR_GALAR_STORY"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Dynamax Band")!));
        Assert.Equal("galarTrain", Next());
        Play("Wedgehurst_Station", "Attendant", "Wedgehurst_Station");
        Assert.Equal(11, story.Var("VAR_GALAR_STORY"));
        // The train runs north to the Wild Area (plan 33 · GL2), where it stops short and Hop goes on ahead
        Assert.Equal("galarWildArea", Next());
        Play("wild_area_meetup_spot", "OnEnter");
        Assert.Equal(12, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarWildArea", Next());
        // Plan 33 · GL4: up the steps, Hop in Motostoke, the registration, the hotel, the ceremony, Bede in the mine, the
        // Turffield mission and Milo's Grass Badge
        Play("wild_area_motostoke_riverbank", "MotostokeSteps");
        Assert.Equal(13, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarRegister", Next());
        Play("motostoke", "HopInMotostoke");
        Assert.Equal(14, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.Has("FLAG_GALAR_BEDE_IN_LOBBY"));
        var shut = Play("motostoke", "WestGate");
        Assert.Equal("motostoke_west_gate_back", shut.OnMarker["player"]);
        Play("Motostoke_Stadium", "OnEnter", "Motostoke_Stadium");
        Assert.Equal(15, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarHotel", Next());
        Play("Motostoke_Hotel", "OnEnter", "Motostoke_Hotel");
        Assert.Equal(16, story.Var("VAR_GALAR_STORY"));
        Assert.False(story.Has("FLAG_GALAR_YELL_AT_HOTEL"));
        Assert.Equal("galarCeremony", Next());
        Play("Motostoke_Stadium", "OnEnter", "Motostoke_Stadium");
        Assert.Equal(16, story.Var("VAR_GALAR_STORY"));
        var ceremony = Play("Motostoke_StadiumPitch", "OnEnter", "Motostoke_StadiumPitch");
        Assert.Equal(17, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("motostoke_stadium_door", ceremony.OnMarker["player"]);
        Assert.True(story.Has("FLAG_GALAR_BEDE_IN_MINE"));
        Assert.Equal("galarTurffield", Next());
        Play("GalarMine", "BedeInTheMine", "GalarMine");
        Assert.Equal(18, story.Var("VAR_GALAR_STORY"));
        Assert.False(story.Has("FLAG_GALAR_BEDE_IN_MINE"));
        Assert.Equal("galarMilo", Next());
        foreach (string wooloo in new[] { "MissionWooloo1", "MissionWooloo2", "MissionWooloo3" })
            Play("Turffield_StadiumMission", wooloo, "Turffield_StadiumMission");
        Assert.True(story.Has("FLAG_GALAR_TURFFIELD_WOOLOO_3"));
        Play("Turffield_StadiumPitch", "Milo", "Turffield_StadiumPitch");
        Assert.Equal(19, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Grass"));
        // Plan 33 · GL4, its second session: Hop on Route 5, Nessa's floodgates and Water Badge, Bede in Galar Mine No. 2,
        // Marnie by Motostoke's east wall, Kabu's round-up and Fire Badge. Galar Mine No. 2 is shut without the Water Badge
        Assert.Equal("galarRoute5", Next());
        Assert.True(story.Has("FLAG_GALAR_HOP_ROUTE_5"));
        var shutNorth = Play("hulbury", "MineTwoShut");
        Assert.Equal("galar_mine_2_back", shutNorth.OnMarker["player"]);
        var shutSouth = Play("motostoke", "MineTwoShut");
        Assert.Equal("galar_mine_2_back_south", shutSouth.OnMarker["player"]);
        Play("galar_route_5", "HopOnRoute5");
        Assert.Equal(20, story.Var("VAR_GALAR_STORY"));
        Assert.False(story.Has("FLAG_GALAR_HOP_ROUTE_5"));
        Assert.Equal("galarNessa", Next());
        foreach (string gate in new[] { "MissionSwitch1", "MissionSwitch2", "MissionSwitch3" })
            Play("Hulbury_StadiumMission", gate, "Hulbury_StadiumMission");
        Assert.True(story.Has("FLAG_GALAR_HULBURY_SWITCH_3"));
        Play("Hulbury_StadiumPitch", "Nessa", "Hulbury_StadiumPitch");
        Assert.Equal(21, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Water"));
        Assert.True(story.Has("FLAG_GALAR_BEDE_IN_MINE_2"));
        Assert.Equal("galarMineTwo", Next());
        var pass = Play("hulbury", "MineTwoShut");
        Assert.False(pass.OnMarker.ContainsKey("player"));
        Play("GalarMine2", "BedeInMineTwo", "GalarMine2");
        Assert.Equal(22, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.Has("FLAG_GALAR_MARNIE_IN_MOTOSTOKE"));
        Assert.Equal("galarMarnie", Next());
        Play("motostoke", "MarnieInMotostoke");
        Assert.Equal(23, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarKabu", Next());
        foreach (string round in new[] { "MissionVulpix", "MissionGrowlithe", "MissionSizzlipede" })
            Play("Motostoke_StadiumMission", round, "Motostoke_StadiumMission");
        Play("Motostoke_StadiumPitch", "Kabu", "Motostoke_StadiumPitch");
        Assert.Equal(24, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Fire"));
        Assert.Equal(3, story.BadgeCount(RegionDatabase.Galar));
        // Plan 33 · GL5, its first session: north over Hammerlocke Hills to Leon and Hop at the gate, Sonia and the tapestries
        // in the vault, Hop on Route 6, the fourth badge (Bea's in this Sword game), the mural and Opal's Fairy Badge.
        // Route 5's north gap was shut without the Fire Badge, and Glimwood Tangle is shut without the fourth
        Assert.Equal("galarHammerlocke", Next());
        var north = Play("galar_route_5", "NorthShut");
        Assert.False(north.OnMarker.ContainsKey("player"));
        var tangle = Play("stow_on_side", "GlimwoodShut");
        Assert.Equal("glimwood_west", tangle.OnMarker["player"]);
        Play("hammerlocke", "LeonInHammerlocke");
        Assert.Equal(25, story.Var("VAR_GALAR_STORY"));
        Assert.False(story.Has("FLAG_GALAR_LEON_HAMMERLOCKE"));
        Assert.Equal("galarVault", Next());
        Play("Hammerlocke_Vault", "OnEnter", "Hammerlocke_Vault");
        Assert.Equal(26, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarRoute6", Next());
        Play("galar_route_6", "HopOnRoute6");
        Assert.Equal(27, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarStow", Next());
        foreach (string gate in new[] { "MissionGate1", "MissionGate2", "MissionGate3" })
            Play("StowOnSide_StadiumMission", gate, "StowOnSide_StadiumMission");
        Assert.True(story.Has("FLAG_GALAR_STOW_GATE_3"));
        Play("StowOnSide_StadiumPitch", "OnEnter", "StowOnSide_StadiumPitch");
        Assert.False(story.Has("FLAG_GALAR_SHIELD_GAME"));
        Play("StowOnSide_StadiumPitch", "Bea", "StowOnSide_StadiumPitch");
        Assert.Equal(28, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Fighting"));
        Assert.Equal("Fighting", Region.BadgeNamesIn(story)[3]);
        Assert.Equal("galarMural", Next());
        Play("stow_on_side", "Mural");
        Assert.Equal(29, story.Var("VAR_GALAR_STORY"));
        Assert.False(story.Has("FLAG_GALAR_BEDE_AT_MURAL"));
        Assert.Equal("galarOpal", Next());
        var through = Play("stow_on_side", "GlimwoodShut");
        Assert.False(through.OnMarker.ContainsKey("player"));
        // A wrong answer leaves the quizmaster at the gate (the first answer, Dragon, is the first question's right one)
        Play("Ballonlea_StadiumMission", "MissionQuiz1", "Ballonlea_StadiumMission");
        Assert.True(story.Has("FLAG_GALAR_BALLONLEA_QUIZ_1"));
        Play("Ballonlea_StadiumPitch", "Opal", "Ballonlea_StadiumPitch");
        Assert.Equal(30, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Fairy"));
        Assert.Equal(5, story.BadgeCount(RegionDatabase.Galar));
        // Plan 33 · GL5, its second session: out of Hammerlocke's east gate (shut before the fifth badge, open now), Hop and
        // Sonia at the Hero's Bath, Gordie's Rock Badge (a Sword game), the bike fitted for the water on Route 9, Marnie at
        // Spikemuth's shutter, Piers's Dark Badge and Raihan's Dragon Badge, his stadium's door shut before the seventh
        Assert.Equal("galarCirchester", Next());
        Assert.True(story.Has("FLAG_GALAR_HOP_CIRCHESTER"));
        var east = Play("hammerlocke", "EastShut");
        Assert.False(east.OnMarker.ContainsKey("player"));
        var stadiumShut = Play("hammerlocke", "StadiumShut");
        Assert.Equal("hammerlocke_stadium_back", stadiumShut.OnMarker["player"]);
        Play("circhester", "HerosBath");
        Assert.Equal(31, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarSixth", Next());
        Play("galar_route_9", "BikeKit");
        Assert.False(story.Has("FLAG_GALAR_BIKE_WATER"));
        var shutter = Play("galar_route_9", "SpikemuthGate");
        Assert.Equal("route9_gate_back", shutter.OnMarker["player"]);
        foreach (string gate in new[] { "MissionGate1", "MissionGate2", "MissionGate3" })
            Play("Circhester_StadiumMission", gate, "Circhester_StadiumMission");
        Assert.True(story.Has("FLAG_GALAR_CIRCHESTER_GATE_3"));
        Play("Circhester_StadiumPitch", "OnEnter", "Circhester_StadiumPitch");
        Play("Circhester_StadiumPitch", "Gordie", "Circhester_StadiumPitch");
        Assert.Equal(32, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Rock"));
        Assert.False(story.Has("FLAG_GALAR_HOP_CIRCHESTER"));
        Assert.Equal("galarBike", Next());
        Play("galar_route_9", "BikeKit");
        Assert.Equal(33, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.Has("FLAG_GALAR_BIKE_WATER"));
        Assert.Equal(MovementGrants.Swim, OpenGrants.Of(story));
        Assert.Equal("galarSpikemuth", Next());
        Play("galar_route_9", "SpikemuthGate");
        Assert.Equal(34, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.Has("FLAG_GALAR_MARNIE_SPIKEMUTH"));
        Assert.Equal("galarPiers", Next());
        Play("spikemuth", "Piers");
        Assert.Equal(35, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Dark"));
        Assert.Equal("galarRaihan", Next());
        var raihanDoor = Play("hammerlocke", "StadiumShut");
        Assert.False(raihanDoor.OnMarker.ContainsKey("player"));
        foreach (string gate in new[] { "MissionGate1", "MissionGate2", "MissionGate3" })
            Play("Hammerlocke_StadiumMission", gate, "Hammerlocke_StadiumMission");
        Play("Hammerlocke_StadiumPitch", "Raihan", "Hammerlocke_StadiumPitch");
        Assert.Equal(36, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Dragon"));
        Assert.Equal(8, story.BadgeCount(RegionDatabase.Galar));
        Assert.True(TrainerDatabase.Get("galar_raihan_1", "galar")!.DoubleBattle);
        Assert.False(TrainerDatabase.Get("galar_piers_1", "galar")!.DynamaxBand);
        // Plan 33 · GL6: the train north to Wyndon, Leon and Hop on the avenue, the Semifinals (Marnie, Hop), the Darkest
        // Day (the stadium's lobby, the Weald's altar, Rose Tower shut until then, Oleana, the chairman, Eternamax's raid
        // and its catch), the Finals (Bede, Raihan) and the Champion Match, and the Hall of Fame
        Assert.Equal("galarWyndon", Next());
        Assert.True(story.Has("FLAG_GALAR_LEON_WYNDON"));
        Play("hammerlocke", "TrainToWyndon");
        Play("wyndon", "WyndonArrival");
        Assert.Equal(37, story.Var("VAR_GALAR_STORY"));
        Assert.False(story.Has("FLAG_GALAR_LEON_WYNDON"));
        Assert.Equal("galarSemifinal", Next());
        Play("Wyndon_StadiumPitch", "OnEnter", "Wyndon_StadiumPitch");
        Assert.True(story.Has("FLAG_GALAR_CUP_MARNIE"));
        Play("Wyndon_StadiumPitch", "Marnie", "Wyndon_StadiumPitch");
        Assert.Equal(38, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.Has("FLAG_GALAR_CUP_HOP"));
        Assert.Equal("galarHopCup", Next());
        Play("Wyndon_StadiumPitch", "Hop", "Wyndon_StadiumPitch");
        Assert.Equal(39, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarLobby", Next());
        var tower = Play("wyndon", "TowerShut");
        Assert.Equal("rose_tower_back", tower.OnMarker["player"]);
        Play("Wyndon_Stadium", "OnEnter", "Wyndon_Stadium");
        Assert.Equal(40, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.Has("FLAG_GALAR_DARKEST_DAY"));
        Assert.Equal("galarAltar", Next());
        Play("slumbering_weald", "Altar");
        Assert.Equal(41, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarRoseTower", Next());
        var towerOpen = Play("wyndon", "TowerShut");
        Assert.False(towerOpen.OnMarker.ContainsKey("player"));
        Play("Wyndon_RoseTower", "Oleana", "Wyndon_RoseTower");
        Assert.Equal(42, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.Has("FLAG_GALAR_OLEANA_BEATEN"));
        Assert.Equal("galarEnergyPlant", Next());
        var plant = Play("Wyndon_EnergyPlant", "OnEnter", "Wyndon_EnergyPlant");
        Assert.Equal(43, story.Var("VAR_GALAR_STORY"));
        Assert.Contains(plant.Log, l => l.StartsWith("raidbattle Eternatus 60 5 stars", StringComparison.Ordinal));
        Assert.Contains(party.Members, p => p.Species.Name == "Eternatus");
        Assert.False(story.Has("FLAG_GALAR_DARKEST_DAY"));
        Assert.Equal("galarFinals", Next());
        Play("Wyndon_StadiumPitch", "OnEnter", "Wyndon_StadiumPitch");
        Assert.True(story.Has("FLAG_GALAR_CUP_BEDE"));
        Play("Wyndon_StadiumPitch", "Bede", "Wyndon_StadiumPitch");
        Assert.Equal(44, story.Var("VAR_GALAR_STORY"));
        Play("Wyndon_StadiumPitch", "Raihan", "Wyndon_StadiumPitch");
        Assert.Equal(45, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarLeon", Next());
        Play("Wyndon_StadiumPitch", "Leon", "Wyndon_StadiumPitch");
        Assert.Equal(46, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.IsRegionComplete(RegionDatabase.Galar));
        // Plan 33 · GL7: after the Hall of Fame, the version's legendary at the Weald's altar (a battle won leaves it in the
        // fog for the next try; a catch ends the story there), and Leon's rematch in the Battle Tower
        Assert.Equal("galarWeald", Next());
        var left = Play("slumbering_weald", "Legend");
        Assert.Contains(left.Log, l => l.StartsWith("wildbattle Zacian 70 Won", StringComparison.Ordinal));
        Assert.Equal(46, story.Var("VAR_GALAR_STORY"));
        Play("slumbering_weald", "Legend", fight: _ => BattleOutcome.Caught);
        Assert.Equal(47, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("galarLeonTower", Next());
        var battleTower = Play("Wyndon_RoseTower", "LeonTower", "Wyndon_RoseTower");
        Assert.Contains(battleTower.Log, l => l.StartsWith("battle galar_leon_rematch", StringComparison.Ordinal));
        Assert.True(story.Has("FLAG_GALAR_LEON_TOWER_WON"));
        Assert.Equal("galarFree", Next());
        Assert.Equal(799, Objectives.Find("galarFree")!.Id);
    }

    [Fact]
    public void EveryOpenSceneOfTheChapterPlayedAndSkippedLeavesTheSame()
    {
        // The scenes the open places' triggers start (SceneSkipTests' walk plays only what a grid map starts): each from
        // the story it waits at, played and then hurried as a player holding the button would, the same story, team,
        // bag and markers afterwards
        foreach (var (file, script, story) in new[]
        {
            ("postwick", "HopAtTheDoor", 1), ("postwick", "WealdGate", 3), ("slumbering_weald", "Shadow", 4),
            ("wedgehurst", "LeonArrives", 2), ("wedgehurst", "HopWaits", 6), ("galar_route_2", "HopsBattle", 8),
            ("galar_route_2", "WishingStars", 9), ("wild_area_meetup_spot", "OnEnter", 11),
            // Plan 33 · GL4's open scenes: up the steps, and Hop at the top of them
            ("wild_area_motostoke_riverbank", "MotostokeSteps", 12), ("motostoke", "HopInMotostoke", 13),
            // Plan 33 · GL4, its second session: Hop on Route 5, and Marnie by Motostoke's east wall
            ("galar_route_5", "HopOnRoute5", 19), ("motostoke", "MarnieInMotostoke", 22),
            // Plan 33 · GL5, its first session: Leon at Hammerlocke's gate, Hop on Route 6, the mural
            ("hammerlocke", "LeonInHammerlocke", 24), ("galar_route_6", "HopOnRoute6", 26), ("stow_on_side", "Mural", 28),
            // Plan 33 · GL5, its second session: the Hero's Bath, the bike's kit, Marnie at the gate, Piers on his stage
            ("circhester", "HerosBath", 30), ("galar_route_9", "BikeKit", 32), ("galar_route_9", "SpikemuthGate", 33), ("spikemuth", "Piers", 34),
            // Plan 33 · GL6: Leon and Hop on Wyndon's avenue, the altar in the Weald on the Darkest Day
            ("wyndon", "WyndonArrival", 36), ("slumbering_weald", "Altar", 40),
            // Plan 33 · GL7: the version's legendary at the altar after the Hall of Fame, caught
            ("slumbering_weald", "Legend", 46)
        })
        {
            var ends = new List<string>();
            foreach (bool hurry in new[] { false, true })
            {
                var host = new HeadlessScriptHost { Region = RegionDatabase.Galar, OpenMap = Open.Value, OpenMapNamed = n => n == "Galar" ? Open.Value : null,
                    Fight = script == "Legend" ? _ => BattleOutcome.Caught : null };
                StoryMigration.BeginNewGame(host.Story, ScriptLibrary.Default);
                host.Story.SetVar("VAR_GALAR_STORY", story);
                host.Story.SetVar("VAR_GALAR_VERSION", 1);
                // (whoever a scene faces stands where the scene before left them)
                foreach (string shown in new[] { "FLAG_GALAR_LEON_HAMMERLOCKE", "FLAG_GALAR_HOP_HAMMERLOCKE", "FLAG_GALAR_HOP_ROUTE_6", "FLAG_GALAR_BEDE_AT_MURAL" })
                    if (script is "LeonInHammerlocke" or "HopOnRoute6" or "Mural") host.Story.Set(shown);
                foreach (string shown in new[] { "FLAG_GALAR_HOP_CIRCHESTER", "FLAG_GALAR_SONIA_CIRCHESTER", "FLAG_GALAR_MARNIE_ROUTE_9", "FLAG_GALAR_MARNIE_SPIKEMUTH" })
                    if (script is "HerosBath" or "SpikemuthGate" or "Piers") host.Story.Set(shown);
                foreach (string shown in new[] { "FLAG_GALAR_LEON_WYNDON", "FLAG_GALAR_HOP_WYNDON", "FLAG_GALAR_HOP_ALTAR" })
                    if (script is "WyndonArrival" or "Altar" or "Legend") host.Story.Set(shown);
                // (the bike's kit waits for the sixth badge, Piers's battle for nothing: a story of 32 has it)
                if (story >= 32) host.Story.GiveBadge(RegionDatabase.Galar, "Rock");
                host.Party.Add(new Pokemon(PokemonDatabase.Get("Grookey")!, 5, new Random(0)));
                var runner = new ScriptRunner(ScriptLibrary.Default, host) { Impatient = hurry };
                runner.Start(ScriptLibrary.Default.Find(script, file)!, null);
                runner.RunToEnd();
                Assert.Empty(host.Problems);
                Assert.True(host.Story.Var("VAR_GALAR_STORY") > story, $"{file}.{script} left the story where it was");
                ends.Add(System.Text.Json.JsonSerializer.Serialize(host.Story.Snapshot()) + "|" + string.Join(",", host.Party.Members.Select(p => p.Species.Name))
                    + "|" + host.Bag.GetQuantity(ItemDatabase.Get("Dynamax Band")!) + "|" + string.Join(",", host.OnMarker.OrderBy(m => m.Key).Select(m => m.Key + "=" + m.Value)));
            }
            Assert.True(ends[0] == ends[1], $"{file}.{script} skipped leaves otherwise than played:\n{ends[0]}\n{ends[1]}");
        }
    }

    [Fact]
    public void AShieldGameSeesZamazentaInTheFogAndASwordGameZacian()
    {
        foreach (var (version, seen) in new[] { (1, "Zacian"), (2, "Zamazenta") })
        {
            var story = new StoryState();
            story.SetVar("VAR_GALAR_VERSION", version);
            story.SetVar("VAR_GALAR_STORY", 4);
            var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Region = RegionDatabase.Galar, OpenMap = Open.Value };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find("Shadow", "slumbering_weald")!, null);
            runner.RunToEnd();
            Assert.Equal(new[] { seen }, host.SeenSpecies);
        }
    }

    [Fact]
    public void AShieldGamesFourthBadgeIsAllistersGhostBadgeInTheSamePlace()
    {
        // Plan 33 · decision 2 and GL5: Bea and Allister stand on one spot of the pitch, the version's shown; the Ghost Badge
        // is the Fighting Badge's place on the card, named by the version, and a script may ask either name
        var story = new StoryState();
        story.SetVar("VAR_GALAR_VERSION", 2);
        story.SetVar("VAR_GALAR_STORY", 27);
        var pitch = Rooms.Value("StowOnSide_StadiumPitch");
        void Play(string script)
        {
            var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Region = RegionDatabase.Galar, Map = pitch };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(script, "StowOnSide_StadiumPitch")!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
        }
        Play("OnEnter");
        Assert.True(story.Has("FLAG_GALAR_SHIELD_GAME"));
        pitch.ApplyPresence(story.Has);
        Assert.Contains(pitch.NPCs, n => n.Id == "allister");
        Assert.DoesNotContain(pitch.NPCs, n => n.Id == "bea");
        Play("Allister");
        Assert.Equal(28, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Ghost"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Fighting"));
        Assert.Equal(1, story.BadgeCount(RegionDatabase.Galar));
        Assert.Equal(new[] { "Grass", "Water", "Fire", "Ghost", "Fairy", "Ice", "Dark", "Dragon" }, Region.BadgeNamesIn(story));
        story.SetVar("VAR_GALAR_VERSION", 1);
        Assert.Equal(Region.BadgeNames, Region.BadgeNamesIn(story));
    }

    [Fact]
    public void AShieldGamesSixthBadgeIsMelonysIceBadgeInTheSamePlace()
    {
        // Plan 33 · GL5, its second session: Gordie and Melony stand on one spot of Circhester's pitch as Bea and Allister do
        var story = new StoryState();
        story.SetVar("VAR_GALAR_VERSION", 2);
        story.SetVar("VAR_GALAR_STORY", 31);
        var pitch = Rooms.Value("Circhester_StadiumPitch");
        void Play(string script)
        {
            var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Region = RegionDatabase.Galar, Map = pitch };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(script, "Circhester_StadiumPitch")!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
        }
        Play("OnEnter");
        pitch.ApplyPresence(story.Has);
        Assert.Contains(pitch.NPCs, n => n.Id == "melony");
        Assert.DoesNotContain(pitch.NPCs, n => n.Id == "gordie");
        Play("Melony");
        Assert.Equal(32, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Ice"));
        Assert.True(story.HasBadge(RegionDatabase.Galar, "Rock"));
        Assert.Equal("Ice", Region.BadgeNamesIn(story)[5]);
    }

    [Fact]
    public void TheBayStopsAWalkerAtItsEdgeAndTheBikesKitTakesThemOver()
    {
        // Plan 33 · GL5's done-when: Route 9's bay is crossed with the grant the mechanic's kit gives, and a walker without it
        // stops at the water's edge (a swim of the player's own until the Rotom Bike has an engine)
        var map = Open.Value;
        foreach (bool kit in new[] { false, true })
        {
            var story = new StoryState();
            if (kit) story.Set(OpenGrants.BikeWaterFlag);
            OpenPos start;
            lock (OpenRegions.Reading) start = OpenPos.FromMetres(34, map.Ground.Footing(34, -1470, 30, 10, walkableOnly: true)!.Value.Y, -1470);
            var field = new OpenField(map, start, 270) { Grants = OpenGrants.Of(story) };
            bool stopped = false;
            lock (OpenRegions.Reading)
                for (int i = 0; i < 40000 && field.Player.Pos.MetresX > -78; i++)
                {
                    field.Player.Step(field.Map, new OpenInput(-1, 0, Run: true), field.Grants);
                    stopped |= field.Player.Body.StoppedAtWater;
                }
            if (kit) Assert.True(field.Player.Pos.MetresX <= -78, $"the kit took the player only to x {field.Player.Pos.MetresX:0.0}");
            else
            {
                Assert.True(stopped, "a walker without the kit never met the water's edge");
                Assert.InRange(field.Player.Pos.MetresX, 8, 30);
            }
        }
    }

    [Fact]
    public void SpikemuthIsHeldInTheNightAndWyndonUnderTheDarkestDayOnlyWhileItLasts()
    {
        // Plan 33 · GL6 (style guide, "Open world", "A place's own light"): an area's light, wanted where its flag allows it
        var map = Open.Value;
        OpenArea At(double x, double z) => map.AreaAt(OpenPos.FromMetres(x, 26.3, z))!;
        var story = new StoryState();
        Assert.Equal(PokemonPlatinumEngine.Graphics.OpenWorld.OpenLook.Night,
            PokemonPlatinumEngine.Graphics.OpenWorld.OpenMoodEase.Wanted(At(-134, -1450), story.Has));
        Assert.Null(PokemonPlatinumEngine.Graphics.OpenWorld.OpenMoodEase.Wanted(At(0, -1567), story.Has));
        Assert.Null(PokemonPlatinumEngine.Graphics.OpenWorld.OpenMoodEase.Wanted(At(100, -1448), story.Has));
        story.Set("FLAG_GALAR_DARKEST_DAY");
        Assert.Equal(PokemonPlatinumEngine.Graphics.OpenWorld.OpenLook.DarkestDay,
            PokemonPlatinumEngine.Graphics.OpenWorld.OpenMoodEase.Wanted(At(0, -1567), story.Has));
        // It eases in over a second and out again, and one light goes before another comes
        var ease = new PokemonPlatinumEngine.Graphics.OpenWorld.OpenMoodEase();
        ease.Settle(null);
        ease.Advance("night", 0.5);
        Assert.Equal(("night", 0.5), (ease.Mood, ease.Share));
        ease.Advance("darkestDay", 0.25);
        Assert.Equal(("night", 0.25), (ease.Mood, ease.Share));
        ease.Advance("darkestDay", 0.25);
        ease.Advance("darkestDay", 2);
        Assert.Equal(("darkestDay", 1.0), (ease.Mood, ease.Share));
        ease.Advance(null, 2);
        Assert.Equal((null as string, 0.0), (ease.Mood, ease.Share));
        // With no light, or none of it in, the rig of the hour is handed back to the bit
        var noon = PokemonPlatinumEngine.Graphics.OpenWorld.OpenLook.Open(12f);
        Assert.Equal(noon, PokemonPlatinumEngine.Graphics.OpenWorld.OpenLook.InMood(noon, null, 1f));
        Assert.Equal(noon, PokemonPlatinumEngine.Graphics.OpenWorld.OpenLook.InMood(noon, "night", 0f));
        Assert.NotEqual(noon, PokemonPlatinumEngine.Graphics.OpenWorld.OpenLook.InMood(noon, "darkestDay", 1f));
    }

    [Fact]
    public void TheWealdsFirstMeetingIsAStagedBattleWithTheVersionsLegendary()
    {
        // Plan 33 · GL6 (docs/mechanics/rulings.md, "Staged battles"): its level hidden, untouchable, passive, gone in
        // three turns; and so it gives no way to the species
        foreach (var (version, species) in new[] { (1, "Zacian"), (2, "Zamazenta") })
        {
            var story = new StoryState();
            story.SetVar("VAR_GALAR_VERSION", version);
            story.SetVar("VAR_GALAR_STORY", 4);
            var party = new Party();
            party.Add(new Pokemon(PokemonDatabase.Get("Grookey")!, 5, new Random(0)));
            var host = new HeadlessScriptHost(story, party, new Inventory()) { Region = RegionDatabase.Galar, OpenMap = Open.Value };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find("Shadow", "slumbering_weald")!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            Assert.Contains(host.Log, l => l.StartsWith($"wildbattle {species} 70", StringComparison.Ordinal) && l.Contains("hidelevel untouchable passive endafter 3"));
        }
    }
}
