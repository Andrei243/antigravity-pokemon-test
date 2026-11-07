using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Alola drawn by hand (plan 23 · Z5, first part; Kalos's way, the user's decision of 2026-11-02): the region's record,
/// the world files the drawings of <c>tools/MapDraw/alola</c> make, Melemele's Route 1, Iki Town, the Mahalo Trail and
/// Hau'oli City's west end built from them, their rooms, where a new game there begins, the walk from the new home and
/// the first chapter played headless from Mom's word to Hau'oli City. Floors, not counts, wherever a later session draws
/// more.
/// </summary>
public class AlolaTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Alola)!.Start!));

    private static World Alola => LoadedWorld.Value;
    private static Region Region => RegionDatabase.Get(RegionDatabase.Alola)!;
    private static Map Overworld => BuiltMaps.Value["Melemele"];

    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    private static readonly string[] Areas = { "alola_route_1", "iki_town", "mahalo_trail", "hauoli_city" };

    [Fact]
    public void AlolaIsBuiltWithItsFileAStartAndMapsOfItsOwn()
    {
        Assert.True(Region.IsBuilt);
        Assert.Equal(7, Region.Generation);
        Assert.Equal(new[] { "Rowlet", "Litten", "Popplio" }, Region.Starters);
        Assert.Equal(new[] { "Melemele", "Akala", "UlaUla", "Poni" }, Region.BadgeNames);
        Assert.Equal("Prof. Kukui", Region.Professor!.Name);
        Assert.Equal("original-alola", Region.Dex);
        Assert.True(Region.HasStory);
        Assert.False(Region.HasSeasons);
        Assert.Equal(205, Region.Levels!.Map(2));
        Assert.Equal(224, Region.Levels!.Map(58));
        foreach (string name in Region.Maps)
        {
            Assert.Equal(1, RegionDatabase.All.Count(r => r.Maps.Contains(name)));
            Assert.NotNull(MapNamed(name));
        }
        // The way from Kalos leaves from Lumiose City's Pokémon Center and lands here now that Alola is built
        var link = RegionDatabase.LinkFrom(RegionDatabase.Kalos)!;
        Assert.Equal("LumioseCity_PokemonCenter_1F", link.DepartureMap);
        Assert.Contains(MapDatabase.Fresh()(link.DepartureMap!).Everyone, n => n.IsTransportAttendant);
        var story = new StoryState();
        story.CompleteRegion(RegionDatabase.Kalos);
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(link, story));
    }

    [Fact]
    public void TheWorldFilesAreWhatTheDrawingsGive()
    {
        string root = MapDraw.Repository.Root();
        var files = MapDraw.Repository.Files(Path.Combine(root, "tools", "MapDraw", "alola"), Path.Combine(root, "PokemonPlatinumEngine", "Data", "world", "alola"));
        foreach (var (path, text) in files)
        {
            Assert.True(File.Exists(path), $"{path} is missing: run `dotnet run --project tools/MapDraw -- alola`");
            Assert.True(File.ReadAllText(path).Replace("\r\n", "\n") == text, $"{path} is not what the drawings give: run `dotnet run --project tools/MapDraw -- alola`");
        }
        Assert.Empty(MapDraw.Repository.Stale(Path.Combine(root, "PokemonPlatinumEngine", "Data", "world", "alola"), files));
    }

    [Fact]
    public void MelemelesFirstPlacesAreOpenWithTheirSongsHousesAndOwnKeys()
    {
        Assert.Equal(Areas, Alola.Index.Areas.Take(Areas.Length));
        foreach (string key in Areas)
        {
            Assert.NotNull(Alola.Area(key));
            Assert.False(string.IsNullOrEmpty(Alola.Overlay(key)?.BgmTrack), $"{key} has no song");
        }
        var others = World.LoadAll().Where(w => w.Index.Region != "Alola").SelectMany(w => w.Index.Areas).ToHashSet();
        Assert.DoesNotContain(Alola.Index.Areas, others.Contains);
        Assert.Equal(Architecture.Islander, Alola.Overlay("iki_town")!.Architecture);
        Assert.Equal(Architecture.Shoreline, Alola.Overlay("hauoli_city")!.Architecture);
        Assert.True(Overworld.PlacedBuildings.Count >= 6, "the houses are found at their doors");
        // Route 1 has its wild Pokémon in the grass
        Assert.NotNull(Alola.Area("alola_route_1")!.Land);
    }

    [Fact]
    public void ANewGameInAlolaBeginsInTheNewHomeAndAChampionArrivesBeforeTheDoor()
    {
        var start = Region.Start!;
        Assert.Equal("AlolaRoute1_PlayersHouse", start.Map);
        var home = MapNamed(start.Map);
        Assert.True(home.IsIndoors);
        Assert.True(home.IsWalkable(start.X, start.Y));
        Assert.Contains(home.Everyone, n => n.Name == "Mom" && n.IsHealingNurse);

        var arrival = Region.ArrivalSpot!;
        Assert.Equal("Melemele", arrival.Map);
        Assert.True(Overworld.IsWalkable(arrival.X, arrival.Y));
        Assert.Equal("AlolaRoute1_PlayersHouse", Overworld.GetWarpAt(arrival.X, arrival.Y - 1)?.TargetMap);
        foreach (var spawn in Region.File!.Spawns)
        {
            var map = MapNamed(spawn.Map ?? "Melemele");
            Assert.True(map.IsWalkable(spawn.X, spawn.Y), $"spawn {spawn.Id} stands on a blocked tile");
            Assert.Equal(spawn.Room, map.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
        }
    }

    [Fact]
    public void FromTheNewHomeEveryRoomIkiTownTheTrailAndHauoliAreReachedOnFoot()
    {
        var walked = Walked.Value;
        foreach (string key in Areas)
            Assert.True(walked.Any(p => p.Key == Overworld && p.Value.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key)), $"{key} is never reached");
        foreach (string room in Region.Maps.Where(m => m != "Melemele"))
            Assert.True(walked.Keys.Any(m => m.Name == room), $"{room} is never reached");
    }

    [Fact]
    public void TheFirstChapterRunsFromMomsWordToHauoliCityHeadless()
    {
        var story = new StoryState();
        var party = new Party();
        var bag = new Inventory();
        var library = ScriptLibrary.Default;
        void Play(string file, string script, Map map)
        {
            var host = new HeadlessScriptHost(story, party, bag) { Map = map, Region = RegionDatabase.Alola };
            var runner = new ScriptRunner(library, host);
            runner.Start(library.Find(script, file)!, null);
            runner.RunToEnd();
        }
        string? Next() => Objectives.Current(story, RegionDatabase.Alola)?.Name;

        Play("common", "AlolaNewGame", MapNamed("AlolaRoute1_PlayersHouse"));
        Assert.Equal("alolaMom", Next());
        Play("AlolaRoute1_PlayersHouse", "MomCalls", MapNamed("AlolaRoute1_PlayersHouse"));
        Assert.Equal(1, story.Var("VAR_ALOLA_STORY"));
        Assert.Equal("alolaDoor", Next());
        story.SetVar("VAR_ALOLA_STORY", 2);   // the door's scene asks where the player stands, which a headless host doesn't
        Assert.Equal("alolaIkiTown", Next());
        Play("iki_town", "KahunaAway", Overworld);
        Assert.Equal(3, story.Var("VAR_ALOLA_STORY"));
        Assert.False(story.Has("FLAG_ALOLA_HIDE_LILLIE_TRAIL"));
        Play("mahalo_trail", "Bridge", Overworld);
        Assert.Equal(4, story.Var("VAR_ALOLA_STORY"));
        Assert.Equal("alolaKahuna", Next());
        Play("iki_town", "KahunaHome", Overworld);
        Assert.Equal(5, story.Var("VAR_ALOLA_STORY"));
        Assert.Single(party.Members);
        Assert.Contains(party.Members[0].Species.Name, Region.Starters);
        Assert.False(story.Has("FLAG_ALOLA_HIDE_KUKUI_LAB"));
        Play("AlolaRoute1_KukuiLab", "Kukui", MapNamed("AlolaRoute1_KukuiLab"));
        Assert.Equal(6, story.Var("VAR_ALOLA_STORY"));
        Assert.True(story.Has("FLAG_HAS_POKEDEX"));
        Assert.Equal(5, bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.Equal("alolaHauoli", Next());
        Play("hauoli_city", "OnEnter", Overworld);
        Assert.Equal(7, story.Var("VAR_ALOLA_STORY"));
        Assert.Equal(699, Objectives.Find("alolaFree")!.Id);
    }
}
