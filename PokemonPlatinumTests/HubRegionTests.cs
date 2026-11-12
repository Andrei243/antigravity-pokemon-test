using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 41 · EG1: the hub, a small region of our own off the chain. Its record and world files, opened by the second Hall
/// of Fame, offered last by every finished region's harbour, landed on at its arrival the first time and in its harbour
/// hall after, its own harbour sailing to every region entered, Fly's last tab, the rules it borrows from the furthest
/// finished region (<see cref="GameEngine.RulesOf"/>), the facility guide and the guides back, and everything that walks
/// the chain stepping round it.
/// </summary>
[Collection("Shared clocks and dice")]
public class HubRegionTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hub));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    private static Region Hub => RegionDatabase.Get(RegionDatabase.Hub)!;
    private static Region Sinnoh => RegionDatabase.Get(RegionDatabase.Sinnoh)!;
    private static Region Kanto => RegionDatabase.Get(RegionDatabase.Kanto)!;
    private static Map Town => BuiltMaps.Value["Hub"];

    private static Pokemon Mon(string species, int level = 60) => new(PokemonDatabase.Get(species)!, level);

    /// <summary>A Champion of Sinnoh and Kanto, both entered: the hub open.</summary>
    private static StoryState TwoChampionships()
    {
        var story = new StoryState();
        foreach (var id in new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto })
        {
            story.EnterRegion(id);
            story.CompleteRegion(id);
        }
        return story;
    }

    [Fact]
    public void TheHubIsASideRegionWithNoStoryBadgesPokedexOrLevelsOfItsOwn()
    {
        Assert.True(Hub.IsBuilt);
        Assert.True(Hub.Side);
        Assert.True(Hub.IsHub);
        Assert.True(Hub.BorrowsRules);
        Assert.False(Hub.HasStory);
        Assert.Empty(Hub.BadgeNames);
        Assert.Empty(Hub.Dexes);
        Assert.Null(Hub.Levels);
        Assert.Null(LevelCaps.TableOf(RegionDatabase.Hub));
        Assert.Equal("Hub", Hub.OverworldMap);
        // Hisui, the branch, keeps its own rules; no region of the chain borrows any
        Assert.False(RegionDatabase.Get(RegionDatabase.Hisui)!.BorrowsRules);
        Assert.False(RegionDatabase.Get(RegionDatabase.Hisui)!.IsHub);
        Assert.All(RegionDatabase.All.Where(r => !r.Side), r => Assert.False(r.BorrowsRules));
        foreach (string name in Hub.Maps)
        {
            Assert.Equal(1, RegionDatabase.All.Count(r => r.Maps.Contains(name)));
            Assert.NotNull(MapNamed(name));
        }
        // It has no wild Pokémon: an empty habitats file
        Assert.Empty(GameDataFiles.Load<WorldHabitatsFile>("world/hub/habitats.json").Areas);
        Assert.True(LoadedWorld.Value.Area("hub_town")!.Land is null or { Count: 0 });
    }

    [Fact]
    public void TheWorldFilesAreWhatTheDrawingsGive()
    {
        string root = MapDraw.Repository.Root();
        var files = MapDraw.Repository.Files(Path.Combine(root, "tools", "MapDraw", "hub"), Path.Combine(root, "PokemonPlatinumEngine", "Data", "world", "hub"));
        foreach (var (path, text) in files)
        {
            Assert.True(File.Exists(path), $"{path} is missing: run `dotnet run --project tools/MapDraw -- hub`");
            Assert.True(File.ReadAllText(path).Replace("\r\n", "\n") == text, $"{path} is not what the drawings give: run `dotnet run --project tools/MapDraw -- hub`");
        }
        Assert.Empty(MapDraw.Repository.Stale(Path.Combine(root, "PokemonPlatinumEngine", "Data", "world", "hub"), files));
    }

    [Fact]
    public void TheTownHasItsSongItsWayOfBuildingAndItsDoorsTheLaterHallsLocked()
    {
        var overlay = LoadedWorld.Value.Overlay("hub_town")!;
        Assert.Equal("hub/town", overlay.BgmTrack);
        Assert.True(PokemonPlatinumEngine.Audio.MusicLibrary.Exists("hub/town"));
        Assert.Equal(Architecture.Crossroads, overlay.Architecture);
        Assert.True(Town.PlacedBuildings.Count >= 6, "the halls are found at their doors");
        // Every door of the town is open: the Pokémon Center, the harbour hall, Pennant Hall (EG3), the Laurel Gallery
        // (EG2), the Open Door Agency (EG7) and the Laurel Court (EG4)
        foreach (var room in new[] { "Hub_PokemonCenter", "Hub_HarbourHall", "Hub_Plaza", "Hub_HallOfChampions", "Hub_Office", "Hub_Arena" })
            Assert.Contains(Town.Warps, w => w.TargetMap == room && !w.Shut);
        foreach (var shut in Array.Empty<string>())
            Assert.DoesNotContain(Town.Warps, w => w.TargetMap == shut && !w.Shut);
        Assert.Contains(MapNamed("Hub_PokemonCenter").Everyone, n => n.IsHealingNurse);
        Assert.Contains(MapNamed("Hub_PokemonCenter2F").Everyone, n => n.Script == "GlobalLink");
        Assert.Contains(MapNamed("Hub_HarbourHall").Everyone, n => n.IsTransportAttendant);
    }

    [Fact]
    public void ItOpensWithTheSecondHallOfFameOfTheChain()
    {
        var story = new StoryState();
        Assert.False(RegionDatabase.HubOpen(story));
        story.CompleteRegion(RegionDatabase.Sinnoh);
        Assert.False(RegionDatabase.HubOpen(story));
        // Hisui's Hall of Fame is off the chain and counts for nothing
        story.CompleteRegion(RegionDatabase.Hisui);
        Assert.False(RegionDatabase.HubOpen(story));
        story.CompleteRegion(RegionDatabase.Kanto);
        Assert.True(RegionDatabase.HubOpen(story));
    }

    [Fact]
    public void EveryFinishedHarbourOffersItLastOnceOpenAndNotBefore()
    {
        var story = new StoryState();
        story.EnterRegion(RegionDatabase.Sinnoh);
        story.CompleteRegion(RegionDatabase.Sinnoh);
        Assert.DoesNotContain(RegionDatabase.LinksFrom(Sinnoh, story), w => w.To == RegionDatabase.Hub);

        story = TwoChampionships();
        foreach (var region in new[] { Sinnoh, Kanto })
        {
            var ways = RegionDatabase.LinksFrom(region, story);
            Assert.Equal(RegionDatabase.Hub, ways[^1].To);
            Assert.Equal(RegionDatabase.LinkFrom(region.Id)!.VesselWord, ways[^1].VesselWord);
            Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(ways[^1], story));
        }
        // An unfinished region's harbour offers nothing, the hub included
        story.EnterRegion(RegionDatabase.Johto);
        Assert.Empty(RegionDatabase.LinksFrom(RegionDatabase.Get(RegionDatabase.Johto)!, story));
        // And no harbour ever sails to the branch
        Assert.DoesNotContain(RegionDatabase.LinksFrom(Sinnoh, story), w => w.To == RegionDatabase.Hisui);
    }

    [Fact]
    public void ItsHarbourSailsToEveryRegionEnteredAndItIsLandedOnItsArrivalFirst()
    {
        var story = TwoChampionships();
        story.EnterRegion(RegionDatabase.Johto);
        story.EnterRegion(RegionDatabase.Hisui);
        var ways = RegionDatabase.LinksFrom(Hub, story);
        Assert.Equal(new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto }, ways.Select(w => w.To));
        Assert.All(ways, w => Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(w, story)));
        Assert.Same(RegionDatabase.HarbourLinkOf(RegionDatabase.Hub), RegionDatabase.Links.Single(l => l.Kind == LinkKind.Hub));
        // Nothing of the chain goes on from it, and it is no branch
        Assert.Null(RegionDatabase.LinkFrom(RegionDatabase.Hub));
        Assert.Empty(RegionDatabase.SideLinksFrom(RegionDatabase.Hub));

        Assert.Equal(Hub.ArrivalSpot, RegionDatabase.LandingOf(Hub, story));
        story.EnterRegion(RegionDatabase.Hub);
        Assert.Equal(Hub.Harbour, RegionDatabase.LandingOf(Hub, story));
    }

    [Fact]
    public void TheArrivalTheHarbourAndTheSpawnAreWalkableBesideWhatTheyAreFor()
    {
        var arrival = Hub.ArrivalSpot!;
        Assert.Equal("Hub", arrival.Map);
        Assert.True(Town.IsWalkable(arrival.X, arrival.Y));
        Assert.Equal("hub_town", Town.AreaAt(arrival.X, arrival.Y)?.Key);

        var harbour = Hub.Harbour!;
        var hall = MapNamed(harbour.Map);
        Assert.True(hall.IsWalkable(harbour.X, harbour.Y));
        Assert.Null(hall.GetWarpAt(harbour.X, harbour.Y));
        Assert.Contains(hall.Everyone, n => n.IsTransportAttendant && n.GridX == harbour.X && n.GridY == harbour.Y - 1);
        Assert.Equal(RegionDatabase.HarbourLinkOf(RegionDatabase.Hub)!.DepartureMap, harbour.Map);

        foreach (var spawn in Hub.File!.Spawns)
        {
            Assert.True(Town.IsWalkable(spawn.X, spawn.Y), $"spawn {spawn.Id} stands on a blocked tile");
            Assert.Equal(spawn.Room, Town.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
        }
    }

    [Fact]
    public void FlyListsItLastOnceEnteredAndLeavesItForEveryRegionEntered()
    {
        var story = TwoChampionships();
        var dexes = new List<string> { Sinnoh.Dex!, Kanto.Dex! };
        Assert.DoesNotContain(Crossing.FlyRegions(Kanto, story, dexes), r => r.IsHub);
        story.EnterRegion(RegionDatabase.Hub);
        Assert.Equal(new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Hub }, Crossing.FlyRegions(Kanto, story, dexes).Select(r => r.Id));
        Assert.True(Crossing.CanFly(Hub, Sinnoh, story, dexes));
        Assert.Equal(new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Hub }, Crossing.FlyRegions(Hub, story, dexes).Select(r => r.Id));
        // The branch stays out of Fly whatever is entered
        story.EnterRegion(RegionDatabase.Hisui);
        Assert.DoesNotContain(Crossing.FlyRegions(Hub, story, dexes), r => r.Id == RegionDatabase.Hisui);
        Assert.NotNull(RegionMap.Of(RegionDatabase.Hub));
    }

    [Fact]
    public void TheRulesUnderfootAreTheFurthestFinishedRegionsAtTheHubAndTheRegionsOwnElsewhere()
    {
        var story = TwoChampionships();
        story.SetBadges(RegionDatabase.Kanto, 0xFF);
        story.SetBadges(RegionDatabase.Sinnoh, 0x01);
        Assert.Same(Kanto, GameEngine.RulesOf(Hub, story));
        Assert.Same(Sinnoh, GameEngine.RulesOf(Sinnoh, story));
        Assert.Same(Kanto, GameEngine.RulesOf(Kanto, story));
        var hisui = RegionDatabase.Get(RegionDatabase.Hisui)!;
        Assert.Same(hisui, GameEngine.RulesOf(hisui, story));

        // A traded Pokémon at 150 obeys at the hub as it does in Kanto, with Kanto's eight Badges
        int badges = story.BadgeCount(GameEngine.RulesOf(Hub, story).Id);
        Assert.Equal(8, badges);
        Assert.Null(Obedience.LevelCap(badges));
        Assert.Equal(0, story.BadgeCount(RegionDatabase.Hub));
        // The Mart's counter stocks by those Badges, not by none
        Assert.True(MartDatabase.Stock(null, badges).Count > MartDatabase.Stock(null, 0).Count);
        // The cap's ceiling is Kanto's, where the hub has none of its own
        Assert.Null(LevelCaps.Ceiling(RegionDatabase.Hub, story));
        Assert.Equal(LevelCaps.Ceiling(RegionDatabase.Kanto, story), LevelCaps.Ceiling(GameEngine.RulesOf(Hub, story).Id, story));
    }

    [Fact]
    public void APacksRegionJoinsAfterTheLastRegionOfTheChainNotAfterTheHub()
    {
        var sample = Path.Combine(Repository.Root, "samples", "packs", "tidemark-isle");
        var chain = RegionDatabase.With(new[] { new DataLayer(sample, DataLayers.ReadManifest(sample)) });
        var ids = chain.Regions.Select(r => r.Id).ToList();
        Assert.True(ids.IndexOf("Tidemark") < ids.IndexOf(RegionDatabase.Hub));
        Assert.Equal(RegionDatabase.Galar, ids[ids.IndexOf("Tidemark") - 1]);
    }

    [Fact]
    public void NoNewGameBeginsAtTheHubAndNoSplitWaitsForIt()
    {
        Assert.NotEmpty(CommandLine.Parse(new[] { "--region", RegionDatabase.Hub }).Errors);
        Assert.Empty(CommandLine.Parse(new[] { "--region", RegionDatabase.Hisui }).Errors);
        Assert.DoesNotContain(RunTimer.Points, p => p.Region == RegionDatabase.Hub);
    }

    [Fact]
    public void FromItsArrivalEveryMapOfTheHubIsReachedOnFoot()
    {
        var walked = WorldWalk.From(MapNamed, Hub.Start!);
        foreach (string map in Hub.Maps)
            Assert.True(walked.Keys.Any(m => m.Name == map), $"{map} is never reached");
    }

    [Fact]
    public void AChampionOfSinnohAndKantoSailsFromCanalaveToTheHubAndBackAndFliesThereFromKanto()
    {
        var sinnoh = MapDatabase.Get("Sinnoh");
        var captain = sinnoh.Everyone.Single(n => n.IsTransportAttendant);
        var host = new HeadlessScriptHost { Map = sinnoh, PlayerTile = (44, 751), MapNamed = MapDatabase.Get };
        foreach (var species in new[] { "Infernape", "Staraptor", "Luxray" }) host.Party.Add(Mon(species));
        host.Party.Add(Mon("Dragonite", 150));
        foreach (var id in new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto })
        {
            host.Story.EnterRegion(id);
            host.Story.CompleteRegion(id);
        }
        var team = host.Party.Members.ToList();

        // Canalave's captain asks where: Kanto, Wayhaven, STAY. Wayhaven's first landing is its quay
        host.Answers.Enqueue(1);
        Play(host, ScriptLibrary.Default.Find(FieldScripts.Attendant)!, captain);
        Assert.Equal("Wayhaven", host.Asked.Last().Answer);
        Assert.Equal(new[] { RegionDatabase.Hub }, host.Crossings);
        Assert.Equal(Hub.ArrivalSpot!.Map, host.Map!.Name);
        Assert.Equal((Hub.ArrivalSpot.X, Hub.ArrivalSpot.Y), host.PlayerTile);
        Assert.Equal(team, host.Party.Members);
        Assert.True(host.Story.HasEntered(RegionDatabase.Hub));

        // The harbourmaster sails back to Sinnoh, landing on Canalave's pier
        var hall = MapDatabase.Get("Hub_HarbourHall");
        host.Map = hall;
        host.Answers.Enqueue(0);
        Play(host, ScriptLibrary.Default.Find(FieldScripts.Attendant)!, hall.Everyone.Single(n => n.IsTransportAttendant));
        Assert.Equal(new[] { RegionDatabase.Hub, RegionDatabase.Sinnoh }, host.Crossings);
        Assert.Equal("Sinnoh", host.Map!.Name);
        Assert.Equal((44, 751), host.PlayerTile);

        // A second sailing lands in the harbour hall, beside the harbourmaster
        host.Answers.Enqueue(1);
        Play(host, ScriptLibrary.Default.Find(FieldScripts.Attendant)!, captain);
        Assert.Equal(Hub.Harbour!.Map, host.Map!.Name);
        Assert.Equal((Hub.Harbour.X, Hub.Harbour.Y), host.PlayerTile);

        // Fly from Kanto lands before the hub's Pokémon Center
        host.Map = MapDatabase.Get("Kanto");
        host.Region = RegionDatabase.Kanto;
        var spawn = SpawnLocations.In(RegionDatabase.Hub).Single();
        host.FlyTo = spawn;
        Assert.True(host.Fly());
        Assert.Equal(RegionDatabase.Hub, host.Crossings[^1]);
        Assert.Equal((spawn.X, spawn.Y), host.PlayerTile);
        Assert.Equal(team, host.Party.Members);
    }

    [Fact]
    public void TheFacilityGuideCrossesAChampionToEachDoorAndAGuideThereCrossesBack()
    {
        var hall = MapDatabase.Get("Hub_HarbourHall");
        var guide = hall.Everyone.Single(n => n.Script == "FacilityGuide");
        var targets = new[] { ("BattleFrontier", RegionDatabase.Sinnoh), ("HoennBattleFrontier", RegionDatabase.Hoenn), ("GearStation", RegionDatabase.Unova) };
        for (int i = 0; i < targets.Length; i++)
        {
            var (map, region) = targets[i];
            var host = new HeadlessScriptHost { Map = hall, PlayerTile = (9, 4), MapNamed = MapDatabase.Get, Region = RegionDatabase.Hub };
            host.Party.Add(Mon("Garchomp"));
            host.Story.EnterRegion(RegionDatabase.Hub);

            // Not a Champion of that region: no crossing
            host.Answers.Enqueue(i);
            Play(host, ScriptLibrary.Default.Find("FacilityGuide", "Hub_HarbourHall")!, guide);
            Assert.Empty(host.Crossings);

            host.Story.CompleteRegion(region);
            host.Answers.Enqueue(i);
            Play(host, ScriptLibrary.Default.Find("FacilityGuide", "Hub_HarbourHall")!, guide);
            Assert.Equal(new[] { region }, host.Crossings);
            Assert.Equal(map, host.Map!.Name);
            Assert.True(host.Map.IsWalkable(host.PlayerTile.X, host.PlayerTile.Y), $"the landing at {map} is blocked");

            // The guide there, shown once the hub is entered, stands beside the landing and crosses back
            var there = MapDatabase.Get(map);
            there.ApplyPresence(host.Story.Has);
            var backs = there.NPCs.Where(n => n.Script == "HubGuide").ToList();
            Assert.True(backs.Count == 1, $"{map}: {backs.Count} return guides, hidden by {string.Join(",", there.Everyone.Where(n => n.Script == "HubGuide").Select(n => n.HiddenBy + "/" + n.ShownBy))}");
            var back = backs[0];
            Assert.Equal(1, Math.Abs(back.GridX - host.PlayerTile.X) + Math.Abs(back.GridY - host.PlayerTile.Y));
            Play(host, ScriptLibrary.Default.Find("HubGuide", back.ScriptFile ?? "common")!, back);
            Assert.Equal(new[] { region, RegionDatabase.Hub }, host.Crossings);
            Assert.Equal("Hub_HarbourHall", host.Map!.Name);
            Assert.Equal((9, 4), host.PlayerTile);
            Assert.True(hall.IsWalkable(9, 4));
        }
    }

    private static void Play(HeadlessScriptHost host, Script script, NPC? subject)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(script, subject);
        runner.RunToEnd();
    }
}
