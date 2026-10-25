using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The post-game of Hoenn's Hall of Fame (plan 21 · H12, part a): the sea floor under Route 134 and the Sealed Chamber
/// at its end, the riddle that opens the doors of the three Regis, the three caves and their braille (Dig, Rock Smash,
/// Flash and a lap of a hall), the Regis battled and their caves forgetting a fled one, and Southern Island with the
/// eon Pokémon that is not roaming Hoenn. Each scene starts as the game starts it, on maps of this class's own.
/// </summary>
public class HoennPostGameTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));

    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;
        public List<HeadlessScriptHost> Played { get; } = new();
        public HeadlessScriptHost Last => Played[^1];

        public Game(string map, int x, int y, params (string Species, string[] Moves)[] team)
        {
            foreach (var m in Hoenn.Value.BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            Story.Set("HoennHallOfFame");
            foreach (var (species, moves) in team)
            {
                var pokemon = new Pokemon(PokemonDatabase.Get(species)!, 50, new Random(Party.Members.Count));
                pokemon.Moves.Clear();
                foreach (string move in moves) pokemon.Moves.Add(new Move(MoveDatabase.Get(move)!));
                Party.Add(pokemon);
            }
            Story.SetBadges("Hoenn", 0xFF);
            Arrive(map, x, y);
        }

        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
            Map.ApplyDoors(Story.Has);
            Wandering.SendHome(Map);
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnEnter) is { } script) Play(script);
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc);
        }

        /// <summary>A tile read by facing it, or the script of a field move worked where the player stands.</summary>
        public HeadlessScriptHost Read(int x, int y, int fromX, int fromY)
        {
            Tile = (fromX, fromY);
            Assert.True(Map.TileScripts.TryGetValue((x, y), out var name), $"Nothing is read at {x},{y} of {Map.Name}");
            return Play(Scripts.Find(name!, Map.ScriptFileAt(x, y))!);
        }

        public HeadlessScriptHost Work(FieldMove move)
        {
            Tile = Tile;
            var spot = FieldMoveRules.SpotOf(Map, Tile.X, Tile.Y, Direction.Up, new Walker(TravelMode.OnFoot, Map.HeightAt(Tile.X, Tile.Y)));
            Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(move, spot, Story));
            Assert.Equal(move, spot.Braille);
            var (_, script) = Map.BrailleSpots[(Tile.X, Tile.Y)];
            return Play(Scripts.Find(script, Map.ScriptFileAt(Tile.X, Tile.Y))!);
        }

        public HeadlessScriptHost? StepAt(int x, int y)
        {
            Tile = (x, y);
            if (FieldScripts.TriggerAt(Map, x, y, Story) is not { } trigger) return null;
            return Play(Scripts.Find(trigger.Script, trigger.ScriptFile ?? Map.ScriptFileAt(x, y))!);
        }

        public HeadlessScriptHost Play(Script script, NPC? subject = null)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                Fight = _ => Fight,
                NeedsPokemon = true
            };
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Played.Add(host);
            Assert.Empty(host.Problems);
            foreach (var map in maps.Values.Distinct())
            {
                map.ApplyPresence(Story.Has);
                map.ApplyDoors(Story.Has);
            }
            Tile = host.PlayerTile;
            if (host.Log.LastOrDefault(l => l.StartsWith("warp ")) != null && host.Map is { } there && there != Map)
            {
                Map = there;
                Map.ForgetForced();
                Map.ApplyPresence(Story.Has);
                Map.ApplyDoors(Story.Has);
            }
            return host;
        }
    }

    private static readonly (string, string[])[] Diggers = { ("Swampert", new[] { "Dig", "Rock Smash", "Flash", "Surf" }) };

    [Fact]
    public void TheRegisCavesAndTheSealedChamberAreHoennsAndRegisteredInItsMaps()
    {
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        foreach (string name in new[] { "Underwater_Route134", "Underwater_SealedChamber", "SealedChamber_OuterRoom", "SealedChamber_InnerRoom", "DesertRuins", "IslandCave", "AncientTomb", "SouthernIsland_Exterior", "SouthernIsland_Interior" })
            Assert.Contains(name, hoenn.Maps);
        foreach (string key in new[] { "underwater_route_134", "underwater_sealed_chamber", "sealed_chamber_outer_room", "sealed_chamber_inner_room", "desert_ruins", "island_cave", "ancient_tomb", "southern_island_exterior", "southern_island_interior" })
            Assert.Contains(key, Hoenn.Value.Index.Areas);
    }

    [Fact]
    public void Route134DivesToItsOwnFloorWhichComesUpAgainAndLeadsOnToTheSealedChamber()
    {
        var game = new Game("Hoenn", 300, 291);
        var sea = game.Map;
        // Somewhere in the route's dark water there is a tile to dive from
        var from = Enumerable.Range(240, 100).SelectMany(x => Enumerable.Range(255, 70).Select(y => (x, y)))
            .First(t => sea.AreaAt(t.x, t.y)?.Key == "route_134" && sea.BehaviourAt(t.x, t.y) == TileBehavior.DiveWater);
        Assert.True(FieldMovement.CanDive(sea, from.x, from.y, new Walker(TravelMode.Surfing, sea.HeightAt(from.x, from.y), Moves: FieldMoves.Dive)));
        var down = sea.DiveTargetAt(from.x, from.y)!;
        Assert.Equal(("Underwater_Route134", 8, 6), (down.Map, down.X, down.Y));

        // The floor is swum and comes up in the route at one place, on water to surf on
        var floor = MapDatabase.Get("Underwater_Route134");
        Assert.True(floor.IsUnderwater);
        Assert.False(floor.IsSolid(8, 6));
        var up = floor.EmergeTargetAt(8, 6)!;
        Assert.Equal(("Hoenn", 300, 291), (up.Map, up.X, up.Y));
        Assert.True(FieldMovement.SurfacesOnto(MapDatabase.Get("Hoenn"), up.X, up.Y));
        // ... and its passage leads on to the chamber's long corridor
        Assert.Equal("Underwater_SealedChamber", floor.GetWarpAt(8, 8)!.TargetMap);
    }

    [Fact]
    public void ADiverSwimsTheDarkWaterOfTheFloorUnderRoute134ToItsPassageAndSomeoneOnFootDoesNot()
    {
        var floor = MapDatabase.Get("Underwater_Route134");
        var diver = new Walker(TravelMode.Diving, floor.HeightAt(8, 6), Moves: FieldMoves.Dive);
        int x = 8, y = 6;
        for (int i = 0; i < 2; i++)
        {
            var step = FieldMovement.Step(floor, x, y, Direction.Down, diver);
            Assert.True(step.Moves, $"the diver is stopped going down from {x},{y}: {step.Obstacle}");
            (x, y) = (step.X, step.Y);
        }
        Assert.Equal((8, 8), (x, y));
        Assert.NotNull(floor.GetWarpAt(8, 8));
        Assert.False(FieldMovement.Step(floor, 8, 6, Direction.Down, new Walker(TravelMode.OnFoot, floor.HeightAt(8, 6))).Moves);
    }

    [Fact]
    public void TheWalkOfTheWholeGameReachesTheSealedChambersRoomsAndTheCavesOfTheRegis()
    {
        var world = Hoenn.Value;
        var built = world.BuildMaps().ToDictionary(m => m.Name);
        Map MapNamed(string name) => built.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);
        var starts = new[] { RegionDatabase.Get(RegionDatabase.Hoenn)!.Start! };
        var walked = WorldWalk.From(MapNamed, starts);
        foreach (string name in new[] { "Underwater_Route134", "Underwater_SealedChamber", "SealedChamber_OuterRoom", "SealedChamber_InnerRoom", "DesertRuins", "IslandCave", "AncientTomb" })
            Assert.True(walked.Keys.Any(m => m.Name == name), $"nobody walks into {name}");
        // The chambers behind the three walls are walked too (the walk goes by the maps as they are built, doors open)
        foreach (string name in new[] { "DesertRuins", "IslandCave", "AncientTomb" })
            Assert.Contains((8, 8), walked.Single(kv => kv.Key.Name == name).Value);
        // Southern Island is come to by the ferry, which is a script's warp the walk follows
        Assert.True(walked.Keys.Any(m => m.Name == "SouthernIsland_Exterior"));
        Assert.True(walked.Keys.Any(m => m.Name == "SouthernIsland_Interior"));
    }

    [Fact]
    public void TheCorridorUnderTheChamberComesUpInTheChamberAtItsEndAndInRoute134Elsewhere()
    {
        var corridor = MapDatabase.Get("Underwater_SealedChamber");
        var end = corridor.EmergeTargetAt(12, 44)!;
        Assert.Equal(("SealedChamber_OuterRoom", 10, 19), (end.Map, end.X, end.Y));
        var elsewhere = corridor.EmergeTargetAt(7, 10)!;
        Assert.Equal(("Hoenn", 300, 291), (elsewhere.Map, elsewhere.X, elsewhere.Y));
        Assert.True(FieldMovement.SurfacesOnto(MapDatabase.Get("SealedChamber_OuterRoom"), 10, 19));

        // The chamber's pool dives back down to the corridor's end
        var room = MapDatabase.Get("SealedChamber_OuterRoom");
        Assert.Equal(TileBehavior.DiveWater, room.BehaviourAt(10, 19));
        var down = room.DiveTargetAt(10, 19)!;
        Assert.Equal(("Underwater_SealedChamber", 12, 44), (down.Map, down.X, down.Y));
        Assert.True(FieldMovement.CanDive(room, 10, 19, new Walker(TravelMode.Surfing, room.HeightAt(10, 19), Moves: FieldMoves.Dive)));
    }

    [Fact]
    public void DigBreaksTheOuterRoomsWallOnlyBeforeItAndOnlyForAPokemonThatKnowsIt()
    {
        var game = new Game("SealedChamber_OuterRoom", 10, 3, Diggers);
        // Shut until it is dug through
        Assert.True(game.Map.IsSolid(10, 2));
        var wall = game.Read(10, 2, 10, 3);
        Assert.Contains(wall.Transcript, l => l.Text.Contains("dig here"));
        Assert.False(game.Story.Has("FLAG_SYS_BRAILLE_DIG"));

        // Dig anywhere else in the room is no puzzle's (it is the cave's own way out there)
        var elsewhere = FieldMoveRules.SpotOf(game.Map, 10, 5, Direction.Up, new Walker(TravelMode.OnFoot, 0f));
        Assert.Null(elsewhere.Braille);

        game.Work(FieldMove.Dig);
        Assert.True(game.Story.Has("FLAG_SYS_BRAILLE_DIG"));
        Assert.False(game.Map.IsSolid(10, 2));
        Assert.Equal("SealedChamber_InnerRoom", game.Map.GetWarpAt(10, 2)!.TargetMap);
        Assert.Contains(game.Read(10, 2, 10, 3).Transcript, l => l.Text.Contains("big hole"));
    }

    [Theory]
    [InlineData(new[] { "Wailord", "Swampert", "Relicanth" }, true)]
    [InlineData(new[] { "Wailord", "Relicanth" }, true)]
    [InlineData(new[] { "Relicanth", "Swampert", "Wailord" }, false)]
    [InlineData(new[] { "Wailord", "Swampert" }, false)]
    [InlineData(new[] { "Swampert", "Wailord", "Relicanth" }, false)]
    [InlineData(new[] { "Wailord" }, false)]
    public void TheInnerRoomsBackWallOpensTheRegisDoorsForAWailordFirstAndARelicanthLast(string[] team, bool opens)
    {
        var game = new Game("SealedChamber_InnerRoom", 10, 5, team.Select(s => (s, new[] { "Splash" })).ToArray());
        var wall = game.Read(10, 4, 10, 5);
        Assert.Equal(opens, game.Story.Has("FLAG_REGI_DOORS_OPENED"));
        Assert.Equal(opens, wall.Transcript.Any(l => l.Text.Contains("doors opened")));
        Assert.Contains(wall.Transcript, l => l.Text.Contains("great whale"));
        // Read again, an open door is not opened twice
        if (opens) Assert.DoesNotContain(game.Read(10, 4, 10, 5).Transcript, l => l.Text.Contains("doors opened"));
    }

    [Fact]
    public void RockSmashBreaksTheDesertRuinsWallOnceTheChambersDoorsAreOpenAndRegirockIsBattledAndGoneWhenCaught()
    {
        var game = new Game("DesertRuins", 8, 21, Diggers);
        // The slabs and the shut way
        Assert.NotNull(game.Present("regirock_wall_middle"));
        Assert.True(game.Map.IsSolid(8, 20));
        // Before the chamber's doors have opened Rock Smash does nothing
        game.Work(FieldMove.RockSmash);
        Assert.False(game.Story.Has("FLAG_SYS_REGIROCK_PUZZLE_COMPLETED"));
        Assert.Contains(game.Last.Transcript, l => l.Text.Contains("nothing happened"));

        game.Story.Set("FLAG_REGI_DOORS_OPENED");
        game.Work(FieldMove.RockSmash);
        Assert.True(game.Story.Has("FLAG_SYS_REGIROCK_PUZZLE_COMPLETED"));
        Assert.Null(game.Present("regirock_wall_middle"));
        Assert.Equal("DesertRuins", game.Map.GetWarpAt(8, 20)!.TargetMap);

        // Through the way, Regirock is there until it is beaten or caught, and is run from and back with the next arrival
        game.Arrive("DesertRuins", 8, 10);
        Assert.NotNull(game.Present("regirock"));
        game.Fight = BattleOutcome.Fled;
        game.Talk("regirock");
        Assert.Contains("wildbattle Regirock 40 Fled", game.Last.Log);
        Assert.False(game.Story.Has("FLAG_DEFEATED_REGIROCK"));
        game.Arrive("DesertRuins", 8, 10);
        Assert.NotNull(game.Present("regirock"));
        game.Fight = BattleOutcome.Caught;
        game.Talk("regirock");
        Assert.True(game.Story.Has("FLAG_DEFEATED_REGIROCK"));
        game.Arrive("DesertRuins", 8, 10);
    }

    [Fact]
    public void FlashLightsTheAncientTombsWallAndRegisteelWaitsBehindIt()
    {
        var game = new Game("AncientTomb", 8, 21, Diggers);
        game.Story.Set("FLAG_REGI_DOORS_OPENED");
        Assert.Contains(game.Read(8, 20, 8, 21).Transcript, l => l.Text.Contains("let light shine"));
        game.Work(FieldMove.Flash);
        Assert.True(game.Story.Has("FLAG_SYS_REGISTEEL_PUZZLE_COMPLETED"));
        Assert.Equal("AncientTomb", game.Map.GetWarpAt(8, 20)!.TargetMap);
        game.Arrive("AncientTomb", 8, 10);
        Assert.NotNull(game.Present("registeel"));
        game.Talk("registeel");
        Assert.Contains("wildbattle Registeel 40 Won", game.Last.Log);
        Assert.True(game.Story.Has("FLAG_DEFEATED_REGISTEEL"));
    }

    [Fact]
    public void ALapOfTheIslandCavesHallByItsFourCornersAndBackOpensTheWallInOrderOnly()
    {
        var game = new Game("IslandCave", 8, 21, Diggers);
        game.Story.Set("FLAG_REGI_DOORS_OPENED");
        // Nothing runs before the wall has been read
        Assert.Null(game.StepAt(4, 22));
        game.Read(8, 20, 8, 21);
        Assert.Equal(1, game.Story.Var("VAR_ISLAND_CAVE_LAP"));

        // The corners out of order go nowhere
        Assert.Null(game.StepAt(12, 27));
        Assert.Null(game.StepAt(7, 21));
        Assert.Equal(1, game.Story.Var("VAR_ISLAND_CAVE_LAP"));
        game.StepAt(4, 22);
        game.StepAt(4, 27);
        game.StepAt(12, 27);
        Assert.Equal(4, game.Story.Var("VAR_ISLAND_CAVE_LAP"));
        // A lap given up is forgotten as the cave is left and come to again
        game.Arrive("IslandCave", 8, 28);
        Assert.Equal(0, game.Story.Var("VAR_ISLAND_CAVE_LAP"));
        Assert.Null(game.StepAt(12, 22));

        game.Read(8, 20, 8, 21);
        foreach (var (x, y) in new[] { (4, 22), (4, 27), (12, 27), (12, 22) }) game.StepAt(x, y);
        Assert.Equal(5, game.Story.Var("VAR_ISLAND_CAVE_LAP"));
        Assert.False(game.Story.Has("FLAG_SYS_BRAILLE_REGICE_COMPLETED"));
        game.StepAt(8, 21);
        Assert.True(game.Story.Has("FLAG_SYS_BRAILLE_REGICE_COMPLETED"));
        Assert.Equal(0, game.Story.Var("VAR_ISLAND_CAVE_LAP"));
        Assert.Equal("IslandCave", game.Map.GetWarpAt(8, 20)!.TargetMap);
        game.Arrive("IslandCave", 8, 10);
        game.Talk("regice");
        Assert.Contains("wildbattle Regice 40 Won", game.Last.Log);
    }

    [Fact]
    public void TheLapsCornersAreOpenGroundOfTheCave()
    {
        var cave = MapDatabase.Get("IslandCave");
        foreach (var (x, y) in new[] { (4, 22), (4, 27), (12, 27), (12, 22), (7, 21), (8, 21), (9, 21) })
            Assert.False(cave.IsSolid(x, y), $"{x},{y} of the Island Cave is no open ground");
    }

    [Fact]
    public void RouteOneHundredFiveAndRouteOneHundredElevenLeadIntoTheirCavesAndTheChambersBehindThemAreShutUntilTheirPuzzles()
    {
        var overworld = MapDatabase.Get("Hoenn");
        Assert.Equal("IslandCave", overworld.GetWarpAt(9, 282)!.TargetMap);
        Assert.Equal("DesertRuins", overworld.GetWarpAt(229, 87)!.TargetMap);
        foreach (string name in new[] { "IslandCave", "DesertRuins", "AncientTomb" })
        {
            var cave = new Game(name, 8, 28).Map;
            Assert.True(cave.IsSolid(8, 20), $"{name}'s wall is open from the start");
            Assert.Equal(name, cave.GetWarpAt(8, 20)!.TargetMap);
        }
    }

    [Fact]
    public void ANewGameHidesTheRegisAndTheEonPairAndAnOlderSaveIsGivenThemOnTheWay()
    {
        var story = new StoryState();
        StoryMigration.BeginNewGame(story, Scripts);
        foreach (string flag in new[] { "FLAG_HIDE_REGIROCK", "FLAG_HIDE_REGICE", "FLAG_HIDE_REGISTEEL", "FLAG_HIDE_SOUTHERN_ISLAND_LATIOS", "FLAG_HIDE_SOUTHERN_ISLAND_LATIAS" })
            Assert.True(story.Has(flag), flag);
        Assert.True(story.Has("FLAG_HOENN_CHAPTER_FOURTEEN"));

        // A save that never ran it: the caves' arrivals run it, and the Regi is shown by the cave itself
        var game = new Game("Hoenn", 300, 291);
        game.Story.Unset("FLAG_HOENN_CHAPTER_FOURTEEN");
        game.Story.Unset("FLAG_HIDE_REGICE");
        game.Arrive("IslandCave", 8, 28);
        Assert.True(game.Story.Has("FLAG_HOENN_CHAPTER_FOURTEEN"));
        Assert.NotNull(game.Present("regice"));
    }

    [Fact]
    public void AChampionIsGivenTheSouthernIslandPassOnceAndSailedThereBytheFerryAttendant()
    {
        var game = new Game("LilycoveCity_Harbor", 8, 11);
        var attendant = game.Map.NPCs.First(n => n.Key == "lilycove_harbor_attendant");
        Assert.Equal("FerryAttendant", FieldScripts.For(attendant));
        Assert.False(game.Story.Has("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND"));
        var talk = game.Talk("lilycove_harbor_attendant");
        Assert.True(game.Story.Has("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND"));
        Assert.Contains(talk.Log, l => l.StartsWith("warp SouthernIsland_Exterior 13 22"));
        Assert.Equal("SouthernIsland_Exterior", game.Map.Name);

        // Someone who has not entered the Hall of Fame is told she takes no passengers
        var early = new Game("LilycoveCity_Harbor", 8, 11);
        early.Story.Unset("HoennHallOfFame");
        var told = early.Talk("lilycove_harbor_attendant");
        Assert.False(early.Story.Has("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND"));
        Assert.Contains(told.Transcript, l => l.Text.Contains("isn't taking passengers"));
    }

    [Fact]
    public void TheSailorOnTheBeachTakesAChampionBackToLilycove()
    {
        var game = new Game("SouthernIsland_Exterior", 13, 22);
        game.Talk("southern_island_sailor");
        Assert.Equal("LilycoveCity_Harbor", game.Map.Name);
    }

    [Theory]
    [InlineData(0, "Latios", BattleOutcome.Won, "FLAG_DEFEATED_LATIAS_OR_LATIOS")]
    [InlineData(1, "Latias", BattleOutcome.Caught, "FLAG_CAUGHT_LATIAS_OR_LATIOS")]
    [InlineData(0, "Latios", BattleOutcome.Fled, "")]
    public void TheStoneInSouthernIslandsPondBringsDownTheEonPokemonThatIsNotRoamingHoennAtLevelFiftyWithASoulDew(int roaming, string island, BattleOutcome outcome, string flag)
    {
        var game = new Game("SouthernIsland_Interior", 13, 12, Diggers);
        game.Story.Set("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND");
        game.Story.SetVar("VAR_ROAMER_POKEMON", roaming);
        game.Fight = outcome;
        var stone = game.Read(13, 11, 13, 12);
        Assert.Contains($"wildbattle {island} 50 {outcome}", stone.Log);
        // (the one caught comes with the item it held)
        if (outcome == BattleOutcome.Caught) Assert.Equal("Soul Dew", game.Party.Members[^1].HeldItem?.Name);
        Assert.True(game.Story.Has("FLAG_ENCOUNTERED_LATIAS_OR_LATIOS"));
        if (flag.Length > 0) Assert.True(game.Story.Has(flag));
        else Assert.False(game.Story.Has("FLAG_DEFEATED_LATIAS_OR_LATIOS") || game.Story.Has("FLAG_CAUGHT_LATIAS_OR_LATIOS"));
        // On the same visit the stone is only a stone; one beaten or caught is gone for good, one run from comes with the next visit
        int battles = game.Played.Sum(h => h.Log.Count(l => l.StartsWith("wildbattle")));
        game.Read(13, 11, 13, 12);
        Assert.Equal(battles, game.Played.Sum(h => h.Log.Count(l => l.StartsWith("wildbattle"))));
        game.Story.ClearLocal();
        game.Read(13, 11, 13, 12);
        Assert.Equal(flag.Length == 0 ? battles + 1 : battles, game.Played.Sum(h => h.Log.Count(l => l.StartsWith("wildbattle"))));
    }

    [Fact]
    public void TheStoneIsOnlyAStoneWithoutTheFerrysPass()
    {
        var game = new Game("SouthernIsland_Interior", 13, 12);
        var stone = game.Read(13, 11, 13, 12);
        Assert.DoesNotContain(stone.Log, l => l.StartsWith("wildbattle"));
        Assert.False(game.Story.Has("FLAG_ENCOUNTERED_LATIAS_OR_LATIOS"));
    }
}
