using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T7: the playthrough bot and the key fuzzer. The policy walks a town by the field's own rules with no game
/// at all; the softlock detector and the report are plain data; and the bot and the fuzzer play a game with no
/// window (plan 16 · T12) for a minute or two of game time each, failing on an exception, a softlock, a script gone
/// wrong or a value out of range. They share the dice, the save folder and the rules with the whole process, so they
/// run with the others that do.
/// </summary>
[Collection("Shared clocks and dice")]
public class PlaythroughBotTests
{
    private static readonly Direction[] Ways = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

    [Fact]
    public void ThePolicyVisitsEveryTileOfTwinleafTownSpeaksToEveryoneAndTakesItsWays()
    {
        var map = Fixtures.Map("TwinleafTown");
        var walker = new Walker();
        var (x, y) = OpenTileNearTheMiddle(map);
        float h = map.HeightAt(x, y);
        var memory = new AutoplayMemory();
        memory.Visit(map.Name, x, y);

        // What can be come to, found apart from the policy: a flood by the field's own steps that ends on a warp
        var reachable = Flood(map, x, y, walker);
        var warps = reachable.Where(t => map.GetWarpAt(t.X, t.Y) != null && t != (x, y)).ToHashSet();
        var people = map.NPCs.Where(n => Ways.Any(w =>
        {
            var (dx, dy) = FieldMovement.Delta(w);
            return reachable.Contains((n.GridX - dx, n.GridY - dy)) && !warps.Contains((n.GridX - dx, n.GridY - dy))
                || map.IsCounter(n.GridX - dx, n.GridY - dy) && reachable.Contains((n.GridX - 2 * dx, n.GridY - 2 * dy));
        })).ToList();
        Assert.NotEmpty(people);
        Assert.NotEmpty(warps);

        var spoken = new HashSet<NPC>();
        var taken = new HashSet<(int, int)>();
        int plans = 0;
        for (; plans < 20000; plans++)
        {
            var plan = Policy.Next(map, x, y, walker with { Height = h }, memory, reach: 400);
            if (plan == null) break;
            foreach (var way in plan.Steps)
            {
                var step = FieldMovement.Step(map, x, y, way, walker with { Height = h });
                Assert.True(step.Moves, $"The plan for {plan.Goal.Key} walks into {step.Obstacle} at {x},{y} going {way}.");
                (x, y, h) = (step.X, step.Y, step.Height);
                memory.Visit(map.Name, x, y);
            }
            var goal = plan.Goal;
            Assert.Equal((goal.StandX, goal.StandY), (x, y));
            switch (goal.Kind)
            {
                case GoalKind.Speak or GoalKind.Item or GoalKind.Obstacle:
                    var (dx, dy) = FieldMovement.Delta(goal.Face);
                    var faced = map.GetNpcAt(x + dx, y + dy) ?? map.GetNpcAt(x + 2 * dx, y + 2 * dy);
                    Assert.NotNull(faced);
                    Assert.Equal((goal.TargetX, goal.TargetY), (faced!.GridX, faced.GridY));
                    spoken.Add(faced);
                    break;
                case GoalKind.Warp:
                    taken.Add((x, y));
                    break;
            }
            memory.Complete(goal.Key);
        }
        Assert.True(plans < 20000, "The policy never ran out of goals.");

        Assert.Empty(reachable.Where(t => !warps.Contains(t) && !memory.Visited(map.Name, t.X, t.Y)));
        Assert.Empty(people.Where(p => !spoken.Contains(p)).Select(p => $"{p.Name} at {p.GridX},{p.GridY}"));
        Assert.Equal(warps.OrderBy(t => t).ToList(), taken.OrderBy(t => t).ToList());
    }

    [Fact]
    public void TheSoftlockDetectorFiresOnAFrozenStateAndOnlyThen()
    {
        var watch = new Softlock();
        const float frame = 1f / 60f;
        // A state that keeps changing never fires
        for (int f = 0; f < 60 * 30; f++) Assert.False(watch.Observe($"Dialogue|{f / 30}", frame, watched: true));
        // Nor one that stands still while nobody is acting on it
        for (int f = 0; f < 60 * 30; f++) Assert.False(watch.Observe("Overworld|free", frame, watched: false));
        // One frozen while watched fires once, after ten seconds
        int fired = 0, at = -1;
        for (int f = 0; f < 60 * 30; f++)
            if (watch.Observe("Battle|frozen", frame, watched: true)) { fired++; at = f; }
        Assert.Equal(1, fired);
        Assert.InRange(at, 60 * 10 - 2, 60 * 10 + 2);
        Assert.Equal("Battle|frozen", watch.Frozen);
    }

    [Fact]
    public void AGoalThatFailsThreeTimesIsDropped()
    {
        var memory = new AutoplayMemory();
        Assert.False(memory.Fail("x"));
        Assert.True(memory.IsOpen("x"));
        Assert.False(memory.Fail("x"));
        Assert.True(memory.Fail("x"));
        Assert.False(memory.IsOpen("x"));
        Assert.Equal(new[] { "x" }, memory.Dropped);
    }

    [Fact]
    public void TheReportNamesWhatAWalkOfTheGameReachesThatTheRunDidNot()
    {
        var report = new AutoplayReport();
        report.Maps.Add("Sinnoh");
        report.Maps.Add("PlayerHouse");
        Assert.True(report.Arrive("twinleaf_town"));
        Assert.False(report.Arrive("twinleaf_town"));
        Assert.True(report.Clean);
        Assert.Equal(new[] { "RowanLab" }, report.NotReached(new[] { "Sinnoh", "RowanLab", "PlayerHouse" }));
        report.Softlocks.Add("10 seconds unchanged: Battle");
        Assert.False(report.Clean);
        string md = report.ToMarkdown(new[] { "Sinnoh", "RowanLab" });
        Assert.Contains("## Findings: softlocks (1)", md);
        Assert.Contains("- RowanLab", md);
        Assert.Contains("- twinleaf_town", md);
    }

