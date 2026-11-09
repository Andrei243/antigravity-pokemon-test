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

    private static readonly string[] Areas = { "postwick", "slumbering_weald", "galar_route_1", "wedgehurst", "galar_route_2" };

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
    public void TheOpenMapHasItsFiveAreasTheirSongsAndTheirPlaces()
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
        Assert.Equal(Region.Maps.Where(m => m != "Postwick_PlayersHouse_2F").OrderBy(m => m), map.Doors.Select(d => d.Room).OrderBy(m => m));
        foreach (var door in map.Doors)
        {
            Assert.NotNull(map.Marker(door.Out));
            var room = Rooms.Value(door.Room);
            Assert.True(room.IsWalkable(door.RoomX, door.RoomY), $"{door.Name} lands on a blocked tile of {door.Room}");
            var way = room.Warps.Single(w => w.TargetMap == "Galar");
            Assert.Equal(door, map.DoorBackFrom(room.Name, way.SourceX, way.SourceY));
        }
        // Wild Pokémon live in the grass of the two routes only, every row a species of the data, at their levels
        Assert.Equal(new[] { "route1_west", "route1_east", "route2_south", "route2_north" }, map.Zones.Select(z => z.Name));
        foreach (var slot in map.Zones.SelectMany(z => z.Slots))
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
    }

    [Fact]
    public void TheFirstChapterRunsFromTheBedroomToTheTrainHeadless()
    {
        var story = new StoryState();
        var party = new Party();
        var bag = new Inventory();
        var library = ScriptLibrary.Default;
        var open = OpenRegions.Load(Region).Single();
        HeadlessScriptHost Play(string file, string script, string? room = null)
        {
            var host = new HeadlessScriptHost(story, party, bag)
            {
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
        Assert.Equal("galarFree", Next());
        Assert.Equal(799, Objectives.Find("galarFree")!.Id);
    }

    [Fact]
    public void AShieldGameSeesZamazentaInTheFogAndASwordGameZacian()
    {
        foreach (var (version, seen) in new[] { (1, "Zacian"), (2, "Zamazenta") })
        {
            var story = new StoryState();
            story.SetVar("VAR_GALAR_VERSION", version);
            story.SetVar("VAR_GALAR_STORY", 4);
            var pokedex = new Pokedex();
            var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Region = RegionDatabase.Galar, OpenMap = Open.Value, Pokedex = pokedex };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find("Shadow", "slumbering_weald")!, null);
            runner.RunToEnd();
            Assert.True(pokedex.IsSeen(PokemonDatabase.Get(seen)!.DexNumber), seen);
            Assert.False(pokedex.IsSeen(PokemonDatabase.Get(version == 1 ? "Zamazenta" : "Zacian")!.DexNumber));
        }
    }
}
