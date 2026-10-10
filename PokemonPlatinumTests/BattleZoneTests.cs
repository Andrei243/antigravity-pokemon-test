using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The Battle Zone's outdoors as plan 01 · M10, part a opened them: the Fight Area, the Survival Area and the Resort
/// Area with Routes 225 to 230 between them, and Route 224 behind Victory Road's back rooms.
/// </summary>
public class BattleZoneTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static World Sinnoh => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Sinnoh"];

    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);

    private static string? AreaKey(Map map, int x, int y) => map.AreaAt(x, y)?.Key;

    /// <summary>The Battle Zone's towns and routes, from the Fight Area outward, and Route 224.</summary>
    private static readonly string[] Outdoors =
    {
        "fight_area", "route_225", "survival_area", "route_226", "route_227", "route_228", "route_229", "route_230", "resort_area", "route_224"
    };

    private static readonly string[] BackRooms = { "victory_road_1f_room_1", "victory_road_1f_room_2", "victory_road_1f_room_3" };

    private static readonly (int X, int Y)[] Steps = { (0, -1), (1, 0), (-1, 0), (0, 1) };

    private static IEnumerable<NPC> PeopleOf(Map map, string area) => map.Everyone.Where(n => AreaKey(map, n.GridX, n.GridY) == area);

    /// <summary>The whole game walked from its start and from where only the story takes the player (the ship's landing among them).</summary>
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked = new(() =>
        WorldWalk.From(MapNamed, WorldWalk.StoryArrivals.Prepend(RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!).ToArray()));

    // ------------------------------------------------------------------ open, and come to

    [Fact]
    public void TheBattleZonesTownsAndRoutesAreOpen()
    {
        foreach (string key in Outdoors.Concat(BackRooms))
        {
            Assert.True(Sinnoh.IsOpen(key), $"{key} isn't open");
            Assert.NotNull(Sinnoh.Overlay(key));
        }
        foreach (string key in Outdoors)
        {
            var area = Overworld.FindArea(key);
            Assert.True(area is { Open: true }, key);
            // The Battle Zone's forests are broad-leaved, and its houses its own
            Assert.Equal(TreeStyle.Round, area!.Trees);
            Assert.Equal(Architecture.Resort, area.Architecture);
        }
        Assert.Equal("Fight Area", Overworld.DisplayNameAt(647, 431));
        Assert.Equal("Survival Area", Overworld.DisplayNameAt(659, 340));
        Assert.Equal("Resort Area", Overworld.DisplayNameAt(802, 474));
    }

    [Fact]
    public void EverythingInTheBattleZoneCanBeComeTo()
    {
        var reached = Walked.Value;
        var missed = new List<string>();
        var places = Outdoors.Select(key => (Key: key, Map: Overworld)).Concat(BackRooms.Select(key => (Key: key, Map: BuiltMaps.Value[Sinnoh.MapOf(key)!.Name])));
        foreach (var (key, map) in places)
        {
            Assert.True(reached.TryGetValue(map, out var reach), $"{map.Name} can't be reached");
            bool Beside(int x, int y) => Steps.Any(s => reach!.Contains((x + s.X, y + s.Y)));
            Assert.True(reach!.Any(t => AreaKey(map, t.X, t.Y) == key), $"nobody can come to {key}");

            foreach (var npc in PeopleOf(map, key))
                if (!Beside(npc.GridX, npc.GridY) && !reach.Contains((npc.GridX, npc.GridY)))
                    missed.Add($"{key}: {npc.Key ?? npc.Name} at ({npc.GridX},{npc.GridY})");
            foreach (var (x, y) in map.Signboards.Keys.Where(t => AreaKey(map, t.Item1, t.Item2) == key))
                if (!Beside(x, y)) missed.Add($"{key}: the sign at ({x},{y})");
            foreach (var (x, y) in map.HiddenItems.Keys.Where(t => AreaKey(map, t.Item1, t.Item2) == key))
                if (!Beside(x, y) && !reach.Contains((x, y))) missed.Add($"{key}: the hidden {map.HiddenItems[(x, y)].Item} at ({x},{y})");
            foreach (var warp in map.Warps.Where(w => AreaKey(map, w.SourceX, w.SourceY) == key))
                if (!reach.Contains((warp.SourceX, warp.SourceY))) missed.Add($"{key}: the way to {warp.TargetMap} at ({warp.SourceX},{warp.SourceY})");
        }
        Assert.True(missed.Count == 0, "Out of reach: " + string.Join("; ", missed));

        // Nobody walks out of what is built (the island's other places are part b's, PostGameTests)
        Assert.All(reached[Overworld], t => Assert.True(Overworld.AreaAt(t.X, t.Y)!.Open));
    }

    [Fact]
    public void TheShipLandsOnTheFightAreasPierAndItsSailorSailsBack()
    {
        var map = Overworld;
        // Where the original's TakeShipFromSnowpoint puts the player down, facing east: on the pier's planks
        var landing = Assert.Single(WorldWalk.StoryArrivals, s => s.Map == "Sinnoh" && AreaKey(map, s.X, s.Y) == "fight_area");
        Assert.Equal((623, 434, Direction.Right), (landing.X, landing.Y, landing.Facing));
        Assert.Equal("fight_area", AreaKey(map, landing.X, landing.Y));
        Assert.Equal(TileType.Planks, map.GetGroundTile(landing.X, landing.Y));
        Assert.True(map.IsWalkable(landing.X, landing.Y));

        // The ferry keeps off the planks it is moored to: the sailor stands on the pier beside it, in plain sight
        var sailor = Assert.Single(PeopleOf(map, "fight_area"), n => n.Script == "Ferry");
        Assert.Equal((619, 435), (sailor.GridX, sailor.GridY));
        Assert.Equal(TileType.Planks, map.GetGroundTile(sailor.GridX, sailor.GridY));
        var boats = map.Props.Where(p => p.Type == PropType.Boat && AreaKey(map, p.X, p.Y) == "fight_area").ToList();
        Assert.Equal(4, boats.Count);
        Assert.DoesNotContain(boats, b => b.Covers(sailor.GridX, sailor.GridY));
        Assert.Contains(boats, b => b.Model == "c09_s02" && b.Y == sailor.GridY + 1);

        // He sails to Snowpoint City's quay, beside the ship's sailor there
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (sailor.GridX + 1, sailor.GridY), PlayerFacing = Direction.Left };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.For(sailor)!, "fight_area")!, sailor);
        runner.RunToEnd();
        Assert.Contains(host.Log, l => l.StartsWith("warp Sinnoh"));
        Assert.Equal("snowpoint_city", AreaKey(map, host.PlayerTile.X, host.PlayerTile.Y));
        Assert.True(map.IsWalkable(host.PlayerTile.X, host.PlayerTile.Y));
        Assert.Null(map.GetNpcAt(host.PlayerTile.X, host.PlayerTile.Y));
        Assert.Contains(map.Everyone, n => n.Key == "sailor_ss_spiral" && Math.Abs(n.GridX - host.PlayerTile.X) + Math.Abs(n.GridY - host.PlayerTile.Y) == 1);

        // Saying no keeps the player on the pier
        var staying = new HeadlessScriptHost { Map = map, PlayerTile = (sailor.GridX + 1, sailor.GridY) };
        staying.Answers.Enqueue(1);
        runner = new ScriptRunner(ScriptLibrary.Default, staying);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.For(sailor)!, "fight_area")!, sailor);
        runner.RunToEnd();
        Assert.DoesNotContain(staying.Log, l => l.StartsWith("warp"));
    }

    [Fact]
    public void VictoryRoadsCollectorStandsAsideOnceTheHallOfFameIsEntered()
    {
        var floor = BuiltMaps.Value["VictoryRoad1F"];
        var collector = Assert.Single(floor.Everyone, n => n.Key == "collector");
        Assert.Equal("FLAG_HIDE_VICTORY_ROAD_1F_COLLECTOR", collector.HiddenBy);
        // He stands in the way into the back rooms, which leads on through three of them to Route 224
        var way = Assert.Single(floor.Warps, w => w.TargetMap == "VictoryRoad1FRoom1");
        Assert.Equal((collector.GridX, collector.GridY), (way.SourceX, way.SourceY));
        Assert.Contains(BuiltMaps.Value["VictoryRoad1FRoom1"].Warps, w => w.TargetMap == "VictoryRoad1FRoom2");
        Assert.Contains(BuiltMaps.Value["VictoryRoad1FRoom2"].Warps, w => w.TargetMap == "VictoryRoad1FRoom3");
        var out_ = Assert.Single(BuiltMaps.Value["VictoryRoad1FRoom3"].Warps, w => w.TargetMap == "Sinnoh");
        Assert.Equal("route_224", AreaKey(Overworld, out_.TargetX, out_.TargetY));
        Assert.Equal("VictoryRoad1FRoom3", Assert.Single(Overworld.Warps, w => AreaKey(Overworld, w.SourceX, w.SourceY) == "route_224").TargetMap);

        // The floor's arrival script moves him only for a Champion
        var story = new StoryState();
        void Arrive()
        {
            var runner = new ScriptRunner(ScriptLibrary.Default, new HeadlessScriptHost(story));
            runner.Start(ScriptLibrary.Default.In("victory_road_1f", ScriptLibrary.OnEnter)!);
            runner.RunToEnd();
        }
        Arrive();
        Assert.False(story.Has("FLAG_HIDE_VICTORY_ROAD_1F_COLLECTOR"));
        story.Set("FLAG_GAME_COMPLETED");
        Arrive();
        Assert.True(story.Has("FLAG_HIDE_VICTORY_ROAD_1F_COLLECTOR"));
    }

    // ------------------------------------------------------------------ doors, gates and rooms

    [Fact]
    public void EachTownHasItsCenterAndTheTwoWithAMartTheirs()
    {
        var map = Overworld;
        // The Resort Area has no Mart (the original's is one of its unused maps)
        foreach (var (town, room) in new[]
                 {
                     ("fight_area", "FightAreaPokemonCenter"), ("fight_area", "FightAreaPokeMart"), ("survival_area", "SurvivalAreaPokemonCenter"),
                     ("survival_area", "SurvivalAreaPokeMart"), ("resort_area", "ResortAreaPokemonCenter")
                 })
        {
            var door = Assert.Single(map.Warps, w => w.TargetMap == room);
            Assert.Equal(town, AreaKey(map, door.SourceX, door.SourceY));
            var inside = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, room + ".json")).ToMap();
            Assert.Equal(room, inside.Name);
            Assert.True(inside.IsWalkable(door.TargetX, door.TargetY));
            var exit = Assert.Single(inside.Warps);
            Assert.Equal(("Sinnoh", door.SourceX, door.SourceY + 1), (exit.TargetMap, exit.TargetX, exit.TargetY));
            Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(room)!.Id);
            Assert.Contains(inside.NPCs, n => room.EndsWith("Mart") ? n.IsPokeMartClerk : n.IsHealingNurse);
            // Nobody stands in the doorway or in anything solid
            Assert.All(inside.NPCs.Where(n => !n.IsPCTerminal), n => Assert.False(inside.IsSolid(n.GridX, n.GridY) && !n.IsHealingNurse && !n.IsPokeMartClerk, $"{room}: {n.Name}"));
            Assert.Null(inside.GetNpcAt(door.TargetX, door.TargetY));
        }

        // A Battle Zone Mart has the common counter alone, as in Platinum
        foreach (string mart in new[] { "FightAreaPokeMart", "SurvivalAreaPokeMart" })
        {
            var clerk = Assert.Single(GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, mart + ".json")).ToMap().NPCs, n => n.IsPokeMartClerk);
            Assert.Null(clerk.Mart);
        }

        // Fly lands before each Center once the town has been come to
        var story = new StoryState();
        bool Open(string key) => map.FindArea(key)?.Open == true;
        Assert.DoesNotContain(SpawnLocations.FlyDestinations(story, Open), s => s.Area is "fight_area" or "survival_area" or "resort_area");
        foreach (string town in new[] { "fight_area", "survival_area", "resort_area" })
        {
            var spot = SpawnLocations.ArrivedIn(town)!;
            story.Set(spot.ArrivalFlag);
            Assert.Contains(SpawnLocations.FlyDestinations(story, Open), s => s.Area == town);
            Assert.True(map.IsWalkable(spot.X, spot.Y), $"Fly would land in something solid in {town}");
            Assert.Equal(spot.Room, map.GetWarpAt(spot.X, spot.Y - 1)?.TargetMap);
        }
    }

    [Theory]
    [InlineData("fight_area", "route_225")]
    [InlineData("route_225", "fight_area")]
    [InlineData("route_226", "route_228")]
    [InlineData("route_228", "route_226")]
    public void AGateHouseOfTheBattleZoneIsWalkedThrough(string from, string to)
    {
        var map = Overworld;
        var ways = map.Warps.Where(w => w.TargetMap == "Sinnoh" && AreaKey(map, w.SourceX, w.SourceY) == from && AreaKey(map, w.TargetX, w.TargetY) == to).ToList();
        Assert.Equal(2, ways.Count);
        Assert.All(ways, w =>
        {
            Assert.True(map.IsWalkable(w.TargetX, w.TargetY), $"the way out at ({w.TargetX},{w.TargetY}) is blocked");
            Assert.Null(map.GetWarpAt(w.TargetX, w.TargetY));
        });
    }

    [Fact]
    public void TheDoorsOfWhatIsNotBuiltYetStayShut()
    {
        var map = Overworld;
        // The Battleground (the story's), the Villa (plan 06 · R16b) and the Rock Peak Ruins (the story's). The Battle
        // Frontier's gate and the Ribbon Syndicate's door are open since part b (PostGameTests)
        foreach (var (x, y) in new[] { (652, 338), (822, 469), (785, 340) })
        {
            Assert.Null(map.GetWarpAt(x, y));
            Assert.True(map.IsSolid(x, y), $"the way in at ({x},{y}) can be walked into");
        }
        // The Battleground's door says why, read where it stands
        Assert.Equal("BattlegroundDoor", map.TileScripts[(652, 338)]);
        var host = new HeadlessScriptHost { Map = map };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("BattlegroundDoor", "survival_area")!);
        runner.RunToEnd();
        Assert.Contains(host.Transcript, line => line.Text.Contains("Members only"));

        // The story's people wait for the story: the blockades, the rival, Buck, the Leaders at the Villa
        Assert.DoesNotContain(Outdoors.SelectMany(key => PeopleOf(map, key)), n => n.Key is "rival" or "buck" or "volkner" or "flint" or "palmer" or "ace_trainer_m_2"
            or "black_belt_2" or "marley" or "prof_oak" or "shaymin" or "crasher_wake" or "school_kid_m" or "prof_rowan");
    }

    // ------------------------------------------------------------------ people, trainers, items, wild Pokémon, weather

    [Fact]
    public void TheBattleZonesTrainersArePlatinums()
    {
        var map = Overworld;
        NPC Trainer(Map on, string key) => Assert.Single(on.Everyone, n => n.Key == key && n.IsTrainer);
        string Team(NPC npc) => string.Join(", ", npc.TrainerData!.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"));

        // res/trainers/data of the decompilation, by way of trainers.json
        Assert.Equal("Metagross 60", Team(Trainer(map, "ace_trainer_jamie")));
        Assert.Equal("Tauros 61", Team(Trainer(map, "ace_trainer_saul")));
        Assert.Equal("Tyrogue 54, Makuhita 54, Machamp 57", Team(Trainer(map, "black_belt_davon")));
        Assert.Equal("Luvdisc 52, Lapras 54", Team(Trainer(map, "swimmer_joanna")));
        Assert.Equal("Dusknoir 56", Team(Trainer(BuiltMaps.Value["VictoryRoad1FRoom2"], "psychic_deandre")));
        Assert.Equal(("Jamie", "Ace Trainer", 6), (Trainer(map, "ace_trainer_jamie").Name, Trainer(map, "ace_trainer_jamie").TrainerData!.TrainerClass, Trainer(map, "ace_trainer_jamie").TrainerData!.SightRange));

        // Every trainer the original stands on these routes is in the game, with a team and two lines of ours
        int trainers = 0;
        foreach (var (key, on) in Outdoors.Select(k => (k, map)).Concat(BackRooms.Select(k => (k, BuiltMaps.Value[Sinnoh.MapOf(k)!.Name]))))
        {
            var file = Sinnoh.Area(key)!;
            foreach (var o in file.Objects.Where(o => o.Trainer != null && on.AreaAt(o.X, o.Z)?.Key == key))
            {
                var npc = Assert.Single(on.Everyone, n => n.Key == o.Id && (n.GridX, n.GridY) == (o.X, o.Z));
                Assert.True(npc.IsTrainer, $"{key}: {o.Id} doesn't battle");
                Assert.NotEmpty(npc.TrainerData!.Party.Members);
                Assert.All(npc.TrainerData.Party.Members, p => Assert.InRange(p.Level, 47, 62));
                Assert.False(string.IsNullOrWhiteSpace(npc.TrainerData.DialogueBefore) || string.IsNullOrWhiteSpace(npc.TrainerData.DialogueAfter), $"{key}: {o.Id} has nothing to say");
                Assert.StartsWith(npc.TrainerData.TrainerClass + " " + npc.Name + ":", npc.TrainerData.DialogueBefore);
                trainers++;
            }
        }
        Assert.True(trainers >= 45, $"only {trainers} trainers in the Battle Zone");

        // The towns' people have lines of ours, and those who give something give it once
        foreach (string town in new[] { "fight_area", "survival_area", "resort_area" })
            Assert.True(PeopleOf(map, town).Count(n => !n.IsThing && (n.DialogLines.Count > 0 || n.Script != null)) >= 4, town);
        var mart = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "FightAreaPokeMart.json")).ToMap();
        foreach (var (on, file, script, flag, item) in new[]
                 {
                     (map, "fight_area", "Fisherman", "FLAG_RECEIVED_SUPER_ROD", "Super Rod"), (map, "route_229", "Pokefan", "FLAG_RECEIVED_ROUTE_229_NUGGET", "Nugget"),
                     (mart, "FightAreaPokeMart", "Socialite", "FLAG_RECEIVED_FIGHT_AREA_MART_SCOPE_LENS", "Scope Lens")
                 })
        {
            var giver = Assert.Single(on.Everyone, n => n.Script == script && (on != map || AreaKey(on, n.GridX, n.GridY) == file));
            var host = new HeadlessScriptHost { Map = on, PlayerTile = (giver.GridX, giver.GridY + 1), PlayerFacing = Direction.Up };
            for (int time = 0; time < 2; time++)
            {
                var runner = new ScriptRunner(ScriptLibrary.Default, host);
                runner.Start(ScriptLibrary.Default.Find(script, file)!, giver);
                runner.RunToEnd();
            }
            Assert.True(host.Story.Has(flag));
            Assert.Equal(item == "Nugget" ? 2 : 1, host.Bag.GetQuantity(ItemDatabase.Get(item)!));
        }
    }

    [Fact]
    public void ItemsBerriesAndWildPokemonAreTheOriginals()
    {
        var map = Overworld;
        NPC Ball(string area, string key) => Assert.Single(PeopleOf(map, area), n => n.Key == key && n.IsItemBall);
        Assert.Equal("Dubious Disc", Ball("route_225", "item_dubious_disc").Item);
        Assert.Equal("Razor Claw", Ball("route_224", "item_razor_claw").Item);
        Assert.Equal("Rare Candy", Ball("survival_area", "item_rare_candy").Item);
        Assert.Equal("Reaper Cloth", Ball("route_229", "item_reaper_cloth").Item);
        Assert.Equal("FLAG_OBTAINED_ROUTE_228_SHINY_STONE", Ball("route_228", "item_shiny_stone").HiddenBy);
        Assert.StartsWith("TM", Ball("route_226", "item_tm53").Item);

        // Hidden items, soft soil and the obstacles come with the areas
        foreach (string route in new[] { "route_224", "route_225", "route_226", "route_227", "route_228", "route_230" })
            Assert.Contains(map.HiddenItems.Keys, t => AreaKey(map, t.X, t.Y) == route);
        foreach (string area in new[] { "fight_area", "resort_area", "route_224", "route_225", "route_226", "route_228", "route_229", "route_230" })
            Assert.Equal(4, PeopleOf(map, area).Count(n => n.IsBerrySoil));
        Assert.Equal(8, PeopleOf(map, "route_225").Count(n => n.NpcType == NPC.CutTreeType));
        Assert.Equal(4, PeopleOf(map, "route_228").Count(n => n.NpcType == NPC.CrackedRockType));
        Assert.Equal(10, BuiltMaps.Value["VictoryRoad1FRoom2"].Everyone.Count(n => n.NpcType == NPC.BoulderType));

        // The grass and the water have Platinum's tables (encounters_<area>.json)
        string[] Land(string key) => map.FindArea(key)!.WildEncounters.Select(e => e.SpeciesName).Distinct().ToArray();
        Assert.Contains("Hippowdon", Land("route_228"));
        Assert.Contains("Cacturne", Land("route_228"));
        Assert.Contains("Camerupt", Land("route_227"));
        Assert.Contains("Skarmory", Land("route_227"));
        Assert.Contains("Fearow", Land("route_225"));
        Assert.Contains("Volbeat", Land("route_229"));
        Assert.Contains("Floatzel", Land("route_224"));
        Assert.All(new[] { "route_224", "route_225", "route_226", "route_227", "route_228", "route_229", "route_230" }, key =>
        {
            var area = map.FindArea(key)!;
            Assert.Equal(WorldAreaFile.LandSlotWeights.Length, area.WildEncounters.Count);
            Assert.All(area.WildEncounters, e => Assert.InRange(e.MinLevel, 47, 54));
        });
        Assert.Empty(Land("fight_area"));
        // Something big lurks in the Resort Area's pond, as its swimmer says
        Assert.Equal(new[] { "Golduck", "Psyduck" }, map.FindArea("resort_area")!.WaterEncounters.Select(e => e.SpeciesName).Distinct().OrderBy(n => n));
    }

    [Fact]
    public void Route228HasItsSandstormAndRoute227ItsAsh()
    {
        var map = Overworld;
        FieldWeather WeatherOf(string key)
        {
            var b = map.AreaBounds(key)!.Value;
            return map.WeatherAt(b.X + 1, b.Y + 1);
        }
        Assert.Equal(FieldWeather.Sandstorm, WeatherOf("route_228"));
        Assert.Equal(FieldWeather.Ash, WeatherOf("route_227"));
        foreach (string clear in new[] { "fight_area", "survival_area", "resort_area", "route_224", "route_225", "route_226", "route_229", "route_230" })
            Assert.Equal(FieldWeather.Clear, WeatherOf(clear));
        // Route 228 is sand underfoot, with its slopes for bicycles
        Assert.Contains(Walked.Value[map], t => AreaKey(map, t.X, t.Y) == "route_228" && map.GetGroundTile(t.X, t.Y) == TileType.Sand);
        Assert.Contains(Enumerable.Range(0, 96 * 32).Select(i => (X: 768 + i % 32, Y: 320 + i / 32)), t => map.BehaviourAt(t.X, t.Y) is TileBehavior.BikeRampEast or TileBehavior.BikeRampWest
            or TileBehavior.BikeSlopeTop or TileBehavior.BikeSlopeBottom or (>= TileBehavior.BikeBridgeNorthSouth and <= TileBehavior.BikeBridgeEastWestOverSand));
    }

    // ------------------------------------------------------------------ what stands there

    [Fact]
    public void TheBattleZonesStandInsOfThisPartAreGone()
    {
        var map = Overworld;
        foreach (string model in new[] { "c11_o01", "d32_s06", "t6_s01", "t7_s01", "t7_s03" })
            Assert.Null(WorldModels.Of(model)!.StandInUntil);
        // Part b built the rest, Stark Mountain's two and the Battle Frontier's: no model has a stand-in any more
        Assert.DoesNotContain(WorldModels.All, m => m.StandInUntil != null);

        // The Fight Area's quay is stacked with containers, in more than one colour, each on the tiles its model blocks
        var containers = map.Props.Where(p => p.Type == PropType.Container).ToList();
        Assert.Equal(12, containers.Count);
        Assert.All(containers, c =>
        {
            Assert.Equal(("fight_area", 3, 3, "c11_o01"), (AreaKey(map, c.X, c.Y), c.Width, c.Depth, c.Model));
            Assert.True(map.IsSolid(c.X + 1, c.Y + 1));
        });
        Assert.True(containers.Select(c => Landmarks.ContainerPaintAt(c.X, c.Y)).Distinct().Count() >= 3);
        Assert.DoesNotContain(map.Props, p => p.Type == PropType.Hedge && AreaKey(map, p.X, p.Y) == "fight_area");

        // A fountain stands in each of the two pools before the Battle Frontier's gate, on water
        var fountains = map.Props.Where(p => p.Type == PropType.GrandFountain).ToList();
        Assert.Equal(2, fountains.Count);
        Assert.All(fountains, f =>
        {
            int cx = f.X + f.Width / 2, cy = f.Y + f.Depth / 2;
            Assert.Equal(TileType.Water, map.GetGroundTile(cx, cy));
            Assert.True(map.IsSolid(cx, cy));
            Assert.Equal(TileType.Water, map.GetGroundTile(f.X, cy));
        });
        Assert.True(fountains[0].X < 654 != fountains[1].X < 654, "the fountains stand either side of the avenue");

        // The Resort Area's two pools, set in paving; a swimmer stands in one, in water to his chest
        var pools = map.Props.Where(p => p.Type == PropType.Pool).ToList();
        Assert.Equal(2, pools.Count);
        Assert.All(pools, p => Assert.Equal("resort_area", AreaKey(map, p.X, p.Y)));
        Assert.All(pools, p => Assert.Equal(TileType.Paving, map.GetGroundTile(p.X + 1, p.Y + 1)));
        var swimmer = Assert.Single(PeopleOf(map, "resort_area"), n => n.Key == "swimmer_m");
        Assert.Contains(pools, p => p.Covers(swimmer.GridX, swimmer.GridY));
        Assert.Equal(WorldMapBuilder.PoolDepth, swimmer.Wading);
        Assert.False(map.IsSolid(swimmer.GridX, swimmer.GridY));
        Assert.Null(swimmer.Movement);
        Assert.All(map.Everyone.Where(n => n != swimmer), n => Assert.Equal(0, n.Wading));

        // The Ribbon Syndicate and the Battleground are buildings with their names over the door
        var syndicate = Assert.Single(map.PlacedBuildings, b => b.Model == "t7_s01");
        Assert.Equal((BuildingKind.Hall, "RIBBONS", 2), (syndicate.Kind, syndicate.Sign, syndicate.Storeys));
        var style = BuildingArt.StyleOf(syndicate, map.ArchitectureAt(syndicate.X0, syndicate.Y0));
        Assert.True(style.Veranda);
        Assert.Equal((BuildingArt.RibbonRose, RoofShape.Hip, WallKind.Clapboard), (style.RoofColor, style.Roof, style.Wall));
        Assert.True(syndicate.Porch.Count >= 6 && syndicate.PorchIsOpen, "the veranda runs along its front, either side of the way in");
        var battleground = Assert.Single(map.PlacedBuildings, b => b.Model == "t6_s01" && !b.Annex);
        Assert.Equal((BuildingKind.Shop, "BATTLE"), (battleground.Kind, battleground.Sign));
        Assert.False(BuildingArt.StyleOf(battleground, Architecture.Resort).Veranda);
    }

    [Fact]
    public void AVerandaIsGlazedAndAContainerRibbed()
    {
        // The veranda's side: a wide window to a bay, lit after dark like a public building's
        var veranda = new BuildingStyle { Wall = WallKind.Clapboard, Roof = RoofShape.Hip, Portal = 84, Veranda = true, Accent = BuildingArt.RibbonRose };
        var glazed = new PixelCanvas(4 * BuildingArt.Bay - BuildingArt.Inset, BuildingArt.CanopyUnder(BuildingArt.PortalHeight));
        BuildingArt.PaintPorchFlank(glazed, veranda, 0);
        var plain = new PixelCanvas(glazed.Width, glazed.Height);
        BuildingArt.PaintPorchFlank(plain, veranda with { Veranda = false }, 0);
        int Lit(PixelCanvas c) => Enumerable.Range(0, c.Width * c.Height).Count(i => c.Get(i % c.Width, i / c.Width).A is > 128 and < 255);
        Assert.Equal(0, Lit(plain));
        Assert.True(Lit(glazed) > 4 * 200, "four windows of glass");
        // The canopy carries the name between two rosettes
        var canopy = new PixelCanvas(9 * BuildingArt.Bay, 22);
        BuildingArt.PaintCanopy(canopy, veranda with { Sign = SignKind.Text, SignText = "RIBBONS" }, sign: true);
        Assert.Equal(2, new[] { 0, 1 }.Count(half => Enumerable.Range(0, canopy.Width / 2 * canopy.Height)
            .Any(i => canopy.Get(half * canopy.Width / 2 + i % (canopy.Width / 2), i / (canopy.Width / 2)) is { R: 250, G: 210, B: 76 })));

        // A container: ribs all the way up its sides, doors with their bars on its end, each colour its own
        var seen = new HashSet<(byte, byte, byte)>();
        for (int paint = 0; paint < 4; paint++)
        {
            var side = new PixelCanvas(86, Landmarks.ContainerRows);
            Landmarks.PaintContainerSide(side, paint);
            var doors = new PixelCanvas(90, Landmarks.ContainerRows);
            Landmarks.PaintContainerDoors(doors, paint);
            var top = new PixelCanvas(90, 86);
            Landmarks.PaintContainerTop(top, paint);
            foreach (var canvas in new[] { side, doors, top })
                Assert.All(Enumerable.Range(0, canvas.Width * canvas.Height), i => Assert.Equal(255, canvas.Get(i % canvas.Width, i / canvas.Width).A));
            var body = side.Get(41, 30);
            Assert.True(seen.Add((body.R, body.G, body.B)), "two colours of container are the same");
            Assert.NotEqual(side.Get(40, 30), side.Get(43, 30));
            Assert.NotEqual(doors.Get(44, 30), doors.Get(46, 30));   // the seam between the leaves
        }

        // A pool's water is the pond's, inside a kerb of pale tiles
        var pool = new PixelCanvas(120, 118);
        Landmarks.PaintPool(pool);
        var kerb = pool.Get(2, 2);
        var water = pool.Get(60, 60);
        Assert.True(kerb.R > 160 && kerb.G > 160 && kerb.B > 160, "the kerb is pale");
        Assert.True(water.B > water.R + 60, "the water is blue");
    }

    [Fact]
    public void TheForestRoundTheBattleZoneIsBroadLeavedToo()
    {
        var map = Overworld;
        // West of the Fight Area the world has no chunk at all, and south of the Resort Area and between Route 225 and
        // the Survival Area it has the forest that fills it between its places, which grows pines everywhere else: here
        // it is the Battle Zone's own
        foreach (var (cx, cy) in new[] { (18, 13), (25, 15), (20, 11) })
        {
            int x = cx * Map.ChunkTiles + 16, y = cy * Map.ChunkTiles + 16;
            Assert.False(map.AreaAt(x, y)!.Open);
            Assert.Equal(TileType.Tree, map.GetGroundTile(x, y));
            Assert.Equal(TreeStyle.Round, map.TreesAt(x, y));
        }
        // Pines still where nothing is imported, in Twinleaf Town and in the filler forest south of it
        Assert.Equal(TreeStyle.Pine, map.TreesAt(2, 2));
        Assert.Equal(TreeStyle.Pine, map.TreesAt(116, 886));
        Assert.Equal(TreeStyle.Pine, map.TreesAt(3 * Map.ChunkTiles + 16, 28 * Map.ChunkTiles + 16));
    }
}