    [Fact]
    public void SuggestPicksTheMoveThatHitsHardestAndDrawsNothing()
    {
        var battle = Scenario.Battle(Scenario.Mon("Squirtle", 20, "Growl", "Tackle", "Water Gun"), Scenario.Mon("Charmander", 20, "Growl"));
        uint before = ((BattleRandom)battle.Random).State;
        Assert.Equal(2, battle.Suggest());
        Assert.Equal(before, ((BattleRandom)battle.Random).State);

        // Nothing but status moves: the first that can be picked
        var calm = Scenario.Battle(Scenario.Mon("Squirtle", 20, "Growl", "Tail Whip"), Scenario.Mon("Charmander", 20, "Growl"));
        Assert.Equal(0, calm.Suggest());
    }

    [Fact]
    public void TheBotPlaysTwinleafTownWithNoWindowAndFindsNothingWrong()
    {
        Headless(engine =>
        {
            PastTheOpening(engine);
            var bot = new Autoplayer(engine, seed: 7);
            bot.Play(90);
            bot.Release();
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.True(report.Tiles > 40, $"Only {report.Tiles} tiles stood on.\n{report.ToMarkdown()}");
            Assert.True(report.Spoken.Count > 0, report.ToMarkdown());
            Assert.True(report.Warps.Count > 0, report.ToMarkdown());
            // Through a door and out again: a house of Twinleaf Town is come to
            Assert.True(report.Maps.Count > 1, report.ToMarkdown());
        });
    }

    [Fact]
    public void TheBotPlaysANewGameFromTheBedroomOutOfTheHouse()
    {
        Headless(engine =>
        {
            var game = engine.Drive;
            Assert.Equal("PlayerHouse2F", game.Map.Name);
            Assert.Empty(game.Party.Members);
            var bot = new Autoplayer(engine, seed: 3);
            bot.Play(240);
            bot.Release();
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Contains("PlayerHouse", report.Maps);
            Assert.Contains("Sinnoh", report.Maps);
        });
    }

    [Fact]
    public void TheKeyFuzzerMashesTheGameWithNoWindowAndNothingThrows()
    {
        Headless(engine =>
        {
            PastTheOpening(engine);
            int fuzzSeed = int.TryParse(Environment.GetEnvironmentVariable("FUZZ_SEED"), out int s) ? s : 11;
            var fuzzer = new Autoplayer(engine, seed: fuzzSeed, keys: true);
            fuzzer.Play(120);
            fuzzer.Release();
            Assert.Empty(fuzzer.Report.Exceptions);
            Assert.Empty(fuzzer.Report.OutOfRange);
            Assert.Empty(fuzzer.Report.ScriptsGoneWrong);
        });
    }

    [Fact]
    public void TheFuzzerIsTheSameForTheSameSeed()
    {
        var a = new KeyFuzzer(5);
        var b = new KeyFuzzer(5);
        var c = new KeyFuzzer(6);
        var fromA = Enumerable.Range(0, 500).Select(_ => a.Next()).ToList();
        Assert.Equal(fromA, Enumerable.Range(0, 500).Select(_ => b.Next()).ToList());
        Assert.NotEqual(fromA, Enumerable.Range(0, 500).Select(_ => c.Next()).ToList());
        Assert.DoesNotContain(fromA, f => f.IsDown(GameAction.Fullscreen) || f.IsDown(GameAction.Mute) || f.IsDown(GameAction.Screenshot));
    }

    // ---------------------------------------------------------------- helpers

    // A game with no window, begun in Sinnoh's bedroom, in a save folder of the test's own
    private static void Headless(Action<GameEngine> play)
    {
        string folder = Path.Combine(Path.GetTempPath(), "playbot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(12);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            engine.Drive.State = GameState.Overworld;
            play(engine);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    // Past the first chapter as the harness takes it, with a lead strong enough not to lose to Route 201's grass
    private static void PastTheOpening(GameEngine engine)
    {
        var game = engine.Drive;
        StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 30));
        game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
        game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Down);
        game.TrainersLookOnArrival = false;
        game.State = GameState.Overworld;
    }

    private static (int X, int Y) OpenTileNearTheMiddle(Map map)
    {
        int cx = map.Width / 2, cy = map.Height / 2;
        for (int r = 0; r < Math.Max(map.Width, map.Height); r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (map.InBounds(x, y) && map.IsWalkable(x, y) && map.GetWarpAt(x, y) == null && map.GetNpcAt(x, y) == null) return (x, y);
                }
        throw new InvalidOperationException("No open tile on " + map.Name);
    }

    private static HashSet<(int X, int Y)> Flood(Map map, int x, int y, Walker walker)
    {
        var heights = new Dictionary<(int, int), float> { [(x, y)] = map.HeightAt(x, y) };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((x, y));
        while (queue.Count > 0)
        {
            var here = queue.Dequeue();
            if (here != (x, y) && map.GetWarpAt(here.X, here.Y) != null) continue;
            foreach (var way in Ways)
            {
                var step = FieldMovement.Step(map, here.X, here.Y, way, walker with { Height = heights[here] });
                if (!step.Moves || heights.ContainsKey((step.X, step.Y))) continue;
                heights[(step.X, step.Y)] = step.Height;
                queue.Enqueue((step.X, step.Y));
            }
        }
        return heights.Keys.ToHashSet();
    }
}
