using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Trick House's later puzzles (plan 21 · H12, part j; docs/mechanics/rulings.md, "The Trick House's eight
/// puzzles"): the Trick Master's hiding places and Badges, the way behind the scroll into the puzzle of the level, each
/// puzzle's mechanism (the holes and their buttons, the red and blue doors, the boulders, the Mechadolls' quiz, the
/// rotating gates, the arrows and statues, the slippery floor) and the rewards. Each scene starts as the game starts it,
/// on maps of this class's own.
/// </summary>
public class HoennTrickHouseTests
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
        public HeadlessScriptHost Last { get; private set; } = null!;
        public Queue<int> Answers { get; } = new();
        public Queue<int> Rolls { get; } = new();

        public Game()
        {
            foreach (var m in Hoenn.Value.BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 50, new Random(1)));
        }

        public Map MapOf(string name) => maps[name];

        /// <summary>Comes to a place as a warp does: its OnTransition behind the fade, then its OnEnter.</summary>
        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Map.Puzzle?.Arrive(Map, Story, new Random(1));
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnTransition) is { } transition) Play(transition);
            Settle(Map);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnEnter) is { } script) Play(script);
        }

        private void Settle(Map map)
        {
            map.ApplySwappedPlaces(Story.Var, Story.Has);
            map.ApplyPresence(Story.Has);
            map.ApplyDoors(Story.Has);
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc);
        }

        /// <summary>A script of the place the player stands in, by its name (a trigger's, a tile read).</summary>
        public HeadlessScriptHost Run(string name) => Play(Scripts.Find(name, Map.ScriptFileAt(Tile.X, Tile.Y))!);

        /// <summary>The tile in front read, as the game reads it (an overlay's <c>readAt</c>).</summary>
        public HeadlessScriptHost Read(int x, int y) => Run(Map.TileScripts[(x, y)]);

        public HeadlessScriptHost Play(Script script, NPC? subject = null)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                NeedsPokemon = true
            };
            foreach (int a in Answers) host.Answers.Enqueue(a);
            Answers.Clear();
            foreach (int r in Rolls) host.Rolls.Enqueue(r);
            Rolls.Clear();
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Assert.True(host.Problems.Count == 0, string.Join(" | ", host.Problems));
            foreach (var map in maps.Values.Distinct()) Settle(map);
            Tile = host.PlayerTile;
            if (host.Map is { } now && now != Map) Map = now;
            return Last = host;
        }
    }

    private static readonly string[] Puzzles =
    {
        "Route110_TrickHousePuzzle2", "Route110_TrickHousePuzzle3", "Route110_TrickHousePuzzle4", "Route110_TrickHousePuzzle5",
        "Route110_TrickHousePuzzle6", "Route110_TrickHousePuzzle7", "Route110_TrickHousePuzzle8"
    };

    [Fact]
    public void TheLaterPuzzlesAreOpenInHoennAndLeadToTheLastRoom()
    {
        var game = new Game();
        for (int n = 2; n <= 8; n++)
        {
            Assert.Contains(Puzzles[n - 2], RegionDatabase.Get(RegionDatabase.Hoenn)!.Maps);
            Assert.Contains($"route_110_trick_house_puzzle_{n}", Hoenn.Value.Index.Areas);
            var room = game.MapOf(Puzzles[n - 2]);
            // The door to the last room is shut until the code is written on it, then leads there
            room.ApplyDoors(game.Story.Has);
            Assert.True(room.GetWarpAt(13, 1) is null or { Shut: true });
            game.Story.Set($"FLAG_HOENN_TRICK_HOUSE_PUZZLE_{n}_UNLOCKED");
            room.ApplyDoors(game.Story.Has);
            Assert.Equal("Route110_TrickHouseEnd", room.GetWarpAt(13, 1)!.TargetMap);
            // and the way out at its foot leads back to the front room
            Assert.Equal("Route110_TrickHouseEntrance", room.GetWarpAt(0, 21)!.TargetMap);
        }
        // The last room's door back is locked: it is every puzzle's
        Assert.True(game.MapOf("Route110_TrickHouseEnd").GetWarpAt(10, 1) is null or { Shut: true });
    }

    [Fact]
    public void EachPuzzleWaitsForItsBadgeAndTheMasterHidesSomewhereNewForIt()
    {
        var game = new Game();
        game.Story.SetVar("VAR_TRICK_HOUSE_LEVEL", 1);
        game.Story.SetVar("VAR_TRICK_HOUSE_ENTRANCE_STATE", 3);
        // No Dynamo Badge: he is still at work by the scroll, and the way is shut
        game.Arrive("Route110_TrickHouseEntrance", 5, 6);
        Assert.Equal(3, game.Story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE"));
        Assert.NotNull(game.Present("trick_master"));
        Assert.True(game.Map.GetWarpAt(5, 2) is null or { Shut: true });
        // With it, he hides: not under the desk this time, but behind the potted tree
        game.Story.GiveBadge("Hoenn", "Dynamo");
        game.Arrive("Route110_TrickHouseEntrance", 5, 6);
        Assert.Equal(0, game.Story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE"));
        Assert.Null(game.Present("trick_master"));
        game.Read(6, 3);
        Assert.False(game.Story.Has("FLAG_HOENN_TRICK_HOUSE_OPEN"));
        game.Read(11, 5);
        Assert.True(game.Story.Has("FLAG_HOENN_TRICK_HOUSE_OPEN"));
        Assert.Equal(1, game.Story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE"));
        Assert.False(game.Map.GetWarpAt(5, 2)!.Shut);
        // The way behind the scroll leads into the second puzzle, not the first
        var trigger = FieldScripts.TriggerAt(game.Map, 5, 2, game.Story);
        Assert.Equal("IntoPuzzle2", trigger!.Script);
        game.Run(trigger.Script);
        Assert.Contains("warp Route110_TrickHousePuzzle2 0 20", game.Last.Log);

        // Each later level asks its own Badge, and the eighth the Hall of Fame
        foreach (var (level, badge) in new[] { (2, "Heat"), (3, "Balance"), (4, "Feather"), (5, "Mind"), (6, "Rain") })
        {
            game.Story.SetVar("VAR_TRICK_HOUSE_LEVEL", level);
            game.Story.SetVar("VAR_TRICK_HOUSE_ENTRANCE_STATE", 3);
            game.Arrive("Route110_TrickHouseEntrance", 5, 6);
            Assert.Equal(3, game.Story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE"));
            game.Story.GiveBadge("Hoenn", badge);
            game.Arrive("Route110_TrickHouseEntrance", 5, 6);
            Assert.Equal(0, game.Story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE"));
        }
        game.Story.SetVar("VAR_TRICK_HOUSE_LEVEL", 7);
        game.Arrive("Route110_TrickHouseEntrance", 5, 6);
        Assert.Equal(3, game.Story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE"));
        game.Story.Set("HoennHallOfFame");
        game.Arrive("Route110_TrickHouseEntrance", 5, 6);
        Assert.Equal(0, game.Story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE"));
        game.Read(4, 4);
        Assert.Equal("IntoPuzzle8", FieldScripts.TriggerAt(game.Map, 5, 2, game.Story)!.Script);
        // and once all eight are solved he has run out of tricks
        game.Story.SetVar("VAR_TRICK_HOUSE_LEVEL", 8);
        game.Story.SetVar("VAR_TRICK_HOUSE_ENTRANCE_STATE", 3);
        game.Arrive("Route110_TrickHouseEntrance", 5, 6);
        Assert.Equal(4, game.Story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE"));
        Assert.NotNull(game.Present("trick_master"));
    }

    [Fact]
    public void AnOlderSaveHalfwayIntoTheFirstPuzzleKeepsItsWayOpen()
    {
        var game = new Game();
        game.Story.Set("FLAG_HOENN_TRICK_HOUSE_PUZZLE_1_BEGUN");
        game.Story.SetVar("VAR_TRICK_HOUSE_ENTRANCE_STATE", 1);
        game.Arrive("Route110_TrickHouseEntrance", 5, 6);
        Assert.True(game.Story.Has("FLAG_HOENN_TRICK_HOUSE_OPEN"));
        Assert.Equal("Route110_TrickHousePuzzle1", game.Map.GetWarpAt(5, 2)!.TargetMap);
        Assert.Null(FieldScripts.TriggerAt(game.Map, 5, 2, game.Story));
    }

    [Fact]
    public void EachButtonOfTheSecondPuzzleCoversItsHoleUntilTheRoomIsLeft()
    {
        var game = new Game();
        game.Arrive("Route110_TrickHousePuzzle2", 0, 20);
        (int X, int Y)[] holes = { (1, 13), (5, 6), (7, 15), (14, 12) };
        foreach (var hole in holes) Assert.True(game.Map.IsSolid(hole.X, hole.Y), $"{hole}");
        // Buttons in any order: each covers its own hole and leaves the others
        foreach (int b in new[] { 3, 1 })
        {
            game.Run($"Button{b}");
            for (int h = 0; h < 4; h++)
                Assert.Equal(h + 1 is 3 or 1 && (b == 1 || h + 1 == 3), !game.Map.IsSolid(holes[h].X, holes[h].Y));
        }
        Assert.Equal(5, game.Story.Var("VAR_TEMP_5"));
        game.Run("Button2");
        game.Run("Button4");
        foreach (var hole in holes) Assert.False(game.Map.IsSolid(hole.X, hole.Y), $"{hole}");
        // Coming in again opens every hole
        game.Arrive("Route110_TrickHousePuzzle2", 0, 20);
        foreach (var hole in holes) Assert.True(game.Map.IsSolid(hole.X, hole.Y), $"{hole}");
    }

    [Fact]
    public void EveryButtonOfTheThirdPuzzleSwapsItsRedAndBlueDoors()
    {
        var game = new Game();
        game.Arrive("Route110_TrickHousePuzzle3", 0, 20);
        // As the room is come into the red doors are shut and the blue ones open
        (int X, int Y) red = (1, 4), blue = (1, 7);
        Assert.True(game.Map.IsSolid(red.X, red.Y));
        Assert.False(game.Map.IsSolid(blue.X, blue.Y));
        game.Run("Button1");
        Assert.False(game.Map.IsSolid(red.X, red.Y));
        Assert.True(game.Map.IsSolid(blue.X, blue.Y));
        Assert.Equal(6, game.Story.Var("VAR_TEMP_7"));
        game.Run("Button3");
        Assert.True(game.Map.IsSolid(red.X, red.Y));
        Assert.False(game.Map.IsSolid(blue.X, blue.Y));
        Assert.Equal(3, game.Story.Var("VAR_TEMP_7"));
        // The button pressed waits for another before it presses again (the room's triggers)
        Assert.Equal(1, game.Story.Var("VAR_TEMP_3"));
        Assert.Equal(0, game.Story.Var("VAR_TEMP_1"));
        game.Arrive("Route110_TrickHousePuzzle3", 0, 20);
        Assert.True(game.Map.IsSolid(red.X, red.Y));
        Assert.Equal(0, game.Story.Var("VAR_TEMP_9"));
    }

    [Fact]
    public void TheFourthPuzzlesBouldersArePushedWithStrength()
    {
        var game = new Game();
        game.Arrive("Route110_TrickHousePuzzle4", 0, 20);
        Assert.Equal(10, game.Map.Everyone.Count(n => n.Obstacle is not null));
    }

    [Fact]
    public void AMechadollLetsARightAnswerByAndSendsAWrongOneBackToTheStart()
    {
        var game = new Game();
        game.Arrive("Route110_TrickHousePuzzle5", 0, 20);
        // Its corridor's trigger asks; the die draws its first question, whose answer is the first
        game.Tile = (5, 17);
        game.Rolls.Enqueue(0);
        game.Answers.Enqueue(0);
        game.Run("Quiz1");
        Assert.Equal(1, game.Story.Var("VAR_TEMP_1"));
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("warp"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 5, 17, game.Story));
        // Spoken to afterwards, it only lets the player by
        game.Talk("mechadoll_1");
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("warp"));
        // The next doll's second question answered wrong: back to the start, the dolls set back
        game.Tile = (1, 7);
        game.Rolls.Enqueue(1);
        game.Answers.Enqueue(0);
        game.Run("Quiz2");
        Assert.Contains("warp Route110_TrickHousePuzzle5 0 20", game.Last.Log);
        game.Arrive("Route110_TrickHousePuzzle5", 0, 20);
        Assert.Equal(0, game.Story.Var("VAR_TEMP_1"));
        Assert.Equal(0, game.Story.Var("VAR_TEMP_2"));
    }

    [Fact]
    public void TheSixthPuzzlesGatesAreEmeraldsTrickHouseTable()
    {
        var game = new Game();
        game.Arrive("Route110_TrickHousePuzzle6", 0, 20);
        var gates = Assert.IsType<FortreeGates>(game.Map.Puzzle);
        Assert.Equal(FortreeGates.TrickHouseName, gates.Name);
        Assert.Equal(FortreeGates.TrickHouseLayout.Select(g => g.Start), gates.Gates.Select(g => g.Turn));
        // Every gate's pivot stands among open floor the arms can sweep
        foreach (var (gate, _) in gates.Gates) Assert.False(game.Map.IsSolid(gate.X, gate.Y) && game.Map.IsSolid(gate.X - 1, gate.Y - 1));
    }

    [Fact]
    public void TheSeventhPuzzlesSwitchesCarryItsTrainersRoundTheirLoops()
    {
        var game = new Game();
        game.Arrive("Route110_TrickHousePuzzle7", 0, 20);
        var tiles = Assert.IsType<MossdeepTiles>(game.Map.Puzzle);
        Assert.Equal(20, tiles.RoomArrows.Count);
        // Every switch the room's triggers name is the puzzle's
        foreach (var (x, y, _) in tiles.RoomSwitches) Assert.NotNull(FieldScripts.TriggerAt(game.Map, x, y, game.Story));
        var joshua = game.Present("psychic_m")!;
        Assert.Equal((9, 20), (joshua.GridX, joshua.GridY));
        game.Run("YellowSwitch");
        Assert.Equal((8, 20), (joshua.GridX, joshua.GridY));
        var statue = game.Present("trick_house_statue_2")!;
        Assert.Equal((4, 6), (statue.GridX, statue.GridY));
        // (the green loop's north-east arrow points west)
        game.Run("GreenSwitch");
        Assert.Equal((3, 6), (statue.GridX, statue.GridY));
        // and coming in again puts them back
        game.Arrive("Route110_TrickHousePuzzle7", 0, 20);
        Assert.Equal((9, 20), (joshua.GridX, joshua.GridY));
        Assert.Equal((4, 6), (statue.GridX, statue.GridY));
    }

    [Fact]
    public void TheEighthPuzzlesFloorIsSlippery()
    {
        var game = new Game();
        var room = game.MapOf("Route110_TrickHousePuzzle8");
        int ice = 0;
        for (int y = 0; y < room.Height; y++)
            for (int x = 0; x < room.Width; x++)
                if (room.BehaviourAt(x, y) == TileBehavior.Ice) ice++;
        Assert.True(ice > 40, $"{ice} tiles of ice");
    }

    // The game sends everyone home as a map is come to (Wandering.SendHome): he ran off down the room last time
    private static void ComeBackToHisPlace(Game game)
    {
        var master = game.MapOf("Route110_TrickHouseEnd").Everyone.Single(n => n.Key == "trick_master_end");
        (master.GridX, master.GridY) = (4, 5);
    }

    [Fact]
    public void EachPuzzlesRewardIsGivenAtTheEndAndTheWayShutsBehind()
    {
        string[] rewards = { "Rare Candy", "Timer Ball", "Hard Stone", "Smoke Ball", "TM12", "Magnet", "PP Max" };
        var game = new Game();
        for (int level = 0; level < 7; level++)
        {
            game.Story.SetVar("VAR_TRICK_HOUSE_LEVEL", level);
            game.Story.SetVar("VAR_TRICK_HOUSE_ENTRANCE_STATE", 1);
            game.Story.Set("FLAG_HOENN_TRICK_HOUSE_OPEN");
            ComeBackToHisPlace(game);
            game.Arrive("Route110_TrickHouseEnd", 10, 2);
            int before = game.Bag.GetQuantity(ItemDatabase.Get(rewards[level])!);
            game.Talk("trick_master_end");
            Assert.Equal(before + 1, game.Bag.GetQuantity(ItemDatabase.Get(rewards[level])!));
            Assert.Equal(level + 1, game.Story.Var("VAR_TRICK_HOUSE_LEVEL"));
            Assert.Equal(3, game.Story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE"));
            Assert.False(game.Story.Has("FLAG_HOENN_TRICK_HOUSE_OPEN"));
        }
        // The eighth gives a tent for the secret base, the colour chosen
        game.Story.SetVar("VAR_TRICK_HOUSE_ENTRANCE_STATE", 1);
        ComeBackToHisPlace(game);
        game.Arrive("Route110_TrickHouseEnd", 10, 2);
        game.Answers.Enqueue(1);
        game.Talk("trick_master_end");
        Assert.Contains(game.Last.Transcript, l => l.Text.Contains("Blue Tent"));
        Assert.Single(game.Last.Decorations);
        Assert.Equal(8, game.Story.Var("VAR_TRICK_HOUSE_LEVEL"));
        Assert.False(game.Story.Has("FLAG_HOENN_TRICK_HOUSE_TENT_OWED"));
    }
}
