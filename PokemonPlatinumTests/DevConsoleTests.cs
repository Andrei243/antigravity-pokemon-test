using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T6: the developer's console (its line, history and output, the commands it shares with the scenarios),
/// a script's trace, and the live reload's parts that need no window: which files ask for what, the quiet the changes
/// wait for, and a reload of a bad file, which says what is wrong and changes nothing.
/// </summary>
[Collection("Shared clocks and dice")]
public class DevConsoleTests
{
    [Fact]
    public void TheConsolesOwnCommandsReadTheirExamplesAndRefuseBadWords()
    {
        foreach (string name in new[] { "flags", "encounters", "trace", "reload", "fps", "screenshot", "help" })
        {
            var command = DevCommands.Find(name);
            Assert.NotNull(command);
            Assert.Null(DevCommands.Check(name, DevCommands.Split(command!.Example)!.Skip(1).ToList()));
        }
        Assert.StartsWith("it is on or off, not 'maybe'", DevCommands.Check("trace", new[] { "maybe" }));
        Assert.StartsWith("it is on or off", DevCommands.Check("encounters", new[] { "sometimes" }));
        Assert.StartsWith("'sprites' is nothing to reload", DevCommands.Check("reload", new[] { "sprites" }));
        Assert.Null(DevCommands.Check("reload", Array.Empty<string>()));
        Assert.StartsWith("fps is on, off or sections", DevCommands.Check("fps", new[] { "fast" }));
        Assert.StartsWith("'screenshot' takes 0 word(s)", DevCommands.Check("screenshot", new[] { "now" }));
        Assert.Equal("no command is called 'fly'", DevCommands.Check("help", new[] { "fly" }));
        Assert.StartsWith("'flags' takes 0 to 1 word(s)", DevCommands.Check("flags", new[] { "A", "B" }));
        // The names the console's words stand for
        Assert.Equal(ReloadKind.Rooms | ReloadKind.Overlays, Reload.Named("maps"));
        Assert.Equal(ReloadKind.All, Reload.Named("all"));
        Assert.Equal("scripts, music", Reload.Describe(ReloadKind.Scripts | ReloadKind.Music));
    }

    [Fact]
    public void TheConsoleKeepsItsLineHistoryAndOutput()
    {
        var console = new DevConsole();
        console.Type("he`lp\u0007");
        Assert.Equal("help", console.Line);
        console.Backspace();
        Assert.Equal("hel", console.Line);

        // A line with no game behind it: a command that needs none answers, and a bad one is refused in red
        var refused = console.Run(null!, "dance now");
        Assert.False(refused.Ok);
        Assert.Equal(new ConsoleLine("> dance now", ConsoleTone.Typed), console.Output[0]);
        Assert.Equal(new ConsoleLine("no command is called 'dance'", ConsoleTone.Refused), console.Output[1]);
        Assert.False(console.Run(null!, "give \"Poké Ball 5").Ok);
        Assert.Equal("a quote is never closed", console.Output[^1].Text);
        Assert.True(console.Run(null!, "help").Ok);
        Assert.Contains(console.Output, l => l.Text == "  warp <map> <x> <y> [up|down|left|right]");
        Assert.Contains(console.Output, l => l.Text == "  reload [scripts|maps|music|models|all]");

        // Up and down go through the lines typed, and back to the one being typed
        console.Type("x");
        console.Older();
        Assert.Equal("help", console.Line);
        console.Older();
        Assert.Equal("give \"Poké Ball 5", console.Line);
        console.Newer();
        console.Newer();
        Assert.Equal("helx", console.Line);
        Assert.Equal(new[] { "dance now", "give \"Poké Ball 5", "help" }, console.History);
    }

    [Fact]
    public void TheTraceIsToldOfEveryInstructionOnce()
    {
        var library = ScriptLibrary.FromSources(("test", "script S\n setflag FLAG_TRACED\n setvar VAR_TRACED 2\n clearflag FLAG_TRACED\n"));
        var script = library.All.Single();
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        var told = new List<string>();
        runner.Trace = (s, i) => told.Add(GameEngine.TraceLine(s, i));
        runner.Start(script);
        runner.RunToEnd();
        Assert.Equal(new[] { "test.S:2 setflag FLAG_TRACED", "test.S:3 setvar VAR_TRACED", "test.S:4 clearflag FLAG_TRACED" }, told);
        Assert.Equal(script.Code.Count, told.Count);

        // No trace, nothing told
        runner.Trace = null;
        runner.Start(script);
        runner.RunToEnd();
        Assert.Equal(3, told.Count);
    }

    [Fact]
    public void AFileSavedAsksForWhatItIs()
    {
        string data = Path.Combine(Path.GetTempPath(), "data");
        string models = Path.Combine(Path.GetTempPath(), "overrides", "models");
        ReloadKind Of(params string[] parts) => Reload.KindOf(data, Path.Combine(new[] { data }.Concat(parts).ToArray()), new[] { models });
        Assert.Equal(ReloadKind.Scripts, Of("scripts", "twinleaf_town.txt"));
        Assert.Equal(ReloadKind.Rooms, Of("maps", "PoffinHouse.json"));
        Assert.Equal(ReloadKind.Overlays, Of("world", "sinnoh", "overlays", "twinleaf_town.json"));
        Assert.Equal(ReloadKind.Music, Of("music", "sinnoh", "twinleaf.mml"));
        Assert.Equal(ReloadKind.None, Of("world", "sinnoh", "areas", "twinleaf_town.json"));
        Assert.Equal(ReloadKind.None, Of("species.json"));
        Assert.Equal(ReloadKind.None, Of("scripts", "notes.md"));
        Assert.Equal(ReloadKind.Models, Reload.KindOf(data, Path.Combine(models, "Bidoof.glb"), new[] { models }));
        Assert.Equal(ReloadKind.None, Reload.KindOf(data, Path.Combine(models, "Bidoof.png"), new[] { models }));
    }

