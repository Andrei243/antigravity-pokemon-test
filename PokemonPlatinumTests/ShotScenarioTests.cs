using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T17: a scenario read into checked steps (every command and verb, a bad line named by its file and line,
/// the developer's commands shared with the console's table), the developer's commands at work on a game with no
/// window, and a snapshot of that game that names its state and tile.
/// </summary>
[Collection("Shared clocks and dice")]
public class ShotScenarioTests
{
    private const string File = "look.scn";

    [Fact]
    public void EveryVerbAndCommandIsALineOfAScenario()
    {
        var scenario = ShotScenario.Parse("""
            # a look at everything a scenario can say
            newgame 12 room
            load saves/one.json 3
            at Sinnoh 116 886 up
            step left 3 running
            press confirm
            talk
            answer 1
            read
            wait 0.5
            wild Starly 4
            trainer youngster_tristan
            menu
            move 0
            shot scn_look
            shot scn_look_close 100 200 300 150 4
            state
            expect map Sinnoh
            warp Sinnoh 116 886 down
            time night 2026-06-01
            weather rain
            give "Poké Ball" 5
            pokemon Starly 5
            heal
            flag FLAG_HAS_POKEDEX on
            var VAR_SPAWN_LOCATION 2
            """, File);
        string[] verbs = { "newgame", "load", "at", "step", "press", "talk", "answer", "read", "wait", "wild", "trainer", "menu", "move", "shot", "shot", "state", "expect",
                           "warp", "time", "weather", "give", "pokemon", "heal", "flag", "var" };
        Assert.Equal(verbs, scenario.Steps.Select(s => s.Verb));
        Assert.Equal(2, scenario.Steps[0].Line);
        Assert.Equal(new[] { "Poké Ball", "5" }, scenario.Steps.Single(s => s.Verb == "give").Args);
        Assert.True(scenario.Steps.Single(s => s.Verb == "heal").IsCommand);
        Assert.False(scenario.Steps.Single(s => s.Verb == "talk").IsCommand);
        Assert.Equal(new[] { "flag.FLAG_HAS_POKEDEX", "var.VAR_SPAWN_LOCATION" }, scenario.Watched);
        Assert.Equal("look", scenario.Name);
        // Every harness verb is in the scenario above
        Assert.Empty(ShotScenario.Verbs.Select(v => v.Name).Except(verbs));
    }

    [Fact]
    public void EveryConsoleCommandIsAScenarioLine()
    {
        // The scenarios and the console share one table (plan 16 · T6, T17): every command's own example reads
        Assert.NotEmpty(DevCommands.Commands);
        foreach (var command in DevCommands.Commands)
        {
            var scenario = ShotScenario.Parse("newgame\n" + command.Example, File);
            Assert.Equal(command.Name, scenario.Steps[1].Verb);
            Assert.True(scenario.Steps[1].IsCommand);
            Assert.Null(DevCommands.Check(command.Name, DevCommands.Split(command.Example)!.Skip(1).ToList()));
        }
        // ... and no verb of the harness's is also a command's name
        Assert.Empty(ShotScenario.Verbs.Select(v => v.Name).Intersect(DevCommands.Commands.Select(c => c.Name)));
        foreach (string name in new[] { "warp", "time", "weather", "give", "pokemon", "heal", "flag", "var" }) Assert.NotNull(DevCommands.Find(name));
    }

    [Fact]
    public void ABadLineIsNamedByItsFileAndLine()
    {
        var e = Assert.Throws<ScenarioException>(() => ShotScenario.Parse("""
            newgame
            at Sinnoh twelve 3 up
            dance
            step sideways 2
            give "Poké Ball
            shot a b
            warp Sinnoh 1
            time noon
            press jump
            answer
            """, File));
        Assert.Equal(9, e.Problems.Count);
        Assert.StartsWith($"{File}:2: x must be a whole number", e.Problems[0]);
        Assert.Equal($"{File}:3: 'dance' is no command or verb", e.Problems[1]);
        Assert.StartsWith($"{File}:4: 'sideways' is no way to face", e.Problems[2]);
        Assert.Equal($"{File}:5: a quote is never closed", e.Problems[3]);
        Assert.StartsWith($"{File}:6: a shot is a name, or a name and x y w h scale", e.Problems[4]);
        Assert.StartsWith($"{File}:7: 'warp' takes 3 to 4 word(s)", e.Problems[5]);
        Assert.StartsWith($"{File}:8: 'noon' is no time of day", e.Problems[6]);
        Assert.StartsWith($"{File}:9: 'jump' is no action", e.Problems[7]);
        Assert.StartsWith($"{File}:10: 'answer' takes 1 word(s)", e.Problems[8]);
    }

    [Fact]
    public void AScenarioBeginsAGameFirst()
    {
        var e = Assert.Throws<ScenarioException>(() => ShotScenario.Parse("talk\nnewgame", File));
        Assert.Equal($"{File}:1: a scenario begins a game first (newgame or load), not with 'talk'", Assert.Single(e.Problems));
        var empty = Assert.Throws<ScenarioException>(() => ShotScenario.Parse("# nothing\n\n", File));
        Assert.Equal($"{File}:1: the scenario has no lines", Assert.Single(empty.Problems));
        // A comment after a line, and a # inside quotes kept
        var scenario = ShotScenario.Parse("load save.json # the morning's\nexpect text \"Room #2\"", File);
        Assert.Equal(new[] { "save.json" }, scenario.Steps[0].Args);
        Assert.Equal(new[] { "text", "Room #2" }, scenario.Steps[1].Args);
    }

    [Fact]
    public void TheKeptScenariosRead()
    {
        // The harness's `scenarios` mode plays them; every one reads, begins a game and takes a shot named scn_*
        var files = Directory.GetFiles(Path.Combine(Repository.Root, "tools", "ShotHarness", "scenarios"), "*.scn");
        Assert.True(files.Length >= 4, "The kept scenarios are gone.");
        foreach (string file in files)
        {
            var scenario = ShotScenario.Load(file);
            Assert.StartsWith("scn_", scenario.Name);
            Assert.Contains(scenario.Steps, s => s.Verb == "shot" && s.Args[0].StartsWith("scn_", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void TheCommandsRefuseBadWordsBeforeAnyGame()
    {
        Assert.Null(DevCommands.Check("heal", Array.Empty<string>()));
        Assert.Equal("no command is called 'fly'", DevCommands.Check("fly", Array.Empty<string>()));
        Assert.StartsWith("'heal' takes 0 word(s)", DevCommands.Check("heal", new[] { "now" }));
        Assert.StartsWith("count must be a whole number from 1", DevCommands.Check("give", new[] { "Potion", "0" }));
        Assert.StartsWith("'sunny' is no weather", DevCommands.Check("weather", new[] { "sunny" }));
        Assert.Null(DevCommands.Check("weather", new[] { "off" }));
        Assert.StartsWith("'2026-13-01' is no date", DevCommands.Check("time", new[] { "day", "2026-13-01" }));
        Assert.StartsWith("a flag is on or off", DevCommands.Check("flag", new[] { "FLAG_X", "maybe" }));
        Assert.Equal(new[] { "give", "Poké Ball", "5" }, DevCommands.Split("give  \"Poké Ball\" 5 "));
        Assert.Null(DevCommands.Split("give \"Poké Ball 5"));
    }

    [Fact]
    public void TheCommandsChangeAGameWithNoWindowAndTheSnapshotNamesWhereItStands()
    {
        string folder = Path.Combine(Path.GetTempPath(), "scenario-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(7);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.State = GameState.Overworld;

            DevResult Run(string line)
            {
                var words = DevCommands.Split(line)!;
                return DevCommands.Run(engine, words[0], words.Skip(1).ToList());
            }

            Assert.True(Run("warp Sinnoh 116 886 left").Ok);
            Assert.False(Run("warp Nowhere 1 1").Ok);
            Assert.False(Run("warp Sinnoh 5000 1").Ok);
            Assert.True(Run("pokemon Starly 5").Ok);
            Assert.False(Run("pokemon Missingno 5").Ok);
            Assert.True(Run("give \"Poké Ball\" 3").Ok);
            Assert.Equal(3, game.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
            game.Party.Members[0].CurrentHP = 1;
            Assert.True(Run("heal").Ok);
            Assert.Equal(game.Party.Members[0].MaxHP, game.Party.Members[0].CurrentHP);
            Assert.True(Run("flag FLAG_SCENARIO_LOOK on").Ok);
            Assert.True(game.Story.Has("FLAG_SCENARIO_LOOK"));
            Assert.Equal("VAR_SCENARIO_LOOK is 4", Run("var VAR_SCENARIO_LOOK 4").Text);
            Assert.True(Run("weather rain").Ok);
            Assert.Equal(FieldWeather.Rain, game.Map.WeatherAt(116, 886));
            Assert.True(Run("weather off").Ok);
            Assert.Null(game.Map.WeatherOverride);
            Assert.True(Run("time night 2026-06-01").Ok);
            Assert.Equal(TimeOfDay.Night, GameClock.Now);
            Assert.Equal(new DateTime(2026, 6, 1), GameClock.Today);

            var snapshot = StateSnapshot.Of(game.Flow, new[] { "flag.FLAG_SCENARIO_LOOK", "var.VAR_SCENARIO_LOOK", "FLAG_HAS_POKEDEX" });
            Assert.Equal("Overworld", snapshot["state"]);
            Assert.Equal("Sinnoh", snapshot["map"]);
            Assert.Equal("116", snapshot["x"]);
            Assert.Equal("886", snapshot["y"]);
            Assert.Equal("left", snapshot["facing"]);
            Assert.Equal("twinleaf_town", snapshot["area"]);
            Assert.Equal("1", snapshot["party.count"]);
            Assert.StartsWith("Starly 5 ", snapshot["party.0"]);
            Assert.Equal("on", snapshot["flag.FLAG_SCENARIO_LOOK"]);
            Assert.Equal("4", snapshot["var.VAR_SCENARIO_LOOK"]);
            Assert.Equal(game.Story.Has("FLAG_HAS_POKEDEX") ? "on" : "off", snapshot["flag.FLAG_HAS_POKEDEX"]);
            Assert.Null(snapshot["battle.menu"]);

            // The file reads back as it was written, and two snapshots name what changed between them
            var back = StateSnapshot.FromJson(snapshot.ToJson());
            Assert.Equal(snapshot.Fields, back.Fields);
            Assert.True(Run("warp Sinnoh 117 886 right").Ok);
            var moved = StateSnapshot.Changes(snapshot, StateSnapshot.Of(game.Flow)).ToList();
            Assert.Contains(("x", "116", "117"), moved);
            Assert.Contains(("facing", "left", "right"), moved);
            Assert.Contains(moved, m => m.Field == "flag.FLAG_SCENARIO_LOOK" && m.After == null);
            Assert.DoesNotContain(moved, m => m.Field == "map");
        }
        finally
        {
            Dice.Seed(null);
            GameClock.Fixed = null;
            GameClock.FixedDate = null;
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
