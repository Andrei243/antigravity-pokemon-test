using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Hisui, a side region off Sinnoh (plan 23 · Z17; decision 3, the user's word of 2026-11-07 "build it"): its record and
/// its branch off the chain, the world files the drawings of <c>tools/MapDraw/hisui</c> make, Jubilife Village and
/// Prelude Beach built from them, their rooms, where a new game there begins and a Champion lands, the way in through
/// Celestic Town's ruins and back by the stone on the beach, and the first chapter played headless from the professor on
/// the beach to a night in the quarters. Floors, not counts, wherever a later session draws more.
/// </summary>
public class HisuiTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Hisui"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Hisui)!.Start!));

    private static World Hisui => LoadedWorld.Value;
    private static Region Region => RegionDatabase.Get(RegionDatabase.Hisui)!;
    private static Map Overworld => BuiltMaps.Value["JubilifeVillage"];

    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    private static readonly string[] Areas = { "jubilife_village", "prelude_beach" };

    [Fact]
    public void HisuiIsASideRegionBuiltWithItsFileAStartAndMapsOfItsOwn()
    {
        Assert.True(Region.IsBuilt);
        Assert.True(Region.Side);
        Assert.Equal(8, Region.Generation);
        Assert.Equal(new[] { "Rowlet", "Cyndaquil", "Oshawott" }, Region.Starters);
        Assert.Empty(Region.BadgeNames);
        Assert.Equal("Prof. Laventon", Region.Professor!.Name);
        Assert.Equal("hisui", Region.Dex);
        Assert.Equal(242, SpeciesSources.DexOf(RegionDatabase.Hisui).Count);
        Assert.True(Region.HasStory);
        Assert.Equal("JubilifeVillage", Region.OverworldMap);
        Assert.Equal(62, Region.Levels!.Map(2));
        Assert.Null(LevelCaps.TableOf(RegionDatabase.Hisui));
        foreach (string name in Region.Maps)
        {
            Assert.Equal(1, RegionDatabase.All.Count(r => r.Maps.Contains(name)));
            Assert.NotNull(MapNamed(name));
        }
    }

    [Fact]
    public void TheBranchLeavesSinnohAfterItsHallOfFameAndIsNoStepOfTheJourney()
    {
        var branch = Assert.Single(RegionDatabase.SideLinksFrom(RegionDatabase.Sinnoh));
        Assert.Equal(LinkKind.Side, branch.Kind);
        Assert.Equal(RegionDatabase.Hisui, branch.To);
        Assert.Same(branch, RegionDatabase.SideLinkInto(RegionDatabase.Hisui));
        Assert.Equal("CelesticTownCave", branch.DepartureMap);
        Assert.Contains(branch.DepartureMap, RegionDatabase.Get(RegionDatabase.Sinnoh)!.Maps);
        // The chain goes on from Sinnoh to Kanto as before, and nothing goes on from Hisui
        Assert.Equal(RegionDatabase.Kanto, RegionDatabase.LinkFrom(RegionDatabase.Sinnoh)!.To);
        Assert.Null(RegionDatabase.LinkFrom(RegionDatabase.Hisui));
        Assert.Empty(RegionDatabase.SideLinksFrom(RegionDatabase.Hisui));

        var story = new StoryState();
        Assert.Equal(TravelCheck.StoryUnfinished, RegionDatabase.CheckTravel(branch, story));
        story.CompleteRegion(RegionDatabase.Sinnoh);
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(branch, story));

        // No harbour sails to it, even once entered; Fly neither lands there nor leaves it for another region
        story.EnterRegion(RegionDatabase.Hisui);
        story.EnterRegion(RegionDatabase.Kanto);
        foreach (var region in RegionDatabase.All.Where(r => r.IsBuilt))
            Assert.DoesNotContain(RegionDatabase.LinksFrom(region, story), w => w.To == RegionDatabase.Hisui);
        var sinnoh = RegionDatabase.Get(RegionDatabase.Sinnoh)!;
        Assert.False(Crossing.CanFly(sinnoh, Region, story, new[] { "hisui", "extended-sinnoh" }));
        Assert.False(Crossing.CanFly(Region, sinnoh, story, new[] { "hisui", "extended-sinnoh" }));
        // Its Hall of Fame moves no other region's rematches (it is off the chain)
        story.CompleteRegion(RegionDatabase.Hisui);
        Assert.Equal(RegionDatabase.Sinnoh, RematchLevels.Furthest(story)!.Id);
    }

    [Fact]
    public void TheWorldFilesAreWhatTheDrawingsGive()
    {
        string root = MapDraw.Repository.Root();
        var files = MapDraw.Repository.Files(Path.Combine(root, "tools", "MapDraw", "hisui"), Path.Combine(root, "PokemonPlatinumEngine", "Data", "world", "hisui"));
        foreach (var (path, text) in files)
        {
            Assert.True(File.Exists(path), $"{path} is missing: run `dotnet run --project tools/MapDraw -- hisui`");
            Assert.True(File.ReadAllText(path).Replace("\r\n", "\n") == text, $"{path} is not what the drawings give: run `dotnet run --project tools/MapDraw -- hisui`");
        }
        Assert.Empty(MapDraw.Repository.Stale(Path.Combine(root, "PokemonPlatinumEngine", "Data", "world", "hisui"), files));
    }

    [Fact]
    public void TheVillageAndTheBeachAreOpenWithTheirSongsHousesAndOwnKeys()
    {
        Assert.Equal(Areas, Hisui.Index.Areas.Take(Areas.Length));
        foreach (string key in Areas)
        {
            Assert.NotNull(Hisui.Area(key));
            Assert.False(string.IsNullOrEmpty(Hisui.Overlay(key)?.BgmTrack), $"{key} has no song");
            Assert.Equal(Architecture.Hisuian, Hisui.Overlay(key)!.Architecture);
        }
        var others = World.LoadAll().Where(w => w.Index.Region != "Hisui").SelectMany(w => w.Index.Areas).ToHashSet();
        Assert.DoesNotContain(Hisui.Index.Areas, others.Contains);
        Assert.True(Overworld.PlacedBuildings.Count >= 7, "the houses are found at their doors");
        // The pastures' keeper opens the boxes, and Galaxy Hall's medic heals
        Assert.Contains(Overworld.Everyone, n => n.Script == "Pastures");
        Assert.Contains(MapNamed("JubilifeVillage_GalaxyHall").Everyone, n => n.IsHealingNurse);
        Assert.Contains(MapNamed("JubilifeVillage_GeneralStore").Everyone, n => n.IsPokeMartClerk);
    }

    [Fact]
    public void ANewGameInHisuiBeginsOnPreludeBeachAndAReturnArrivesBeforeGalaxyHall()
    {
        var start = Region.Start!;
        Assert.Equal("JubilifeVillage", start.Map);
        Assert.Equal("prelude_beach", Overworld.AreaAt(start.X, start.Y)?.Key);
        Assert.True(Overworld.IsWalkable(start.X, start.Y));
        // The stone the player wakes beside is read from the tile north of it, the way back
        Assert.False(Overworld.IsWalkable(start.X, start.Y - 1));

        var arrival = Region.ArrivalSpot!;
        Assert.Equal("JubilifeVillage", arrival.Map);
        Assert.True(Overworld.IsWalkable(arrival.X, arrival.Y));
        Assert.Equal("JubilifeVillage_GalaxyHall", Overworld.GetWarpAt(arrival.X, arrival.Y - 1)?.TargetMap);
        foreach (var spawn in Region.File!.Spawns)
        {
            Assert.True(Overworld.IsWalkable(spawn.X, spawn.Y), $"spawn {spawn.Id} stands on a blocked tile");
            Assert.Equal(spawn.Room, Overworld.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
        }
    }

    [Fact]
    public void FromTheBeachEveryRoomAndTheVillageAreReachedOnFoot()
    {
        var walked = Walked.Value;
        foreach (string key in Areas)
            Assert.True(walked.Any(p => p.Key == Overworld && p.Value.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key)), $"{key} is never reached");
        foreach (string room in Region.Maps.Where(m => m != "JubilifeVillage"))
            Assert.True(walked.Keys.Any(m => m.Name == room), $"{room} is never reached");
    }

    [Fact]
    public void AChampionCrossesThroughTheRuinsPaintingAndComesBackByTheStone()
    {
        var story = new StoryState();
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Torterra")!, 62));
        story.Set("FLAG_EXAMINED_CELESTIC_TOWN_CAVE_PAINTING");
        var library = ScriptLibrary.Default;

        // Before Sinnoh's Hall of Fame the painting is only a painting
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Region = RegionDatabase.Sinnoh, BeganInRegion = RegionDatabase.Sinnoh };
        Run(library, host, "celestic_town_cave", "Painting");
        Assert.Empty(host.Crossings);

        // After it, the light at its heart takes the whole team to Prelude Beach, with Hisui's story begun
        story.CompleteRegion(RegionDatabase.Sinnoh);
        Run(library, host, "celestic_town_cave", "Painting");
        Assert.Equal(new[] { RegionDatabase.Hisui }, host.Crossings);
        Assert.Equal(RegionDatabase.Hisui, host.Region);
        Assert.Equal((48, 77), host.PlayerTile);
        Assert.True(story.Has("FLAG_HISUI_STORY_BEGUN"));
        Assert.True(story.Has("FLAG_HISUI_HIDE_BEACH_ROWLET"));
        Assert.True(story.HasEntered(RegionDatabase.Hisui));
        Assert.Single(party.Members);

        // The stone on the beach brings a Champion back to the painting
        Run(library, host, "prelude_beach", "Stone");
        Assert.Equal(new[] { RegionDatabase.Hisui, RegionDatabase.Sinnoh }, host.Crossings);
        Assert.Equal((9, 3), host.PlayerTile);

        // Once the story there has reached the village, the painting lands the player before Galaxy Hall
        story.SetVar("VAR_HISUI_STORY", 3);
        Run(library, host, "celestic_town_cave", "Painting");
        Assert.Equal(Region.ArrivalSpot!.X, host.PlayerTile.X);
        Assert.Equal(Region.ArrivalSpot!.Y, host.PlayerTile.Y);
    }

    [Fact]
    public void AGameBegunInHisuiFindsNoWayOutByTheStone()
    {
        var story = new StoryState();
        var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Region = RegionDatabase.Hisui };
        Run(ScriptLibrary.Default, host, "prelude_beach", "Stone");
        Assert.Empty(host.Crossings);
        Assert.Empty(host.Asked);
    }

    [Fact]
    public void TheFirstChapterRunsFromTheBeachToANightInTheQuartersHeadless()
    {
        var story = new StoryState();
        var party = new Party();
        var bag = new Inventory();
        var library = ScriptLibrary.Default;
        void Play(string file, string script, Map map)
        {
            var host = new HeadlessScriptHost(story, party, bag) { Map = map, Region = RegionDatabase.Hisui };
            Run(library, host, file, script);
        }
        string? Next() => Objectives.Current(story, RegionDatabase.Hisui)?.Name;

        Play("common", "HisuiNewGame", Overworld);
        Assert.Equal("hisuiLaventon", Next());
        Assert.False(story.Has("FLAG_HISUI_HIDE_LAVENTON_BEACH"));
        Play("prelude_beach", "Laventon", Overworld);
        Assert.Equal(1, story.Var("VAR_HISUI_STORY"));
        Assert.False(story.Has("FLAG_HISUI_HIDE_BEACH_ROWLET"));
        Assert.Equal("hisuiGather", Next());
        Play("prelude_beach", "Rowlet", Overworld);
        Play("prelude_beach", "Cyndaquil", Overworld);
        Assert.Equal(1, story.Var("VAR_HISUI_STORY"));
        Play("prelude_beach", "Oshawott", Overworld);
        Assert.Equal(2, story.Var("VAR_HISUI_STORY"));
        Assert.True(story.Has("FLAG_HISUI_HIDE_BEACH_OSHAWOTT"));
        Assert.Equal("hisuiPartner", Next());
        Play("prelude_beach", "Laventon", Overworld);
        Assert.Equal(3, story.Var("VAR_HISUI_STORY"));
        Assert.Single(party.Members);
        Assert.Contains(party.Members[0].Species.Name, Region.Starters);
        Assert.Equal(5, bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.True(story.Has("FLAG_HISUI_HIDE_LAVENTON_BEACH"));
        Assert.False(story.Has("FLAG_HISUI_HIDE_LAVENTON_VILLAGE"));
        Assert.Equal("hisuiVillage", Next());
        Play("jubilife_village", "Welcome", Overworld);
        Assert.Equal(4, story.Var("VAR_HISUI_STORY"));
        Assert.Equal("hisuiWallflower", Next());
        Play("JubilifeVillage_Wallflower", "OnEnter", MapNamed("JubilifeVillage_Wallflower"));
        Assert.Equal(5, story.Var("VAR_HISUI_STORY"));
        Assert.False(story.Has("FLAG_HISUI_HIDE_CYLLENE_HALL"));
        Assert.True(story.Has("FLAG_HISUI_HIDE_CYLLENE_WALLFLOWER"));
        Assert.Equal("hisuiHall", Next());
        Play("JubilifeVillage_GalaxyHall", "OnEnter", MapNamed("JubilifeVillage_GalaxyHall"));
        Assert.Equal(6, story.Var("VAR_HISUI_STORY"));
        Assert.True(story.Has("FLAG_HAS_POKEDEX"));
        Assert.Equal("hisuiQuarters", Next());
        Play("JubilifeVillage_Quarters", "OnEnter", MapNamed("JubilifeVillage_Quarters"));
        Assert.Equal(7, story.Var("VAR_HISUI_STORY"));
        Assert.Equal("hisuiTrial", Next());
        Assert.Equal(999, Objectives.Find("hisuiTrial")!.Id);
    }

    private static void Run(ScriptLibrary library, HeadlessScriptHost host, string file, string script)
    {
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find(script, file)!, null);
        runner.RunToEnd();
    }
}