    [Fact]
    public void ChangesWaitForTheFilesToBeQuiet()
    {
        var queue = new ReloadQueue();
        Assert.Null(queue.Take(10));
        queue.Note("a.txt", ReloadKind.Scripts, 0.0);
        Assert.Null(queue.Take(0.1));
        queue.Note("b.json", ReloadKind.Rooms, 0.2);
        queue.Note("c.json", ReloadKind.None, 0.3);
        Assert.Null(queue.Take(0.4));
        var batch = queue.Take(0.5);
        Assert.NotNull(batch);
        Assert.Equal(ReloadKind.Scripts | ReloadKind.Rooms, batch!.Kinds);
        Assert.Equal(new[] { "a.txt", "b.json" }, batch.Paths);
        Assert.False(queue.Waiting);
        Assert.Null(queue.Take(5));
    }

    [Fact]
    public void AReloadOfABadFileSaysWhatIsWrongAndChangesNothing()
    {
        string folder = Path.Combine(Path.GetTempPath(), "reload-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var before = ScriptLibrary.Default;
            File.WriteAllText(Path.Combine(folder, "test.txt"), "script S\n frobnicate FLAG_X\n");
            Assert.Null(ScriptLibrary.Reload(out var problems, folder));
            Assert.Contains("test.txt", Assert.Single(problems));
            // A folder that reads is read, and still isn't the game's
            File.WriteAllText(Path.Combine(folder, "test.txt"), "script S\n setflag FLAG_X\n");
            var good = ScriptLibrary.Reload(out problems, folder);
            Assert.NotNull(good!.Find("test.S"));
            Assert.Same(before, ScriptLibrary.Default);

            // A room that doesn't read, or names another map, is refused and the maps are as they were
            Assert.NotNull(MapDatabase.TryGet("Sinnoh"));
            string bad = Path.Combine(folder, "BrokenRoom.json");
            File.WriteAllText(bad, "{ not json");
            Assert.NotEmpty(MapDatabase.Reload(new[] { bad }));
            string misnamed = Path.Combine(folder, "Misnamed.json");
            File.Copy(GameDataFiles.PathOf(Path.Combine(MapDatabase.Folder, "PoffinHouse.json")), misnamed);
            Assert.Contains("names its map 'PoffinHouse'", Assert.Single(MapDatabase.Reload(new[] { misnamed })));
            Assert.Null(MapDatabase.TryGet("BrokenRoom"));
            Assert.Null(MapDatabase.TryGet("Misnamed"));
        }
        finally
        {
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void TheConsoleWarpsGivesHealsAndTracesAGameWithNoWindow()
    {
        string folder = Path.Combine(Path.GetTempPath(), "console-" + Guid.NewGuid().ToString("N"));
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
            var console = engine.DevConsole;

            Assert.True(console.Run(engine, "warp Sinnoh 116 886 down").Ok);
            Assert.Equal((116, 886), (game.Player.GridX, game.Player.GridY));
            Assert.True(console.Run(engine, "give Potion 2").Ok);
            Assert.Equal(2, game.Bag.GetQuantity(ItemDatabase.Get("Potion")!));
            Assert.True(console.Run(engine, "pokemon Starly 5").Ok);
            game.Party.Members[0].CurrentHP = 1;
            Assert.True(console.Run(engine, "heal").Ok);
            Assert.Equal(game.Party.Members[0].MaxHP, game.Party.Members[0].CurrentHP);
            Assert.True(console.Run(engine, "encounters off").Ok);
            Assert.True(engine.WildEncountersOff);
            Assert.True(console.Run(engine, "encounters on").Ok);
            Assert.False(console.Run(engine, "screenshot").Ok);

            // The trace prints a script's instructions as the game carries them out
            Assert.True(console.Run(engine, "trace on").Ok);
            var library = ScriptLibrary.FromSources(("test", "script S\n setflag FLAG_CONSOLE_TRACED\n setvar VAR_CONSOLE_TRACED 3\n"));
            engine.StartScript(library.All.Single());
            for (int frame = 0; frame < 120 && engine.ScriptRunning; frame++)
            {
                InputManager.Feed(ActionFrame.None);
                engine.Update(1f / 30f);
            }
            Assert.False(engine.ScriptRunning);
            Assert.Contains(new ConsoleLine("test.S:2 setflag FLAG_CONSOLE_TRACED", ConsoleTone.Trace), console.Output);
            Assert.Contains(new ConsoleLine("test.S:3 setvar VAR_CONSOLE_TRACED", ConsoleTone.Trace), console.Output);
            Assert.True(console.Run(engine, "trace off").Ok);
            Assert.False(engine.Tracing);

            var flags = console.Run(engine, "flags CONSOLE_TRACED");
            Assert.Contains("FLAG_CONSOLE_TRACED", flags.Text);
            Assert.Contains("VAR_CONSOLE_TRACED = 3", flags.Text);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            GameClock.Fixed = null;
            GameClock.FixedDate = null;
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
