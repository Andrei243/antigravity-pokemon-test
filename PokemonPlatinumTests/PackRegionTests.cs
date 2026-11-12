using ContentCheck;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T14, the last part: a region a pack adds. The sample pack <c>samples/packs/tidemark-isle</c> adds
/// Tidemark, a town and one room, after Paldea (after Galar until Paldea had maps, plan 34 · PD1; after Alola until Galar had them, plan 33 · GL1). Most of it is read by hand with the pack handed over (<c>over</c>,
/// <see cref="RegionDatabase.With"/>); where the game reads through the packs in use (its maps, its trainers, its songs)
/// the pack is put in use and put back, and the class runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class PackRegionTests
{
    private static readonly string Sample = Path.Combine(Repository.Root, "samples", "packs", "tidemark-isle");

    private static DataLayer SampleLayer() => new(Sample, DataLayers.ReadManifest(Sample));

    private static string Scratch()
    {
        string folder = Path.Combine(Path.GetTempPath(), "packregion-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>A copy of the sample in a scratch folder, with a change made to its region file's text.</summary>
    private static string CopyOfTheSample(string root, Func<string, string>? region = null)
    {
        string copy = Path.Combine(root, "tidemark-isle");
        foreach (string path in Directory.EnumerateFiles(Sample, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(copy, Path.GetRelativePath(Sample, path));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(path, target);
        }
        if (region != null)
        {
            string file = Path.Combine(copy, "regions", "tidemark.json");
            File.WriteAllText(file, region(File.ReadAllText(file)));
        }
        return copy;
    }

    [Fact]
    public void WithNoPackTheChainIsTheGamesOwn()
    {
        var own = RegionDatabase.With(Array.Empty<DataLayer>());
        Assert.Same(RegionDatabase.All, own.Regions);
        Assert.Same(RegionDatabase.Links, own.Links);
        Assert.Empty(own.Problems);
        Assert.Empty(own.OfPacks);
        Assert.Null(RegionDatabase.Get("Tidemark"));
    }

    [Fact]
    public void TheSamplesRegionJoinsTheChainAfterTheLastRegionWithMaps()
    {
        var chain = RegionDatabase.With(new[] { SampleLayer() });
        Assert.Empty(chain.Problems);
        var tidemark = Assert.Single(chain.OfPacks);
        Assert.Equal("Tidemark", tidemark.Id);
        Assert.Equal("tidemark-isle", tidemark.Pack);
        Assert.True(tidemark.IsBuilt);
        Assert.Equal(new MapSpot("TidemarkTown_House", 4, 7, Direction.Up), tidemark.Start);
        Assert.Equal(new MapSpot("Tidemark", 14, 23, Direction.Up), tidemark.ArrivalSpot);
        Assert.Equal(new[] { "Tidemark", "TidemarkTown_House" }, tidemark.Maps);
        Assert.Same(tidemark, chain.RegionOfMap("TidemarkTown_House"));

        // Right after Paldea, the last region with maps and the last of the chain
        var ids = chain.Regions.Select(r => r.Id).ToList();
        Assert.Equal(ids.IndexOf(RegionDatabase.Paldea) + 1, ids.IndexOf("Tidemark"));
        // The way from Paldea goes to Tidemark by ferry, with its ferryman; Paldea had no way on, so none goes on from Tidemark
        var into = chain.LinkFrom(RegionDatabase.Paldea)!;
        Assert.Equal("Tidemark", into.To);
        Assert.Equal("ferry", into.VesselWord);
        Assert.Equal("LosPlatos_PokemonCenter", into.DepartureMap);
        Assert.True(into.Attendant!.IsTransportAttendant);
        Assert.Null(chain.LinkFrom("Tidemark"));
        Assert.Equal(RegionDatabase.Links.Count + 1, chain.Links.Count);
        // Every game region and link is the very same object as without the pack
        foreach (var region in RegionDatabase.All) Assert.Same(region, chain.Get(region.Id));
        foreach (var link in RegionDatabase.Links) Assert.Contains(link, chain.Links);
    }

    [Fact]
    public void ARegionThatSaysNoPlaceOrTheWrongOneStaysOutOfTheChainAndIsSaid()
    {
        string root = Scratch();
        try
        {
            // No "after": its maps are its own, nothing sails there
            string none = CopyOfTheSample(Path.Combine(root, "a"), t => t.Replace("\"after\": \"Paldea\",", ""));
            var loose = RegionDatabase.With(new[] { new DataLayer(none, DataLayers.ReadManifest(none)) });
            Assert.Equal("Tidemark", loose.Regions[^1].Id);
            Assert.Null(loose.Links.FirstOrDefault(l => l.To == "Tidemark"));
            Assert.Null(loose.LinkFrom(RegionDatabase.Paldea));
            Assert.Contains(loose.Problems, p => p.Contains("\"after\""));

            // After a region that isn't the last with maps
            string early = CopyOfTheSample(Path.Combine(root, "b"), t => t.Replace("\"after\": \"Paldea\"", "\"after\": \"Sinnoh\""));
            var wrong = RegionDatabase.With(new[] { new DataLayer(early, DataLayers.ReadManifest(early)) });
            Assert.Contains(wrong.Problems, p => p.Contains("only after the last with maps") && p.Contains("Paldea"));
            Assert.Equal(RegionDatabase.Kanto, wrong.LinkFrom(RegionDatabase.Sinnoh)!.To);

            // A region file of a game region's name is the game's file replaced, never a region of its own
            string clash = CopyOfTheSample(Path.Combine(root, "c"), t => t.Replace("\"id\": \"Tidemark\"", "\"id\": \"Kanto\""));
            var twice = RegionDatabase.With(new[] { new DataLayer(clash, DataLayers.ReadManifest(clash)) });
            Assert.Empty(twice.OfPacks);
            Assert.Contains(twice.Problems, p => p.Contains("there already"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ThePacksWorldFolderItsIndexAndItsTrainersAreReadAcrossTheLayers()
    {
        var over = new[] { SampleLayer() };
        Assert.Contains("tidemark", DataLayers.FoldersIn(World.Folder, over));
        Assert.DoesNotContain("tidemark", DataLayers.FoldersIn(World.Folder));
        // A region the game has keeps its own index; a region the pack adds reads the pack's
        Assert.True(DataLayers.IsFixed("world/sinnoh/world.json"));
        Assert.False(DataLayers.IsFixed("world/tidemark/world.json"));
        Assert.Equal(Path.Combine(Sample, "world/tidemark/world.json"), DataLayers.PathOf("world/tidemark/world.json", over));
        Assert.Equal(Path.Combine(GameDataFiles.Directory, "world/sinnoh/world.json"), DataLayers.PathOf("world/sinnoh/world.json", over));
        // Its song is a song of the music folder's, its region file one of the regions'
        Assert.Contains(DataLayers.FilesIn("music", "*.mml", recursive: true, over: over), f => f.Key == "tidemark/town.mml");
        Assert.Contains(DataLayers.FilesIn("regions", "*.json", over: over), f => f.Key == "tidemark.json");
        // Its trainers are a region's table of their own, beside the game's
        var tables = TrainerDatabase.LoadRegions(over);
        Assert.Equal("tidemark_fisherman_wade", Assert.Single(tables["tidemark"]).Id);
        Assert.Equal(TrainerDatabase.LoadRegions().Keys.Order(), tables.Keys.Where(k => k != "tidemark").Order());
    }

    [Fact]
    public void TheDrawingMakesTheSamplesWorldFiles()
    {
        string root = MapDraw.Repository.Root();
        string target = Path.Combine(Sample, "world", "tidemark");
        var files = MapDraw.Repository.Files(Path.Combine(root, "tools", "MapDraw", "tidemark"), target);
        foreach (var (path, text) in files)
            Assert.True(File.Exists(path) && File.ReadAllText(path).Replace("\r\n", "\n") == text,
                $"{path} is not what the drawing gives: run `dotnet run --project tools/MapDraw -- tidemark --out samples/packs/tidemark-isle/world/tidemark`");
        Assert.Empty(MapDraw.Repository.Stale(target, files));
    }

    [Fact]
    public void InUseThePacksRegionIsBuiltItsWaysStayInItAndTheFerrymanWaitsInPaldea()
    {
        var layer = SampleLayer();
        try
        {
            DataLayers.Use(new[] { layer });
            Assert.Equal("Tidemark", RegionDatabase.Get("Tidemark")!.Id);
            Assert.True(MusicLibrary.Reload().Count == 0);
            Assert.True(MusicLibrary.Exists("tidemark/town"));
            var maps = MapDatabase.Fresh();

            var town = maps("Tidemark");
            Assert.Equal("Tidemark", town.Name);
            Assert.Equal("Tidemark Town", town.DisplayNameAt(14, 23));
            Assert.Equal("tidemark/town", town.BgmTrackAt(14, 23));
            var room = maps("TidemarkTown_House");
            Assert.Equal("TidemarkTown_House", room.Name);
            // Every way out of either map stays in the region, and leads to a tile one can stand on
            var region = RegionDatabase.Get("Tidemark")!;
            foreach (var map in new[] { town, room })
            {
                Assert.Same(region, RegionDatabase.RegionOfMap(map.Name));
                Assert.NotEmpty(map.Warps);
                foreach (var warp in map.Warps)
                {
                    Assert.Contains(warp.TargetMap, region.Maps);
                    var beyond = maps(warp.TargetMap);
                    Assert.Equal(warp.TargetMap, beyond.Name);
                    Assert.False(beyond.IsSolid(warp.TargetX, warp.TargetY), $"{map.Name}'s way to {warp.TargetMap} lands on {warp.TargetX},{warp.TargetY}, which is solid");
                }
            }
            // The house's door leads in and the room's leads back out before it
            Assert.Equal("TidemarkTown_House", town.GetWarpAt(6, 7)!.TargetMap);
            Assert.Equal((6, 8), (room.GetWarpAt(4, 9)!.TargetX, room.GetWarpAt(4, 9)!.TargetY));
            // The start and the arrival stand on open ground
            Assert.False(room.IsSolid(region.Start!.X, region.Start.Y));
            Assert.True(town.IsWalkable(region.ArrivalSpot!.X, region.ArrivalSpot.Y));
            // The fisherman is a trainer of the region's own table
            var wade = town.Everyone.Single(n => n.Name == "Wade");
            Assert.Equal("tidemark_fisherman_wade", wade.TrainerData!.Id);

            // And the ferryman stands in Los Platos's Pokémon Center, where nobody else does
            var station = maps("LosPlatos_PokemonCenter");
            var ferryman = station.Everyone.Single(n => n.IsTransportAttendant);
            Assert.Equal((2, 7), (ferryman.GridX, ferryman.GridY));
            Assert.Equal(1, station.Everyone.Count(n => n.GridX == 2 && n.GridY == 7));
            Assert.Equal(TravelCheck.StoryUnfinished, RegionDatabase.CheckTravel(RegionDatabase.LinkFrom(RegionDatabase.Paldea)!, new PokemonPlatinumEngine.Story.StoryState()));
        }
        finally
        {
            DataLayers.Use(null);
            MusicLibrary.Reload();
            MapDatabase.Initialize();
        }
        // Without the pack the game is what it was: no Tidemark, nobody new in Paldea
        Assert.Null(RegionDatabase.Get("Tidemark"));
        Assert.Null(MapDatabase.TryGet("Tidemark"));
        Assert.DoesNotContain(MapDatabase.Get("LosPlatos_PokemonCenter").Everyone, n => n.IsTransportAttendant);
    }

    [Fact]
    public void TheSamplePassesValidatePacks()
    {
        var findings = PackCheck.Check(Sample);
        Assert.True(findings.Count == 0, "the sample's findings:\n" + string.Join("\n", findings));
    }

    [Fact]
    public void ARegionThatBreaksTheRegionsRulesIsNamedByValidatePacks()
    {
        string root = Scratch();
        try
        {
            string broken = CopyOfTheSample(root, t => t
                // A map no region holds, a way into Sinnoh, a start on a wall, no attendant and a vessel nobody knows
                .Replace("\"maps\": [ \"Tidemark\", \"TidemarkTown_House\" ]", "\"maps\": [ \"TidemarkTown_House\", \"PlayerHouse2F\" ]")
                .Replace("\"start\": { \"map\": \"TidemarkTown_House\", \"x\": 4, \"y\": 7", "\"start\": { \"map\": \"TidemarkTown_House\", \"x\": 0, \"y\": 0")
                .Replace("\"vessel\": \"ferry\"", "\"vessel\": \"raft\"")
                .Replace("\"isTransportAttendant\": true", "\"isTransportAttendant\": false"));
            string room = Path.Combine(broken, "maps", "TidemarkTown_House.json");
            File.WriteAllText(room, File.ReadAllText(room).Replace("\"targetMap\": \"Tidemark\"", "\"targetMap\": \"PlayerHouse1F\""));

            var findings = PackCheck.Check(broken);
            Assert.Contains(findings, f => f.Contains("the map Tidemark belongs to no region"));
            Assert.Contains(findings, f => f.Contains("PlayerHouse2F is Sinnoh's"));
            Assert.Contains(findings, f => f.StartsWith("maps/TidemarkTown_House.json") && f.Contains("no way crosses regions"));
            Assert.Contains(findings, f => f.Contains("its start") && f.Contains("no open ground"));
            Assert.Contains(findings, f => f.Contains("\"raft\""));
            Assert.Contains(findings, f => f.Contains("isTransportAttendant"));
            Assert.All(findings.Where(f => f.Contains("regions/")), f => Assert.StartsWith("regions/tidemark.json", f));

            // A way to it that leaves from nowhere of Paldea's, and a region that joins after the wrong one
            string again = CopyOfTheSample(Path.Combine(root, "again"), t => t
                .Replace("\"departureMap\": \"LosPlatos_PokemonCenter\"", "\"departureMap\": \"Sinnoh\""));
            Assert.Contains(PackCheck.Check(again), f => f.Contains("Sinnoh, which is no map of Paldea"));
            string after = CopyOfTheSample(Path.Combine(root, "after"), t => t.Replace("\"after\": \"Paldea\"", "\"after\": \"Johto\""));
            Assert.Contains(PackCheck.Check(after), f => f.StartsWith("regions/tidemark.json") && f.Contains("only after the last with maps"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TheSampleAddsNothingToTheContentCheckAndItsWorldIndexIsMerged()
    {
        string root = Scratch();
        try
        {
            var sample = PackContent.Read(new[] { Sample });
            PackContent.Merge(GameDataFiles.Directory, sample, Path.Combine(root, "merged"));
            // A region the pack adds keeps its own index in the merge; a game region's stays the game's
            Assert.True(File.Exists(Path.Combine(root, "merged", "world", "tidemark", "world.json")));
            Assert.Equal(File.ReadAllText(Path.Combine(GameDataFiles.Directory, "world", "kalos", "world.json")),
                File.ReadAllText(Path.Combine(root, "merged", "world", "kalos", "world.json")));
            var added = PackContent.Check(GameDataFiles.Directory, sample, Path.Combine(root, "sample"));
            Assert.True(added.Count == 0, "the sample adds:\n" + string.Join("\n", added));
        }
        finally
        {
            DataLayers.Use(null);
            MusicLibrary.Reload();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ASaveInAPacksRegionNamesItAndWakesWhereItBeganWithoutIt()
    {
        var layer = SampleLayer();
        var tidemark = RegionDatabase.With(new[] { layer }).Get("Tidemark")!;
        // The pack whose region the player stands in is marked; a save in a game region marks none
        var saved = PackRecords.Of(new[] { layer }, tidemark)!;
        Assert.Equal("Tidemark", Assert.Single(saved).Region);
        Assert.Null(Assert.Single(PackRecords.Of(new[] { layer }, RegionDatabase.First)!).Region);
        Assert.DoesNotContain("\"region\"", GameDataFiles.Serialize(PackRecords.Of(new[] { layer }, RegionDatabase.First)));

        // Without it the title says where the game was saved and that it wakes where it began
        var missing = PackRecords.Missing(saved, Array.Empty<DataLayer>());
        Assert.Equal("Tidemark", Assert.Single(missing).Region);
        var notice = TitleNotice.ForPacks(2, missing)!;
        Assert.Contains("Tidemark", notice.Body);
        Assert.Contains("tidemark-isle", notice.Body);
        Assert.Contains("wakes where it began", notice.Body);
        // Another version, or the pack still there, says nothing of the region
        Assert.DoesNotContain("wakes", TitleNotice.ForPacks(2, new[] { new MissingPack("tidemark-isle", "0.9", "1.0", "Tidemark") })!.Body);

        // The save stands on a map no region has: it wakes at the start of the region its game began in
        var save = new SaveData { CurrentMapName = "TidemarkTown_House", PlayerGridX = 4, PlayerGridY = 7, BeganIn = RegionDatabase.Sinnoh, Packs = saved };
        Assert.Equal(RegionDatabase.First.Start, save.WhereAPackLeftIt(_ => null));
        Assert.Equal(RegionDatabase.First.Start, save.WhereAPackLeftIt(MapDatabase.TryGet));
        var home = new SaveData { CurrentMapName = "PlayerHouse2F", PlayerGridX = 4, PlayerGridY = 6 };
        Assert.Null(home.WhereAPackLeftIt(MapDatabase.TryGet));
    }
}
