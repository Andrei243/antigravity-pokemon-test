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
        // Mom is at the table, and sees to the team as a nurse would until Unova's story is written
        Assert.Contains(home.NPCs, n => n.Name == "Mom" && n.IsHealingNurse);

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
        // The region has no story yet, its starters are Black 2's, and it alone changes with the seasons
        Assert.False(unova.HasStory);
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
        // Every building is a stand-in until plan 22 · U5 gives its town a way of its own
        Assert.All(WorldModels.Unova.Where(m => m.Role == ModelRole.Building), m => Assert.False(string.IsNullOrEmpty(m.StandInUntil), $"{m.Name} is no stand-in"));
        Assert.Equal(Architecture.Plaster, Overworld.ArchitectureAt(47, 762));
        // Unova's names are its own: Sinnoh never looks in its table
        Assert.Null(WorldModels.Of("c12_house"));
    }
}
