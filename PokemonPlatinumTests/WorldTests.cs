using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// The part of Sinnoh that is played on imported data (plan 01 · M2): the world's index and overlays, the maps
/// made from them, the way on foot from Twinleaf Town to the top of Route 202, and what streaming relies on.
/// </summary>
public class WorldTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static World Sinnoh => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Sinnoh"];
    private static Map Lake => BuiltMaps.Value["LakeVerity"];

    /// <summary>A map by name: this class's copy of a map of the world, the database's for a room or a hand-made town.</summary>
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);

    private static IEnumerable<(string Key, WorldAreaFile File, WorldOverlayFile Overlay, Map Map)> OpenAreas() =>
        Sinnoh.Index.Areas.Select(key => (key, Sinnoh.Area(key)!, Sinnoh.Overlay(key)!, BuiltMaps.Value[Sinnoh.MapOf(key)!.Name]));

    private static readonly (int X, int Y)[] Steps = { (0, -1), (1, 0), (-1, 0), (0, 1) };

    /// <summary>
    /// Every tile the player can get to from a tile on foot by the field's own rules (ledges hopped the way they
    /// face, never climbed; water and cliffs in the way). A warp is the end of the way, since stepping on it
    /// leaves the map.
    /// </summary>
    private static HashSet<(int X, int Y)> Reach(Map map, int startX, int startY)
    {
        var seen = new HashSet<(int, int)> { (startX, startY) };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((startX, startY));
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if ((x, y) != (startX, startY) && map.GetWarpAt(x, y) != null) continue;
            foreach (var dir in new[] { Direction.Up, Direction.Right, Direction.Left, Direction.Down })
            {
                var step = FieldMovement.Step(map, x, y, dir, new Walker(Height: map.HeightAt(x, y)));
                if (step.Moves && seen.Add((step.X, step.Y))) queue.Enqueue((step.X, step.Y));
            }
        }
        return seen;
    }

    private static bool CanStandBeside(HashSet<(int X, int Y)> reached, int x, int y) =>
        Steps.Any(s => reached.Contains((x + s.X, y + s.Y)));

    // ------------------------------------------------------------------ the files

    [Fact]
    public void TheIndexSaysWhatIsBuilt()
    {
        Assert.Equal(new[] { "Sinnoh", "LakeVerity" }, Sinnoh.Index.Maps.Select(m => m.Name));
        Assert.Equal(0, Sinnoh.Index.Maps[0].Matrix);
        Assert.Null(Sinnoh.Index.Maps[0].Area);   // the overworld's matrix names the area of each chunk itself
        Assert.Equal(Sinnoh.Index.Areas.Count, Sinnoh.Index.Areas.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.True(Sinnoh.IsOpen("ROUTE_201"));
        Assert.False(Sinnoh.IsOpen("jubilife_city"));

        foreach (string key in Sinnoh.Index.Areas)
        {
            var area = Sinnoh.Area(key);
            Assert.True(area != null, $"{key} is open but has no area file: run tools/MapImporter with --data");
            Assert.Equal(key, area!.Key);
            Assert.True(Sinnoh.MapOf(key) != null, $"{key} lies on matrix {area.Matrix}, which no map is made of");

            var overlay = Sinnoh.Overlay(key);
            Assert.True(overlay != null, $"{key} is open but has no overlay: it would have no music and nobody in it");
            Assert.Equal(key, overlay!.Area);
            Assert.False(string.IsNullOrEmpty(overlay.BgmTrack), $"{key} has no music");
        }

        // The game's maps are made from the same files
        MapDatabase.Initialize();
        Assert.All(Sinnoh.Index.Maps, entry => Assert.Equal(entry.Name, MapDatabase.Get(entry.Name).Name));
        Assert.Equal("Sinnoh", MapDatabase.StartMap);
        Assert.All(Fixtures.Names.Where(n => n != "LakeVerity"), old => Assert.DoesNotContain(old, MapDatabase.MapNames));
    }

    /// <summary>
    /// From the edge of an open area the chunks next to it are in view, so the game ships them too (as scenery).
    /// A chunk missing here means the importer wasn't run with --data after an area was opened.
    /// </summary>
    [Fact]
    public void EveryChunkAnOpenAreaShowsHasItsFile()
    {
        foreach (var entry in Sinnoh.Index.Maps)
        {
            var matrix = Sinnoh.Matrix(entry.Matrix);
            int open = 0;
            for (int cy = 0; cy < matrix.Height; cy++)
                for (int cx = 0; cx < matrix.Width; cx++)
                {
                    string? key = matrix.AreaAt(cx, cy) ?? entry.Area;
                    if (key == null || !Sinnoh.IsOpen(key)) continue;
                    open++;

                    for (int y = Math.Max(0, cy - 1); y <= Math.Min(matrix.Height - 1, cy + 1); y++)
                        for (int x = Math.Max(0, cx - 1); x <= Math.Min(matrix.Width - 1, cx + 1); x++)
                        {
                            int id = matrix.ChunkAt(x, y);
                            if (id == WorldMatrixFile.NoChunk) continue;
                            Assert.True(Sinnoh.Chunk(id) != null, $"chunk {id:000} at ({x},{y}) of matrix {entry.Matrix} is in view of {key} but has no file");
                        }
                }
            Assert.True(open > 0, $"{entry.Name} has no open area");
        }
    }

    /// <summary>An overlay names things by their place in the imported files; a slip of the pen there would only make something quietly not appear.</summary>
    [Fact]
    public void EveryOverlayEntryPointsAtSomethingThatIsThere()
    {
        MapDatabase.Initialize();
        foreach (var (key, file, overlay, map) in OpenAreas())
        {
            var ids = file.Objects.Select(o => o.Id).ToHashSet();
            foreach (string person in overlay.People?.Keys ?? Enumerable.Empty<string>())
                Assert.True(ids.Contains(person), $"{key}: the overlay gives lines to '{person}', and nobody in the area is called that");
            foreach (var (id, person) in overlay.People ?? new())
                Assert.True(person.Dialog?.Count > 0 || person.Trainer != null || person.IsStarterBriefcase == true, $"{key}: {id} has nothing to say");

            foreach (string sign in overlay.Signs?.Keys ?? Enumerable.Empty<string>())
            {
                var named = file.Objects.FirstOrDefault(o => o.Id == sign);
                Assert.True(named != null, $"{key}: the overlay writes on '{sign}', and the area has no such sign");
                Assert.True(map.GetSignboardAt(named!.X, named.Z) == overlay.Signs![sign], $"{key}: '{sign}' is not a sign that can be read");
            }

            foreach (var door in overlay.Doors ?? new())
            {
                Assert.InRange(door.Warp, 0, file.Warps.Count - 1);
                Assert.True(MapDatabase.MapNames.Contains(door.Map), $"{key}: door {door.Warp} leads to {door.Map}, which is no map");
                Assert.True(overlay.Locked?.Contains(door.Warp) != true, $"{key}: door {door.Warp} is both open and locked");
            }
            foreach (int locked in overlay.Locked ?? new())
                Assert.InRange(locked, 0, file.Warps.Count - 1);

            foreach (var exit in overlay.Exits ?? new())
            {
                Assert.Equal(key, map.AreaAt(exit.X, exit.Z)?.Key);
                Assert.True(MapDatabase.MapNames.Contains(exit.Map), $"{key}: the exit at ({exit.X},{exit.Z}) leads to {exit.Map}, which is no map");
            }

            // What the overlay adds of its own stands inside its area
            foreach (var npc in overlay.Npcs ?? new()) Assert.Equal(key, map.AreaAt(npc.X, npc.Y)?.Key);
            foreach (var prop in overlay.Props ?? new()) Assert.Equal(key, map.AreaAt(prop.X, prop.Y)?.Key);
        }
    }

    [Fact]
    public void AnOverlayReadsBackAsItWasWritten()
    {
        var overlay = new WorldOverlayFile
        {
            Area = "somewhere",
            BgmTrack = "sinnoh/twinleaf",
            Trees = TreeStyle.Pine,
            Architecture = Architecture.Plaster,
            Doors = new() { new OverlayDoor { Warp = 2, Map = "PlayerHouse", X = 4, Y = 6, Facing = Direction.Left } },
            Locked = new() { 0, 1 },
            Exits = new() { new OverlayExit { X = 170, Z = 800, Map = "JubilifeCity", ToX = 19, ToY = 32 } },
            People = new() { ["lass"] = new OverlayPerson { Name = "Lass", Dialog = new() { "Hello!" } } },
            Signs = new() { ["map_signpost"] = "Somewhere\nA place." }
        };

        string json = JsonSerializer.Serialize(overlay, GameDataFiles.Json);
        Assert.Contains("\"facing\":\"Left\"", json.Replace(" ", ""));
        Assert.Contains("\"architecture\":\"Plaster\"", json.Replace(" ", ""));

        var read = JsonSerializer.Deserialize<WorldOverlayFile>(json, GameDataFiles.Json)!;
        Assert.Equal((2, "PlayerHouse", 4, 6, Direction.Left), (read.Doors![0].Warp, read.Doors[0].Map, read.Doors[0].X, read.Doors[0].Y, read.Doors[0].Facing));
        Assert.Equal(new[] { 0, 1 }, read.Locked);
        Assert.Equal(("JubilifeCity", 19, 32), (read.Exits![0].Map, read.Exits[0].ToX, read.Exits[0].ToY));
        Assert.Equal("Hello!", read.People!["lass"].Dialog![0]);
        Assert.Equal((TreeStyle.Pine, Architecture.Plaster), (read.Trees!.Value, read.Architecture!.Value));

        var index = JsonSerializer.Deserialize<WorldIndexFile>(
            "{ \"region\": \"Sinnoh\", \"maps\": [ { \"name\": \"Cave\", \"matrix\": 7, \"area\": \"a_cave\" } ], \"areas\": [ \"a_cave\" ] }", GameDataFiles.Json)!;
        Assert.Equal(("Cave", 7, "a_cave", TreeStyle.Pine), (index.Maps[0].Name, index.Maps[0].Matrix, index.Maps[0].Area, index.Maps[0].Trees));
    }

    // ------------------------------------------------------------------ the map made from them

    [Fact]
    public void TheOverworldIsOneMapMeasuredInTilesOfTheWholeRegion()
    {
        var map = Overworld;
        Assert.Equal((960, 960), (map.Width, map.Height));
        Assert.Equal((30, 30), (map.ChunkColumns, map.ChunkRows));
        Assert.True(map.IsStreamed);
        Assert.False(map.IsIndoors);

        foreach (var (key, name, x, y) in new[]
        {
            ("twinleaf_town", "Twinleaf Town", 116, 886), ("route_201", "Route 201", 130, 853), ("verity_lakefront", "Verity Lakefront", 80, 846),
            ("sandgem_town", "Sandgem Town", 177, 843), ("route_202", "Route 202", 174, 815), ("route_219", "Route 219", 178, 870)
        })
        {
            var area = map.AreaAt(x, y);
            Assert.Equal(key, area?.Key);
            Assert.True(area!.Open);
            Assert.Equal(name, map.DisplayNameAt(x, y));
            Assert.Same(area, map.FindArea(key));

            // An area's bounds are whole chunks and hold the place
            var bounds = map.AreaBounds(key)!.Value;
            Assert.Equal((0, 0, 0, 0), (bounds.X % Map.ChunkTiles, bounds.Y % Map.ChunkTiles, bounds.Width % Map.ChunkTiles, bounds.Height % Map.ChunkTiles));
            Assert.True(x >= bounds.X && x < bounds.X + bounds.Width && y >= bounds.Y && y < bounds.Y + bounds.Height);
        }

        // Each place has the look and the music its overlay gives it
        Assert.Equal("sinnoh/twinleaf", map.BgmTrackAt(116, 886));
        Assert.NotEqual(map.BgmTrackAt(116, 886), map.BgmTrackAt(130, 853));
        Assert.Equal((TreeStyle.Pine, Architecture.Timber), (map.TreesAt(116, 886), map.ArchitectureAt(116, 886)));
        Assert.Equal(Architecture.Plaster, map.ArchitectureAt(177, 843));
        Assert.Equal(TreeStyle.Round, map.TreesAt(178, 870));

        // Where nothing is imported yet there is forest, under the region's own name
        Assert.Equal(TileType.Tree, map.GetGroundTile(2, 2));
        Assert.True(map.IsSolid(2, 2));
        Assert.Equal("Sinnoh", map.DisplayNameAt(2, 2));
        Assert.Null(map.AreaAt(-1, 5));

        // The lake is a map of its own, as it is a place of its own in Platinum, and all one area
        Assert.Equal((96, 64), (Lake.Width, Lake.Height));
        Assert.True(Lake.IsStreamed);
        Assert.Equal("Lake Verity", Lake.DisplayNameAt(46, 53));
        Assert.Equal("lake_verity", Assert.Single(Lake.Areas).Key);
    }

    [Fact]
    public void ANewGameStartsOutsideThePlayersDoor()
    {
        var start = RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!;
        Assert.Equal(("Sinnoh", 116, 886), (start.Map, start.X, start.Y));
        Assert.True(Overworld.IsWalkable(116, 886));
        Assert.Equal("twinleaf_town", Overworld.AreaAt(116, 886)!.Key);
        Assert.Equal("PlayerHouse", Overworld.GetWarpAt(116, 885)?.TargetMap);

        var fresh = new SaveData();
        Assert.Equal(start, fresh.Place() with { Facing = start.Facing });

        // Every map of the world belongs to the region
        Assert.All(Sinnoh.Index.Maps, entry => Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(entry.Name)?.Id));
    }

    [Fact]
    public void OneCanWalkFromTwinleafTownToTheTopOfRoute202AndBack()
    {
        var map = Overworld;
        var out_ = Reach(map, 116, 886);

        // Up Route 201, through Sandgem Town and Route 202 to the edge of Jubilife City, with no map change on the way
        Assert.Contains((174, 801), out_);
        Assert.Equal("JubilifeCity", map.GetWarpAt(174, 800)?.TargetMap);
        foreach (string area in new[] { "twinleaf_town", "route_201", "verity_lakefront", "sandgem_town", "route_202", "route_219" })
            Assert.True(out_.Any(t => map.AreaAt(t.X, t.Y)!.Key == area), $"{area} can't be walked to from Twinleaf Town");

        // And home again from where Jubilife City puts the player down
        var back = Reach(map, 173, 801);
        Assert.Contains((116, 886), back);

        // Nobody can walk out of what is built
        Assert.All(out_, t => Assert.True(map.AreaAt(t.X, t.Y)!.Open));
    }

    [Fact]
    public void AnAreaThatIsNotBuiltYetIsSceneryNobodyCanEnter()
    {
        foreach (var map in BuiltMaps.Value.Values)
        {
            int closed = 0;
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    if (map.AreaAt(x, y) is { Open: true }) continue;
                    if (!map.IsSolid(x, y)) closed++;
                }
            Assert.True(closed == 0, $"{map.Name}: {closed} tiles outside the open areas can be walked on");
        }

        // The part of Jubilife City in view from Route 202 is such scenery: it has its buildings, and no way in
        var jubilife = Overworld.AreaAt(174, 790)!;
        Assert.Equal("jubilife_city", jubilife.Key);
        Assert.False(jubilife.Open);
        Assert.Contains(MapStructures.FindBuildings(Overworld), b => Overworld.AreaAt(b.X0, b.Y0) == jubilife);
        Assert.DoesNotContain(Overworld.NPCs, n => Overworld.AreaAt(n.GridX, n.GridY) == jubilife);
    }

    [Theory]
    [InlineData("Sinnoh")]
    [InlineData("LakeVerity")]
    public void EveryWarpOfTheWorldLandsOnOpenGroundAndLeadsBack(string name)
    {
        MapDatabase.Initialize();
        var map = MapNamed(name);
        Assert.NotEmpty(map.Warps);

        foreach (var warp in map.Warps)
        {
            string where = $"{name}: the warp at ({warp.SourceX},{warp.SourceY}) to {warp.TargetMap}";
            Assert.True(map.AreaAt(warp.SourceX, warp.SourceY)?.Open, $"{where} is outside the open areas");
            Assert.False(map.IsSolid(warp.SourceX, warp.SourceY), $"{where} can't be stepped on");
            Assert.Single(map.Warps, w => (w.SourceX, w.SourceY) == (warp.SourceX, warp.SourceY));

            Assert.True(MapDatabase.MapNames.Contains(warp.TargetMap), $"{where} leads to no map");
            var target = MapNamed(warp.TargetMap);
            Assert.True(target.IsWalkable(warp.TargetX, warp.TargetY), $"{where} puts the player on a blocked tile ({warp.TargetX},{warp.TargetY})");
            Assert.True(target.GetWarpAt(warp.TargetX, warp.TargetY) == null, $"{where} puts the player on another warp");

            // Coming back, one is put down within a few steps of where one left
            Assert.True(target.Warps.Any(w => w.TargetMap == name && Math.Abs(w.TargetX - warp.SourceX) + Math.Abs(w.TargetY - warp.SourceY) <= 8),
                $"{where}: nothing in {warp.TargetMap} leads back to it");
        }
    }

    [Fact]
    public void ADoorIsOpenOrListedAsLocked()
    {
        foreach (var (key, file, overlay, map) in OpenAreas())
        {
            for (int i = 0; i < file.Warps.Count; i++)
            {
                var warp = file.Warps[i];
                bool locked = overlay.Locked?.Contains(i) == true;
                bool open = map.GetWarpAt(warp.X, warp.Z) != null;
                if (locked)
                {
                    // Two of the original's warps may share a tile (the lake, and the lake at low water)
                    if (file.Warps.Count(w => (w.X, w.Z) == (warp.X, warp.Z)) == 1)
                        Assert.False(open, $"{key}: warp {i} at ({warp.X},{warp.Z}) is listed as locked, and leads to {map.GetWarpAt(warp.X, warp.Z)?.TargetMap}");
                    continue;
                }
                if (map.GetGroundTile(warp.X, warp.Z) != TileType.Door) continue;
                Assert.True(open, $"{key}: the door at ({warp.X},{warp.Z}) (warp {i}, to {warp.To}) leads nowhere and isn't listed as locked");
                Assert.False(map.IsSolid(warp.X, warp.Z));
            }

            // A locked door stays shut: a wall with a door painted on it
            foreach (int i in overlay.Locked ?? new())
            {
                var warp = file.Warps[i];
                if (map.GetGroundTile(warp.X, warp.Z) == TileType.Door && map.GetWarpAt(warp.X, warp.Z) == null)
                    Assert.True(map.IsSolid(warp.X, warp.Z), $"{key}: the locked door at ({warp.X},{warp.Z}) can be walked into");
            }
        }
    }

    [Fact]
    public void PeopleStandOnOpenGroundWhereTheyCanBeTalkedTo()
    {
        var reached = new Dictionary<Map, HashSet<(int X, int Y)>>
        {
            [Overworld] = Reach(Overworld, 116, 886),
            [Lake] = Reach(Lake, Overworld.GetWarpAt(80, 843)!.TargetX, Overworld.GetWarpAt(80, 843)!.TargetY)
        };

        int people = 0;
        foreach (var (map, reach) in reached)
        {
            foreach (var npc in map.NPCs)
            {
                people++;
                string who = $"{map.Name}: {npc.Name} at ({npc.GridX},{npc.GridY})";
                Assert.True(map.AreaAt(npc.GridX, npc.GridY)?.Open, $"{who} stands outside the open areas");
                Assert.False(map.IsSolid(npc.GridX, npc.GridY), $"{who} stands in something solid");
                Assert.True(map.GetWarpAt(npc.GridX, npc.GridY) == null, $"{who} stands on a warp");
                Assert.Single(map.NPCs, n => (n.GridX, n.GridY) == (npc.GridX, npc.GridY));
                Assert.True(CanStandBeside(reach, npc.GridX, npc.GridY), $"{who} can't be walked up to");
                Assert.True(npc.DialogLines.Count > 0 || npc.IsTrainer || npc.IsStarterBriefcase, $"{who} has nothing to say");
            }

            // Signs and mailboxes can be read from a tile beside them
            foreach (var ((x, y), text) in map.Signboards)
            {
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.True(map.GetGroundTile(x, y) == TileType.Signpost || map.Props.Any(p => p.Type == PropType.Mailbox && p.Covers(x, y)), $"{map.Name}: nothing stands at ({x},{y}) to read");
                Assert.True(map.IsSolid(x, y), $"{map.Name}: the sign at ({x},{y}) can be walked through");
                Assert.True(CanStandBeside(reach, x, y), $"{map.Name}: the sign at ({x},{y}) can't be walked up to");
            }

            // Lamps, benches, boulders and fences block the way
            foreach (var prop in map.Props.Where(p => p.IsSolid))
                for (int y = prop.Y; y < prop.Y + prop.Depth; y++)
                    for (int x = prop.X; x < prop.X + prop.Width; x++)
                        Assert.True(map.IsSolid(x, y), $"{map.Name}: the {prop.Type} at ({x},{y}) can be walked through");
        }
        Assert.True(people >= 15, $"only {people} people in the world");

        // The story's first steps: the rival in town, the professor and his briefcase on Route 201
        Assert.Equal("twinleaf_town", Overworld.AreaAt(Overworld.NPCs.Single(n => n.Name == "Barry").GridX, Overworld.NPCs.Single(n => n.Name == "Barry").GridY)!.Key);
        var briefcase = Overworld.NPCs.Single(n => n.IsStarterBriefcase);
        Assert.Equal("starter_briefcase", briefcase.Id);
        Assert.Equal("route_201", Overworld.AreaAt(briefcase.GridX, briefcase.GridY)!.Key);

        // Route 202's trainers
        var trainers = Overworld.NPCs.Where(n => n.IsTrainer).ToList();
        Assert.Equal(new[] { "Logan", "Natalie", "Tristan" }, trainers.Select(t => t.Name).OrderBy(n => n));
        Assert.All(trainers, t => Assert.Equal("route_202", Overworld.AreaAt(t.GridX, t.GridY)!.Key));
        Assert.All(trainers, t => Assert.True(t.TrainerData!.Party.Count > 0));
    }

    [Fact]
    public void WildPokemonAreThoseOfTheAreaTheGrassIsIn()
    {
        var map = Overworld;
        var route201 = map.FindArea("route_201")!;
        Assert.Equal(WorldAreaFile.LandSlotWeights.Length, route201.WildEncounters.Count);
        Assert.Equal(100, WorldAreaFile.LandSlotWeights.Sum());
        Assert.Equal(100, route201.WildEncounters.Sum(e => e.Weight));
        Assert.Equal(new[] { "Bidoof", "Kricketot", "Starly" }, route201.WildEncounters.Select(e => e.SpeciesName).Distinct().OrderBy(n => n));
        Assert.Contains(map.FindArea("route_202")!.WildEncounters, e => e.SpeciesName == "Shinx");
        Assert.Empty(map.FindArea("sandgem_town")!.WildEncounters);

        foreach (var area in BuiltMaps.Value.Values.SelectMany(m => m.Areas))
            foreach (var wild in area.WildEncounters)
            {
                Assert.True(PokemonDatabase.Get(wild.SpeciesName) != null, $"{area.Key}: no species is called {wild.SpeciesName}");
                Assert.InRange(wild.MinLevel, 1, 100);
                Assert.Equal(wild.MinLevel, wild.MaxLevel);
            }

        // Tall grass grows only where something lives in it, and a step in it meets that area's Pokémon
        (int X, int Y)? grass = null;
        foreach (var m in BuiltMaps.Value.Values)
            for (int y = 0; y < m.Height; y++)
                for (int x = 0; x < m.Width; x++)
                {
                    if (m.GetGroundTile(x, y) != TileType.TallGrass || m.AreaAt(x, y) is not { Open: true } area) continue;
                    Assert.True(area.WildEncounters.Count > 0, $"{m.Name}: tall grass at ({x},{y}) in {area.Key}, where nothing lives");
                    if (m == map && area == route201) grass ??= (x, y);
                }
        Assert.NotNull(grass);

        var met = new HashSet<string>();
        var steps = new EncounterSteps();
        for (int i = 0; i < 4000; i++)
            if (map.RollWildEncounter(grass!.Value.X, grass.Value.Y, steps) is { } wild) met.Add(wild.SpeciesName);
        Assert.NotEmpty(met);
        Assert.Subset(route201.WildEncounters.Select(e => e.SpeciesName).ToHashSet(), met);
        Assert.Null(map.RollWildEncounter(177, 843, new EncounterSteps()));   // nothing jumps out in town
    }

    [Fact]
    public void TheBuildingsOfTheTownsStandWhereTheImportPutsThem()
    {
        var map = Overworld;
        var buildings = MapStructures.FindBuildings(map);
        List<BuildingInfo> In(string area) => buildings.Where(b => map.AreaAt(b.X0, b.Y0)!.Key == area).ToList();

        // Twinleaf Town: four houses built its way, the player's and the rival's open and a storey taller
        var twinleaf = In("twinleaf_town");
        Assert.Equal(4, twinleaf.Count);
        Assert.All(twinleaf, b => Assert.Equal((BuildingKind.House, (Architecture?)Architecture.Timber), (b.Kind, b.Town)));
        Assert.Equal(new[] { "t1_h01", "t1_h01", "t1_s01", "t1_s02" }, twinleaf.Select(b => b.Model).OrderBy(m => m, StringComparer.Ordinal));
        Assert.All(twinleaf, b => Assert.Equal(b.Model == "t1_h01" ? 0 : 2, b.Storeys));
        var home = twinleaf.Single(b => b.Doors.Any(d => d.Target == "PlayerHouse"));
        Assert.Equal((115, 882, 119, 885), (home.X0, home.Y0, home.X1, home.Y1));
        Assert.Equal(new[] { (116, (string?)"PlayerHouse") }, home.Doors);
        Assert.Contains(twinleaf, b => b.Doors.Any(d => d.Target == "RivalHouse"));
        Assert.Equal(2, twinleaf.Count(b => b.Doors.All(d => d.Target == null)));

        // Sandgem Town: what a building is comes from the name of its model, not from where its door leads, and
        // it stands on exactly the tiles its model blocks. The signs beside the Center and the Mart stand free.
        var sandgem = In("sandgem_town");
        Assert.Equal(5, sandgem.Count);
        Assert.Contains(sandgem, b => b.Kind == BuildingKind.PokemonCenter && (b.X0, b.Y0, b.X1, b.Y1) == (175, 839, 179, 842));
        Assert.Contains(sandgem, b => b.Kind == BuildingKind.PokeMart && (b.X0, b.Y0, b.X1, b.Y1) == (186, 840, 189, 842));
        // The lab is eight tiles wide and four deep, with an entrance five tiles wide built out in front of it
        var lab = sandgem.Single(b => b.Kind == BuildingKind.Lab);
        Assert.Equal((166, 838, 173, 841), (lab.X0, lab.Y0, lab.X1, lab.Y1));
        Assert.Equal(new[] { 166, 167, 168, 169, 170 }, lab.Porch);
        Assert.False(lab.PorchIsOpen);
        Assert.Equal(new[] { (168, (string?)"RowanLab") }, lab.Doors);
        Assert.Equal(TileType.Door, map.GetGroundTile(168, 842));
        Assert.Equal(TileType.Wall, map.GetGroundTile(170, 842));
        Assert.False(map.IsSolid(171, 842));
        Assert.Equal(2, sandgem.Count(b => b.Kind == BuildingKind.House));
        Assert.All(sandgem.Where(b => b.Kind == BuildingKind.House), b => Assert.Equal(Architecture.Plaster, b.Town));
        Assert.Equal(TileType.Signpost, map.GetGroundTile(180, 842));
        Assert.False(MapStructures.IsWallSign(map, 180, 842));

        // A building is a rectangle of roof with a front row of wall and doors, two tiles or more each way, and
        // no two share a tile: Jubilife City's blocks, which touch, are each their own
        var taken = new HashSet<(int, int)>();
        foreach (var b in buildings)
        {
            Assert.True(b.Width >= 2 && b.Depth >= 2, $"the building at ({b.X0},{b.Y0}) is {b.Width} × {b.Depth}");
            for (int y = b.Y0; y <= b.Y1; y++)
                for (int x = b.X0; x <= b.X1; x++)
                {
                    Assert.True(taken.Add((x, y)), $"two buildings stand on ({x},{y})");
                    var tile = map.GetGroundTile(x, y);
                    Assert.True(y == b.Y1 ? tile is TileType.Wall or TileType.Door : tile is TileType.RoofRed or TileType.Door,
                        $"the building at ({b.X0},{b.Y0}) has {tile} at ({x},{y})");
                }
        }
        var jubilife = In("jubilife_city");
        Assert.True(jubilife.Count >= 4, $"{jubilife.Count} buildings of Jubilife City are in view");
        Assert.All(jubilife, b => Assert.True(b.Model.StartsWith("c1_") || b.Model is "pc" or "fs", b.Model));
        Assert.Contains(jubilife, b => b.Kind == BuildingKind.Apartments);

        // Each is drawn by exactly one chunk's scene
        int drawn = 0;
        for (int cy = 0; cy < map.ChunkRows; cy++)
            for (int cx = 0; cx < map.ChunkColumns; cx++)
                drawn += MapScene.BuildingsIn(map, MapScene.ChunkWindow(map, cx, cy)).Count();
        Assert.Equal(buildings.Count, drawn);
        Assert.Same(MapStructures.BuildingsOf(map), MapStructures.BuildingsOf(map));
    }


    // ------------------------------------------------------------------ the rules of the translation

    [Fact]
    public void ATileOfTheWorldBecomesATileOfTheGame()
    {
        static (TileType, bool, PropType?) Look(TerrainCover cover, TileBehavior behaviour = TileBehavior.None, bool solid = false) =>
            WorldMapBuilder.Look(cover, behaviour, solid);

        Assert.Equal((TileType.Grass, false, null), Look(TerrainCover.Grass));
        Assert.Equal((TileType.TallGrass, false, null), Look(TerrainCover.TallGrass, TileBehavior.TallGrass));
        Assert.Equal((TileType.FlowerGrass, false, null), Look(TerrainCover.Flowers));
        Assert.Equal((TileType.Path, false, null), Look(TerrainCover.Path));
        Assert.Equal((TileType.Paving, false, null), Look(TerrainCover.Paving));
        Assert.Equal((TileType.Walkway, false, null), Look(TerrainCover.Walkway));
        Assert.Equal((TileType.Sand, false, null), Look(TerrainCover.Sand, TileBehavior.Sand));

        Assert.Equal((TileType.Planks, false, null), Look(TerrainCover.Bridge));
        Assert.Equal((TileType.Stairs, false, null), Look(TerrainCover.Steps));
        Assert.Equal((TileType.Rock, false, null), Look(TerrainCover.Rock));
        Assert.Equal((TileType.Ice, false, null), Look(TerrainCover.Snow, TileBehavior.Ice));

        // A ledge is hopped whatever the data says of walking onto it, each way it can face; a door waits for its warp
        Assert.Equal((TileType.LedgeDown, false, null), Look(TerrainCover.Grass, TileBehavior.LedgeSouth, solid: true));
        Assert.Equal((TileType.LedgeLeft, false, null), Look(TerrainCover.Grass, TileBehavior.LedgeWest, solid: true));
        Assert.Equal((TileType.LedgeRight, false, null), Look(TerrainCover.Grass, TileBehavior.LedgeEast, solid: true));
        Assert.Equal((TileType.Door, true, null), Look(TerrainCover.Building, TileBehavior.Door));

        // Open water is open: it is its behaviour that asks for Surf. A blocked tile of it is a rock in the water
        Assert.Equal((TileType.Water, false, null), Look(TerrainCover.Water, TileBehavior.Sea));
        Assert.Equal((TileType.Water, false, null), Look(TerrainCover.Water, TileBehavior.River));
        Assert.Equal((TileType.Water, true, PropType.Boulder), Look(TerrainCover.Boulder, TileBehavior.Sea, solid: true));

        // A rock face is bare rock nobody walks on; a boulder, a fence and a tree stand on their tiles
        Assert.Equal((TileType.Rock, true, null), Look(TerrainCover.Cliff));
        Assert.Equal((TileType.Dirt, true, PropType.Boulder), Look(TerrainCover.Boulder, solid: true));
        Assert.Equal((TileType.Grass, true, PropType.Fence), Look(TerrainCover.Fence, solid: true));
        Assert.Equal((TileType.Tree, true, null), Look(TerrainCover.Tree, solid: true));

        // What the importer couldn't name: forest if it blocks, lawn if it doesn't
        Assert.Equal((TileType.Tree, true, null), Look(TerrainCover.Unknown, solid: true));
        Assert.Equal((TileType.Grass, false, null), Look(TerrainCover.Unknown));

        // Every cover has an answer, and only what blocks may carry a prop
        foreach (var cover in TerrainCoverCodes.All)
            foreach (bool solid in new[] { false, true })
            {
                var (_, blocks, prop) = Look(cover, TileBehavior.None, solid);
                Assert.True(prop == null || blocks, $"{cover} puts a {prop} on a tile that can be walked on");
                Assert.True(blocks || !solid, $"{cover} opens a tile the world blocks");
            }
    }

    [Fact]
    public void SomeoneComingThroughAWarpIsPutDownOneStepOutOfIt()
    {
        Assert.Equal((10, 21, Direction.Down), WorldMapBuilder.Arrival(TileBehavior.Door, 10, 20));
        Assert.Equal((10, 21, Direction.Down), WorldMapBuilder.Arrival(TileBehavior.EntranceNorth, 10, 20));
        Assert.Equal((10, 19, Direction.Up), WorldMapBuilder.Arrival(TileBehavior.EntranceSouth, 10, 20));
        Assert.Equal((9, 20, Direction.Left), WorldMapBuilder.Arrival(TileBehavior.EntranceEast, 10, 20));
        Assert.Equal((11, 20, Direction.Right), WorldMapBuilder.Arrival(TileBehavior.StairsWest, 10, 20));
        Assert.Equal((10, 20, Direction.Down), WorldMapBuilder.Arrival(TileBehavior.None, 10, 20));

        // Between the lakefront and the lake, each way puts the player beside the other's warp, never on it
        var onto = Overworld.GetWarpAt(80, 843)!;
        Assert.Equal("LakeVerity", onto.TargetMap);
        Assert.Null(Lake.GetWarpAt(onto.TargetX, onto.TargetY));
        var back = Lake.Warps.First(w => w.TargetMap == "Sinnoh");
        Assert.Equal("verity_lakefront", Overworld.AreaAt(back.TargetX, back.TargetY)!.Key);
        Assert.Equal(844, back.TargetY);
        Assert.Equal(Direction.Down, back.TargetFacing);
    }

    [Fact]
    public void ABuildingIsToldByItsModelsName()
    {
        Assert.Equal(BuildingKind.PokemonCenter, WorldModels.Of("pc")!.Kind);
        Assert.Equal(BuildingKind.PokeMart, WorldModels.Of("fs")!.Kind);
        Assert.Equal(BuildingKind.Lab, WorldModels.Of("t2_s01")!.Kind);
        Assert.Equal(BuildingKind.Apartments, WorldModels.Of("c1_b02c")!.Kind);
        Assert.Equal((BuildingKind.House, (Architecture?)Architecture.Timber), (WorldModels.Of("t1_s01")!.Kind, WorldModels.Of("t1_s01")!.Town));
        Assert.Null(WorldModels.Of("a_model_nobody_made"));

        // A model the catalogue doesn't know is a house if it is as large as one
        static ChunkProp Box(float width, float depth, float height) => new() { Width = width, Depth = depth, Height = height };
        Assert.True(WorldMapBuilder.IsBuilding(Box(5.76f, 3.88f, 5.67f)));    // a house
        Assert.False(WorldMapBuilder.IsBuilding(Box(1.25f, 0.13f, 1.81f)));   // its door
        Assert.False(WorldMapBuilder.IsBuilding(Box(1f, 1f, 4f)));            // a lamp post
        Assert.False(WorldMapBuilder.IsBuilding(Box(32f, 32f, 2f)));          // the sheet of a lake's water

        Assert.Equal("Rival", WorldMapBuilder.CharacterFor("barry"));
        Assert.Equal("StarterBriefcase", WorldMapBuilder.CharacterFor("briefcase"));
        Assert.Equal("Trainer", WorldMapBuilder.CharacterFor("someone_new"));
    }

    // ------------------------------------------------------------------ saves

    [Fact]
    public void ASaveFromTheHandMadeMapsWakesUpInTheSamePlace()
    {
        foreach (var (old, map, area) in new[]
        {
            ("TwinleafTown", "Sinnoh", "twinleaf_town"), ("Route201", "Sinnoh", "route_201"), ("SandgemTown", "Sinnoh", "sandgem_town"),
            ("Route202", "Sinnoh", "route_202"), ("LakeVerity", "LakeVerity", "lake_verity")
        })
        {
            // A save from before the import has no WorldVersion, and tiles counted from its own map's corner
            var save = JsonSerializer.Deserialize<SaveData>($"{{\"CurrentMapName\":\"{old}\",\"PlayerGridX\":9,\"PlayerGridY\":7}}")!;
            var spot = save.Place();
            Assert.Equal(map, spot.Map);
            Assert.Equal(area, MapNamed(map).AreaAt(spot.X, spot.Y)?.Key);
            Assert.True(MapNamed(map).IsWalkable(spot.X, spot.Y), $"a save on {old} wakes up on a blocked tile ({spot.X},{spot.Y})");
            Assert.Null(MapNamed(map).GetWarpAt(spot.X, spot.Y));
        }

        // A room is where it was
        var indoors = new SaveData { CurrentMapName = "PlayerHouse", PlayerGridX = 4, PlayerGridY = 5, PlayerFacing = Direction.Up };
        Assert.Equal(0, indoors.WorldVersion);
        Assert.Equal(new MapSpot("PlayerHouse", 4, 5, Direction.Up), indoors.Place());

        // A save of today says so, and keeps its own tiles: the lake's name is the old map's, its tiles are not
        var today = new SaveData { CurrentMapName = "LakeVerity", PlayerGridX = 40, PlayerGridY = 50, PlayerFacing = Direction.Left, WorldVersion = SaveData.ImportedWorld };
        var read = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(today))!;
        Assert.Equal(new MapSpot("LakeVerity", 40, 50, Direction.Left), read.Place());
    }

    // ------------------------------------------------------------------ streaming

    [Fact]
    public void TheCameraKeepsABlockOfThreeChunksByThree()
    {
        var map = Overworld;
        var block = WorldRenderer.ChunksAround(map, 116.5f, 886.5f, WorldRenderer.ChunkReach).ToList();
        Assert.Equal(9, block.Count);
        Assert.Equal(((2, 26), (3, 27), (4, 28)), (block[0], block[4], block[8]));

        // At the map's edge the block is cut off, and a camera past the edge still gets the corner's
        Assert.Equal(4, WorldRenderer.ChunksAround(map, 0.5f, 0.5f, 1).Count());
        Assert.Equal(4, WorldRenderer.ChunksAround(map, 959.5f, 959.5f, 1).Count());
        Assert.Equal(new[] { (0, 0), (1, 0), (0, 1), (1, 1) }, WorldRenderer.ChunksAround(map, -40f, -40f, 1));
        Assert.Equal(6, WorldRenderer.ChunksAround(Lake, 46f, 53f, 1).Count());   // the lake is three chunks by two

        // A chunk's window is its own tiles
        Assert.Equal(new TileWindow(96, 864, 32, 32), MapScene.ChunkWindow(map, 3, 27));
        Assert.True(MapScene.ChunkWindow(map, 3, 27).Contains(116, 886));

        // Nothing in the block is freed while the camera is anywhere in its middle chunk: the furthest edge of a
        // neighbour's window is never further than the distance chunks are kept to
        Assert.True(WorldRenderer.ChunkKeepTiles >= Map.ChunkTiles);
    }

    /// <summary>
    /// Chunks are baked one at a time on other threads, in any order. Their ground must meet without a seam, so
    /// a chunk's ground can depend only on the map and on where its texels are in the world: baking two chunks
    /// at once gives the same texels as baking each alone.
    /// </summary>
    [Fact]
    public void AChunksGroundIsTheSameHoweverMuchIsBakedWithIt()
    {
        var map = Overworld;
        const int texels = Map.ChunkTiles * 32;

        // Route 201 above Twinleaf Town (grass, path, ledges, tall grass, the town's pond), then two chunks side by side
        foreach (var (first, second) in new[] { ((3, 26), (3, 27)), ((3, 27), (4, 27)) })
        {
            var a = MapScene.ChunkWindow(map, first.Item1, first.Item2);
            var b = MapScene.ChunkWindow(map, second.Item1, second.Item2);
            bool stacked = a.X == b.X;
            var both = new TileWindow(a.X, a.Y, stacked ? a.Width : a.Width + b.Width, stacked ? a.Height + b.Height : a.Height);

            var groundA = PixelGround.Bake(map, a, PixelGround.ChunkPad, worldSeeds: true, out var maskA);
            var groundB = PixelGround.Bake(map, b, PixelGround.ChunkPad, worldSeeds: true, out var maskB);
            var ground = PixelGround.Bake(map, both, PixelGround.ChunkPad, worldSeeds: true, out var mask);
            Assert.Equal((texels, texels), (groundA.Width, groundA.Height));
            Assert.Equal(stacked ? (texels, texels * 2) : (texels * 2, texels), (ground.Width, ground.Height));

            int differing = 0, maskDiffering = 0;
            for (int y = 0; y < ground.Height; y++)
                for (int x = 0; x < ground.Width; x++)
                {
                    bool inA = stacked ? y < texels : x < texels;
                    int lx = inA || stacked ? x : x - texels, ly = inA || !stacked ? y : y - texels;
                    var part = inA ? groundA : groundB;
                    var whole = ground.Get(x, y);
                    var alone = part.Get(lx, ly);
                    if (whole.R != alone.R || whole.G != alone.G || whole.B != alone.B || whole.A != alone.A) differing++;

                    var partMask = inA ? maskA : maskB;
                    if (mask == null || partMask == null) continue;
                    var wholeMask = mask.Get(x, y);
                    var aloneMask = partMask.Get(lx, ly);
                    if (wholeMask.R != aloneMask.R || wholeMask.A != aloneMask.A) maskDiffering++;
                }
            Assert.True(differing == 0, $"chunks {first} and {second}: {differing} texels of ground differ when baked together");
            Assert.True(maskDiffering == 0, $"chunks {first} and {second}: {maskDiffering} texels of the water mask differ when baked together");
        }
    }
}
