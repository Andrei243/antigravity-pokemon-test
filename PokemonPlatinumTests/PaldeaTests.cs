using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Paldea on the open world's engine (plan 34 · PD1; plan 32), as Galar is: the region's record, its open map of Cabo
/// Poco, Poco Path and its beach, Los Platos, the South Province's first area and Mesagoza described in
/// <c>Data/world3d/paldea</c> and baked by <c>tools/OpenWorldBake</c>, its six grid rooms behind their doors, where a new
/// game there begins, the walk of the region through its doors and its navigation mesh, and the first chapter played
/// headless from the bedroom to the academy's door. Floors, not counts, wherever a later session builds more.
/// </summary>
public class PaldeaTests
{
    private static Region Region => RegionDatabase.Get(RegionDatabase.Paldea)!;

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

    private static readonly string[] Areas = { "cabo_poco", "poco_path", "los_platos", "south_province_area_one", "mesagoza" };

    [Fact]
    public void PaldeaIsBuiltOnTheOpenWorldWithItsFileAStartAndRoomsOfItsOwn()
    {
        Assert.True(Region.IsBuilt);
        Assert.Equal(9, Region.Generation);
        Assert.Equal(FieldKind.Open, Region.Field);
        Assert.Equal(new[] { "Paldea" }, Region.OpenMaps);
        Assert.Equal(new[] { "Sprigatito", "Fuecoco", "Quaxly" }, Region.Starters);
        Assert.Equal(new[] { "Bug", "Grass", "Electric", "Water", "Normal", "Ghost", "Psychic", "Ice" }, Region.BadgeNames);
        Assert.Equal("Director Clavell", Region.Professor!.Name);
        Assert.Equal("Nemona", Region.Rival);
        Assert.Equal("paldea", Region.Dex);
        Assert.True(Region.HasStory);
        Assert.Equal(MovementDirections.Free, Region.Movement.Directions);
        Assert.Equal(MovementSpeed.Analog, Region.Movement.Speed);
        Assert.True(Region.Movement.Offers(RegionMode.Mount));
        Assert.Equal(CharacterKit.Grown, Region.Kit);
        // Its levels for a crossed team climb from Galar's Champion to two under the cap (plan 03 · D14's Paldea proposals,
        // question 1), and its caps are Scarlet and Violet's
        Assert.Equal(242, Region.Levels!.Map(2));
        Assert.Equal(258, Region.Levels!.Map(62));
        Assert.Equal(new[] { 15, 17, 24, 30, 36, 42, 45, 48, 62 }, Region.File!.Caps);
        // Every room it makes is its own and no other region's, and none of them is an open map's name
        foreach (string name in Region.Maps)
        {
            Assert.Equal(1, RegionDatabase.All.Count(r => r.Maps.Contains(name)));
            Assert.NotNull(Rooms.Value(name));
            Assert.False(OpenRegions.IsOpenMap(name));
        }
        Assert.DoesNotContain("Paldea", MapDatabase.MapNames);
        // The plane from Galar lands here now that Paldea is built, from its attendant in Wyndon's airfield
        var story = new StoryState();
        story.CompleteRegion(RegionDatabase.Galar);
        var link = RegionDatabase.LinkFrom(RegionDatabase.Galar)!;
        Assert.Equal(RegionDatabase.Paldea, link.To);
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(link, story));
        Assert.Equal("Wyndon_Airfield", link.DepartureMap);
        // A return lands inside Los Platos's Pokémon Center, on open floor
        var harbour = Region.HarbourSpot!;
        Assert.True(Rooms.Value(harbour.Map).IsWalkable(harbour.X, harbour.Y));
    }

    [Fact]
    public void TheBakeIsWhatTheDescriptionsGive()
    {
        // tools/OpenWorldBake's --check, held here: a description changed without baking it again fails
        string folder = Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "world3d", "paldea");
        var description = OpenWorldBake.RegionDescription.Read(folder);
        var files = OpenWorldBake.RegionBake.Files(description);
        var differences = OpenWorldBake.RegionBake.Differences(Path.Combine(folder, "baked"), files);
        Assert.True(differences.Count == 0, "run `dotnet run --project tools/OpenWorldBake -- paldea`:\n" + string.Join("\n", differences.Take(10)));
    }

    [Fact]
    public void TheOpenMapHasItsAreasTheirSongsAndTheirPlaces()
    {
        var map = Open.Value;
        Assert.Equal(Areas, map.Areas.Select(a => a.Key));
        foreach (var area in map.Areas) Assert.False(string.IsNullOrEmpty(area.Music), $"{area.Key} has no song");
        // The keys meet no other region's, grid or open
        var others = World.LoadAll().SelectMany(w => w.Index.Areas).ToHashSet();
        Assert.DoesNotContain(Areas, others.Contains);
        Assert.All(Areas, a => Assert.Equal("Paldea", OpenRegions.MapOfArea(a)));
        Assert.NotNull(map.Land);
        Assert.NotNull(map.Land!.Nav);
        // A door to each room, and each room's way out leads back through it to its marker (but upstairs, come to from
        // downstairs)
        var behind = new[] { "CaboPoco_PlayersHouse_2F" };
        Assert.Equal(Region.Maps.Where(m => !behind.Contains(m)).OrderBy(m => m), map.Doors.Select(d => d.Room).Distinct().OrderBy(m => m));
        foreach (var door in map.Doors)
        {
            Assert.NotNull(map.Marker(door.Out));
            var room = Rooms.Value(door.Room);
            Assert.True(room.IsWalkable(door.RoomX, door.RoomY), $"{door.Name} lands on a blocked tile of {door.Room}");
            Assert.Contains(room.Warps.Where(w => w.TargetMap == "Paldea"), way => Equals(door, map.DoorBackFrom(room.Name, way.SourceX, way.SourceY)));
        }
        // Wild Pokémon live in the grass of Poco Path, its beach and the South Province, every row a species of the data at
        // the first chapter's levels
        Assert.Equal(new[] { "poco_path_west", "poco_path_lighthouse", "poco_beach", "area_one_west", "area_one_east" }, map.Zones.Select(z => z.Name));
        foreach (var slot in map.Zones.SelectMany(z => z.Slots))
        {
            Assert.NotNull(PokemonDatabase.Get(slot.Species));
            Assert.InRange(slot.MinLevel, 2, 7);
            Assert.InRange(slot.MaxLevel, slot.MinLevel, 7);
        }
        // The two students are Paldea's own, and Nemona's three teams answer the three starters
        foreach (var person in map.People.Where(p => p.TrainerId != null))
            Assert.NotNull(TrainerDatabase.Get(person.TrainerId, "paldea"));
        Assert.Equal("Fuecoco", TrainerDatabase.Get("paldea_nemona_1a", "paldea")!.Party[^1].Species);
        Assert.Equal("Quaxly", TrainerDatabase.Get("paldea_nemona_1b", "paldea")!.Party[^1].Species);
        Assert.Equal("Sprigatito", TrainerDatabase.Get("paldea_nemona_1c", "paldea")!.Party[^1].Species);
    }

    [Fact]
    public void ANewGameInPaldeaBeginsInThePlayersRoomAndAChampionArrivesInsideTheDoor()
    {
        var start = Region.Start!;
        Assert.Equal("CaboPoco_PlayersHouse_2F", start.Map);
        var bedroom = Rooms.Value(start.Map);
        Assert.True(bedroom.IsIndoors);
        Assert.True(bedroom.IsWalkable(start.X, start.Y));
        var downstairs = Rooms.Value("CaboPoco_PlayersHouse_1F");
        Assert.Contains(downstairs.NPCs, n => n.Name == "Mom" && n.IsHealingNurse);
        var arrival = Region.ArrivalSpot!;
        Assert.Equal("CaboPoco_PlayersHouse_1F", arrival.Map);
        Assert.True(downstairs.IsWalkable(arrival.X, arrival.Y));
        Assert.Equal("Paldea", downstairs.GetWarpAt(arrival.X, arrival.Y + 1)?.TargetMap);
        // A white-out goes home, then to Los Platos's Pokémon Center
        Assert.Equal(new[] { "CaboPoco_PlayersHouse_1F", "LosPlatos_PokemonCenter" }, Region.File!.Spawns.Select(s => s.Room));
    }

    [Fact]
    public void FromTheBedroomEveryRoomAndEveryPlaceOfTheOpenMapIsReachedOnFoot()
    {
        var (grid, open) = Walked.Value;
        foreach (string room in Region.Maps)
            Assert.True(grid.Keys.Any(m => m.Name == room), $"{room} is never reached");
        Assert.True(open.TryGetValue("Paldea", out var walk), "the walk never came out into Paldea");
        var map = Open.Value;
        // Every marker, every door's own marker, every trigger's middle and every person's feet
        Assert.Empty(walk!.Unreached(map.Markers).Select(m => m.Name));
        foreach (var trigger in map.Triggers)
        {
            var at = trigger.Shape.Base;
            (double Y, int Triangle)? ground;
            lock (OpenRegions.Reading) ground = map.Ground.Footing(at.MetresX, at.MetresZ, trigger.Shape.Max.MetresY, trigger.Shape.Min.MetresY, walkableOnly: true);
            Assert.True(ground != null, $"the trigger {trigger.Name} has no ground in it");
            Assert.True(walk.Reaches(OpenPos.FromMetres(at.MetresX, ground!.Value.Y, at.MetresZ)), $"the trigger {trigger.Name} can't be come to");
        }
        foreach (var person in map.People)
            Assert.True(walk.Reaches(person.Home), $"{person.Key} stands where nobody can come");
        // The beach under the cliffs is come to by the track down from Poco Path, and the academy's hill by its stairs
        Assert.True(walk.Reaches(OpenPos.FromMetres(-70, 1.3, -40)));
        Assert.True(walk.Reaches(OpenPos.FromMetres(0, 22, -356)));
        // The banks at the region's present edges are never come to: the east hills, the north mountains, the west heights
        Assert.False(walk.Reaches(OpenPos.FromMetres(93, 20, -150)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(0, 32, -392)));
        Assert.False(walk.Reaches(OpenPos.FromMetres(-92, 20, -330)));
    }

    [Fact]
    public void TheFirstChapterRunsFromTheBedroomToTheAcademysDoorHeadless()
    {
        var story = new StoryState();
        var party = new Party();
        var bag = new Inventory();
        var library = ScriptLibrary.Default;
        var open = OpenRegions.Load(Region).Single();
        HeadlessScriptHost Play(string file, string script, string? room = null, params int[] answers)
        {
            var host = new HeadlessScriptHost(story, party, bag)
            {
                Map = room != null ? Rooms.Value(room) : null,
                OpenMap = room == null ? open : null,
                OpenMapNamed = name => name == open.Name ? open : null,
                Region = RegionDatabase.Paldea
            };
            foreach (int answer in answers) host.Answers.Enqueue(answer);
            var runner = new ScriptRunner(library, host);
            runner.Start(library.Find(script, file)!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
        string? Next() => Objectives.Current(story, RegionDatabase.Paldea)?.Name;

        StoryMigration.BeginNewGame(story, library);
        Assert.Equal("paldeaDownstairs", Next());
        Assert.True(story.Has("FLAG_PALDEA_HIDE_NEMONA_TERRACE"));
        Assert.False(story.Has("FLAG_PALDEA_HIDE_CLAVELL_HOME"));
        // The first morning: Mom and the director
        Play("CaboPoco_PlayersHouse_1F", "OnEnter", "CaboPoco_PlayersHouse_1F");
        Assert.Equal(1, story.Var("VAR_PALDEA_STORY"));
        Assert.True(story.Has("FLAG_PALDEA_HIDE_CLAVELL_HOME"));
        Assert.False(story.Has("FLAG_PALDEA_HIDE_NEMONA_TERRACE"));
        Assert.Equal("paldeaTerrace", Next());
        // The three on the terrace, the Pokédex, and Nemona's first battle
        Play("cabo_poco", "TheThree");
        Assert.Equal(2, story.Var("VAR_PALDEA_STORY"));
        Assert.Single(party.Members);
        Assert.Contains(party.Members[0].Species.Name, Region.Starters);
        Assert.True(story.Has("FLAG_HAS_POKEDEX"));
        Assert.False(story.Has("FLAG_PALDEA_HIDE_NEMONA_LIGHTHOUSE"));
        Assert.Equal("paldeaBeach", Next());
        // The roar on the beach, and the two in the grotto: the second answer is the violet one, a Violet game
        Play("poco_path", "ARoar");
        Assert.Equal(3, story.Var("VAR_PALDEA_STORY"));
        Assert.False(story.Has("FLAG_PALDEA_HIDE_GROTTO_LEGENDS"));
        Assert.Equal("paldeaGrotto", Next());
        Play("InletGrotto", "TheTwo", "InletGrotto", 1);
        Assert.Equal(4, story.Var("VAR_PALDEA_STORY"));
        Assert.Equal((2, 2), (story.Var("VAR_PALDEA_VERSION"), story.Var(MountRules.MountVariable)));
        Assert.True(story.Has("FLAG_PALDEA_HIDE_GROTTO_LEGENDS"));
        Assert.Equal("paldeaLighthouse", Next());
        // Nemona's lesson at the lighthouse, Mesagoza's gate, the director in the entrance hall
        Play("poco_path", "NemonasLesson");
        Assert.Equal(5, story.Var("VAR_PALDEA_STORY"));
        Assert.Equal(5, bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.Equal("paldeaMesagoza", Next());
        Play("mesagoza", "NemonaAtTheGate");
        Assert.Equal(6, story.Var("VAR_PALDEA_STORY"));
        Assert.Equal("paldeaAcademy", Next());
        Play("Mesagoza_AcademyHall", "OnEnter", "Mesagoza_AcademyHall");
        Assert.Equal(7, story.Var("VAR_PALDEA_STORY"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Tera Orb")!));
        Assert.Equal("paldeaFree", Next());
        // A second Tera Orb is never given: the hall's welcome plays once, and a bag with one keeps its one
        Play("Mesagoza_AcademyHall", "OnEnter", "Mesagoza_AcademyHall");
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Tera Orb")!));
    }

    [Fact]
    public void TheFirstAnswerInTheGrottoIsAScarletGameWithKoraidon()
    {
        var story = new StoryState();
        StoryMigration.BeginNewGame(story, ScriptLibrary.Default);
        story.SetVar("VAR_PALDEA_STORY", 3);
        var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Map = Rooms.Value("InletGrotto"), Region = RegionDatabase.Paldea };
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("TheTwo", "InletGrotto")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Equal((1, MountRules.Koraidon), (story.Var("VAR_PALDEA_VERSION"), story.Var(MountRules.MountVariable)));
        Assert.Equal("Koraidon", MountRules.SpeciesOf(story.Var(MountRules.MountVariable)));
    }

    [Fact]
    public void EveryOpenSceneOfTheChapterPlayedAndSkippedLeavesTheSame()
    {
        // The scenes the open places' triggers start (SceneSkipTests' walk plays only what a grid map starts): each from
        // the story it waits at, played and then hurried as a player holding the button would, the same story, team,
        // bag and markers afterwards
        foreach (var (file, script, story) in new[]
        {
            ("cabo_poco", "TheThree", 1), ("poco_path", "ARoar", 2), ("poco_path", "NemonasLesson", 4), ("mesagoza", "NemonaAtTheGate", 5)
        })
        {
            var ends = new List<string>();
            foreach (bool hurry in new[] { false, true })
            {
                var host = new HeadlessScriptHost { Region = RegionDatabase.Paldea, OpenMap = Open.Value, OpenMapNamed = n => n == "Paldea" ? Open.Value : null };
                StoryMigration.BeginNewGame(host.Story, ScriptLibrary.Default);
                host.Story.SetVar("VAR_PALDEA_STORY", story);
                // (whoever a scene faces stands where the scene before left them)
                if (story >= 1) { host.Story.Unset("FLAG_PALDEA_HIDE_NEMONA_TERRACE"); host.Story.Unset("FLAG_PALDEA_HIDE_CLAVELL_TERRACE"); }
                if (story >= 2) host.Story.Unset("FLAG_PALDEA_HIDE_NEMONA_LIGHTHOUSE");
                if (story >= 5) host.Story.Unset("FLAG_PALDEA_HIDE_NEMONA_GATE");
                if (story >= 2) host.Party.Add(new Pokemon(PokemonDatabase.Get("Sprigatito")!, 5, new Random(0)));
                var runner = new ScriptRunner(ScriptLibrary.Default, host) { Impatient = hurry };
                runner.Start(ScriptLibrary.Default.Find(script, file)!, null);
                runner.RunToEnd();
                Assert.Empty(host.Problems);
                Assert.True(host.Story.Var("VAR_PALDEA_STORY") > story, $"{file}.{script} left the story where it was");
                ends.Add(System.Text.Json.JsonSerializer.Serialize(host.Story.Snapshot()) + "|" + string.Join(",", host.Party.Members.Select(p => p.Species.Name))
                    + "|" + host.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!) + "|" + string.Join(",", host.OnMarker.OrderBy(m => m.Key).Select(m => m.Key + "=" + m.Value)));
            }
            Assert.True(ends[0] == ends[1], $"{file}.{script} skipped leaves otherwise than played:\n{ends[0]}\n{ends[1]}");
        }
    }
}
