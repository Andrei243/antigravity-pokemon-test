// Scenarios and the warm harness (plan 16 · T17): a look at something new written as a file of lines, played on the
// game a line at a time (Core/ShotScenario.cs reads it, checked, before anything is played).
//
//   dotnet run --project tools/ShotHarness -- <out dir> run <file.scn> [...]     each file played in turn, cold
//   dotnet run --project tools/ShotHarness -- <out dir> serve                    the engine kept warm: every file written into
//                                                                                <out dir>/inbox is played and moved to done/;
//                                                                                a file named `stop` ends it
//   dotnet run --project tools/ShotHarness -- <out dir> scenarios               the kept scenarios (tools/ShotHarness/scenarios)
//
// A scenario's shots, the state beside each (<shot>.state.json, Core/StateSnapshot.cs) and what it printed
// (printed.txt) go into <out dir>/<its name>/. Every scenario begins a game afresh (`newgame` or `load`): the dice
// seeded, the clocks at nought and on the harness's day, the harness's options, the maps rebuilt and the engine's own
// chance, scenes and field life begun again, so a scenario served after another draws what it draws run alone. The
// models, sprites, shaders and databases stay warm: they are most of a cold start.

partial class Harness
{
    // The harness's day and seed (Program.cs), which every scenario's game begins on
    const int ScenarioSeed = 20261004;
    static readonly DateTime ScenarioDay = new(2026, 6, 1);

    /// <summary>`run <file.scn>...`: each file in turn; the run ends with 1 when one failed or couldn't be read.</summary>
    public void RunScenariosMode()
    {
        var files = args.Skip(2).Select(f => Path.GetFullPath(f, startDir)).ToList();
        if (files.Count == 0) { Console.WriteLine("usage: <out dir> run <file.scn> [...]"); Environment.ExitCode = 2; return; }
        int failed = files.Count(file => !PlayScenarioFile(file));
        Console.WriteLine($"{files.Count} scenario(s) played, {failed} failed.");
        if (failed > 0) Environment.ExitCode = 1;
    }

    /// <summary>`scenarios`: every kept scenario, in the order of their names.</summary>
    public void ScenariosMode()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "scenarios");
        var files = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.scn").OrderBy(f => f, StringComparer.Ordinal).ToList() : new List<string>();
        if (files.Count == 0) { Console.WriteLine($"!! no scenarios in {folder}"); Environment.ExitCode = 1; return; }
        int failed = files.Count(file => !PlayScenarioFile(file));
        Console.WriteLine($"{files.Count} kept scenario(s) played, {failed} failed.");
        if (failed > 0) Environment.ExitCode = 1;
    }

    /// <summary>`serve`: the engine kept warm, playing whatever is written into the inbox until a file named `stop` is.</summary>
    public void ServeMode()
    {
        string inbox = Path.Combine(outDir, "inbox"), done = Path.Combine(outDir, "done");
        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(done);
        Console.WriteLine($"serving: write a .scn file into {inbox}; its shots go into {outDir}/<its name>; a file named stop ends it");
        Console.Out.Flush();
        while (true)
        {
            var waiting = Directory.GetFiles(inbox).OrderBy(f => File.GetLastWriteTimeUtc(f)).ThenBy(f => f, StringComparer.Ordinal).ToList();
            if (waiting.Count == 0) { Thread.Sleep(100); continue; }
            string next = waiting[0];
            string name = Path.GetFileName(next);
            if (Path.GetFileNameWithoutExtension(name) == "stop")
            {
                File.Delete(next);
                Console.WriteLine("stopped");
                return;
            }
            // A file still being written is waited for: its size must hold still for a tenth of a second
            long size = -1;
            while (new FileInfo(next).Length != size) { size = new FileInfo(next).Length; Thread.Sleep(100); }
            if (Path.GetExtension(name) == ".scn")
            {
                // Each scenario has the time limit to itself (SHOTS_TIME_LIMIT), however long the harness has served
                running.Restart();
                bool ok = PlayScenarioFile(next);
                Console.WriteLine(ok ? $"done: {name}" : $"failed: {name}");
            }
            else Console.WriteLine($"!! not a scenario, passed over: {name}");
            string to = Path.Combine(done, name);
            File.Move(next, to, overwrite: true);
            Console.Out.Flush();
        }
    }

    // ------------------------------------------------------------------ one scenario

    /// <summary>Reads and plays one file; false when it can't be read, a line fails or an expectation doesn't hold.</summary>
    bool PlayScenarioFile(string path)
    {
        ShotScenario scenario;
        try
        {
            scenario = ShotScenario.Load(path);
        }
        catch (ScenarioException e)
        {
            foreach (string problem in e.Problems) Console.WriteLine("!! " + problem);
            return false;
        }
        catch (IOException e)
        {
            Console.WriteLine($"!! {path}: {e.Message}");
            return false;
        }

        string folder = Path.Combine(outDir, scenario.Name);
        if (Directory.Exists(folder))
            foreach (string old in Directory.GetFiles(folder).Where(f => f.EndsWith(".png") || f.EndsWith(".json") || f.EndsWith(".txt"))) File.Delete(old);
        Directory.CreateDirectory(folder);

        var printed = new List<string>();
        void Say(string line)
        {
            Console.WriteLine(line);
            printed.Add(line);
        }

        Say($"scenario {scenario.Name} ({scenario.Steps.Count} lines)");
        bool ok = true;
        foreach (var step in scenario.Steps)
        {
            string? failure;
            try
            {
                failure = PlayStep(scenario, step, folder, Say);
            }
            catch (Exception e) when (e is not OutOfMemoryException)
            {
                failure = $"{e.GetType().Name}: {e.Message}";
            }
            if (failure != null)
            {
                Say($"!! {scenario.File}:{step.Line}: {step}: {failure}");
                ok = false;
                break;
            }
        }
        // Nothing a scenario steered outlasts it
        InputManager.Feed(null);
        game.Steering = null;
        Say(ok ? $"scenario {scenario.Name}: passed" : $"scenario {scenario.Name}: failed");
        File.WriteAllLines(Path.Combine(folder, "printed.txt"), printed);
        return ok;
    }

    /// <summary>One line played; null when it went as written, otherwise why not.</summary>
    string? PlayStep(ShotScenario scenario, ScenarioStep step, string folder, Action<string> say)
    {
        var a = step.Args;
        say($"> {step}");
        if (step.IsCommand)
        {
            var result = DevCommands.Run(engine, step.Verb, a);
            say("  " + result.Text);
            Frames(1);
            return result.Ok ? null : result.Text;
        }

        switch (step.Verb)
        {
            case "newgame":
            {
                int seed = a.Where(w => w != "room").Select(int.Parse).DefaultIfEmpty(ScenarioSeed).First();
                BeginScenarioGame(seed);
                engine.NewGameRegion = "Sinnoh";
                engine.StartNewGame();
                game.State = GameState.Overworld;
                game.SaveSlot = 1;
                game.LocationSign.Hide();
                if (!a.Contains("room")) PastTheOpening();
                Frames(2);
                return null;
            }
            case "load":
            {
                string save = Path.GetFullPath(a[0], Path.GetDirectoryName(Path.GetFullPath(scenario.File))!);
                if (!File.Exists(save)) save = Path.GetFullPath(a[0], startDir);
                if (!File.Exists(save)) return $"no save at {a[0]}";
                BeginScenarioGame(a.Count > 1 ? int.Parse(a[1]) : ScenarioSeed);
                game.SaveSlot = 1;
                File.Copy(save, game.SaveFile, overwrite: true);
                engine.ContinueGame();
                game.State = GameState.Overworld;
                game.LocationSign.Hide();
                Frames(2);
                return null;
            }
            case "at":
            {
                if (MapDatabase.TryGet(a[0]) == null) return $"no map is called '{a[0]}'";
                At(a[0], int.Parse(a[1]), int.Parse(a[2]), DevCommands.DirectionOf(a[3])!.Value);
                return null;
            }
            case "step":
            {
                var way = DevCommands.DirectionOf(a[0])!.Value;
                int tiles = int.Parse(a[1]);
                var walker = game.Player;
                int fromX = walker.GridX, fromY = walker.GridY;
                game.Steering = (way, a.Count > 2);
                for (int guard = 0; guard < tiles * 60 + 60 && game.State == GameState.Overworld && !game.ScriptRunning
                     && (Math.Abs(walker.GridX - fromX) + Math.Abs(walker.GridY - fromY) < tiles || walker.IsMoving); guard++)
                    Frames(1);
                game.Steering = null;
                int went = Math.Abs(walker.GridX - fromX) + Math.Abs(walker.GridY - fromY);
                say($"  {went} of {tiles} step(s), now at {walker.GridX}, {walker.GridY}");
                return null;
            }
            case "press":
            {
                InputManager.Feed(ActionFrame.Press(ShotScenario.ActionOf(a[0])!.Value));
                Frames(1);
                InputManager.Feed(null);
                Frames(1);
                return null;
            }
            case "talk":
                game.Interact();
                Frames(3);
                return null;
            case "answer":
            {
                for (int i = 0; i < 1200 && !game.Choice.IsOpen; i++) ReadOneFrame();
                if (!game.Choice.IsOpen) return "no question was asked";
                Frames(10);
                int answer = int.Parse(a[0]);
                if (answer >= game.Choice.Options.Count) return $"the question has {game.Choice.Options.Count} answers ({string.Join(" | ", game.Choice.Options)})";
                for (int i = 0; i < answer; i++) game.Choice.Move(1);
                say($"  answered: {game.Choice.Options[answer]}");
                game.Choice.Confirm();
                Frames(2);
                return null;
            }
            case "read":
                ReadToTheEnd(say);
                return null;
            case "wait":
                Frames(Math.Max(1, (int)Math.Round(ShotScenario.SecondsOf(a[0])!.Value * 60.0)));
                return null;
            case "wild":
            {
                if (PokemonDatabase.Get(a[0]) == null) return $"no species is called '{a[0]}'";
                int level = int.Parse(a[1]);
                game.StartWildBattle(new WildEncounterEntry { SpeciesName = a[0], MinLevel = level, MaxLevel = level });
                return IntoTheBattle();
            }
            case "trainer":
            {
                if (TrainerDatabase.Get(a[0]) is not { } record) return $"no trainer is called '{a[0]}'";
                var trainer = new Trainer { Id = record.Id };
                TrainerDatabase.Fill(trainer, record);
                game.StartTrainerBattle(new NPC { Name = trainer.Name, NpcType = "Youngster", TrainerData = trainer });
                return IntoTheBattle();
            }
            case "menu":
                if (game.State != GameState.Battle || game.Battle == null) return "no battle is on";
                ToMainMenu(game.Battle);
                return game.Battle.HUD.MenuState == BattleMenuState.Main ? null : $"the battle stands at {game.Battle.HUD.MenuState}, not its menu";
            case "move":
            {
                var b = game.Battle;
                if (game.State != GameState.Battle || b == null || b.HUD.MenuState != BattleMenuState.Main) return "no battle stands at its menu";
                int move = int.Parse(a[0]);
                if (move >= b.PlayerPokemon.Moves.Count) return $"{b.PlayerPokemon.DisplayName} knows {b.PlayerPokemon.Moves.Count} move(s)";
                say($"  {b.PlayerPokemon.DisplayName} uses {b.PlayerPokemon.Moves[move].Data.Name}");
                b.SelectMainMenuOption(0);
                b.SelectMove(move);
                if (b.HUD.MenuState == BattleMenuState.SelectTarget) b.SelectTarget(0);
                Frames(1);
                return null;
            }
            case "shot":
            {
                string name = scenario.Name + "/" + a[0];
                if (a.Count == 6) ShotCrop(name, int.Parse(a[1]), int.Parse(a[2]), int.Parse(a[3]), int.Parse(a[4]), int.Parse(a[5]));
                else Shot(name);
                File.WriteAllText(Path.Combine(folder, a[0] + ".state.json"), StateSnapshot.Of(game.Flow, scenario.Watched, scenarioRevisionBase).ToJson());
                return null;
            }
            case "state":
            {
                var snapshot = StateSnapshot.Of(game.Flow, scenario.Watched, scenarioRevisionBase);
                foreach (var (field, value) in snapshot.Fields) say($"  {field}: {value}");
                File.WriteAllText(Path.Combine(folder, StateSnapshot.FileName), snapshot.ToJson());
                return null;
            }
            case "expect":
            {
                string? actual = StateSnapshot.Of(game.Flow, scenario.Watched, scenarioRevisionBase)[a[0]];
                if (actual == a[1]) { say($"  {a[0]} is {a[1]}"); return null; }
                return actual == null ? $"the state has no field '{a[0]}'" : $"{a[0]} was to be '{a[1]}' and is '{actual}'";
            }
        }
        return $"'{step.Verb}' is known to the reader but not to the harness";
    }

    // The story's count of changes when a scenario began: a snapshot tells it from there, so a scenario served after another reads what it reads alone
    int scenarioRevisionBase;

    // The game begun afresh for a scenario, whatever the one before it left
    void BeginScenarioGame(int seed)
    {
        InputManager.Feed(null);
        Dice.Seed(seed);
        tick = 0;
        FrameClock.Fixed = 0;
        GameClock.FixedDate = ScenarioDay;
        WallClock.Fixed = ScenarioDay.AddHours(12);
        // The harness's options (the constructor's), with what a scenario's `time` changed put back
        var settings = game.Settings;
        settings.Muted = true;
        settings.TimeOfDay = TimeOfDay.Day;
        settings.Season = Season.Spring;
        if (Environment.GetEnvironmentVariable("SHOTS_LANGUAGE") is { Length: > 0 } language) settings.Language = language;
        game.ApplySettings();
        // The maps made again (with their people, their trainers' teams and their own dice), then the engine's own
        MapDatabase.Initialize();
        game.Afresh();
        scenarioRevisionBase = game.Flow.Story.Revision;
    }

    // One frame of reading on: a line finished or put away, or the game left to go on
    void ReadOneFrame()
    {
        var box = game.Dialogue;
        if (box.IsActive && !box.IsCurrentLineComplete) box.FinishLine();
        else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) box.Advance();
        Frames(1);
    }

    // `read`: the text box to its end (a question is left open for `answer`); in a battle, its lines to its menu or its end
    void ReadToTheEnd(Action<string> say)
    {
        if (game.State == GameState.Battle && game.Battle is { } b)
        {
            for (int i = 0; i < 400 && !b.IsBattleOver && b.HUD.MenuState is not (BattleMenuState.Main or BattleMenuState.SwitchPokemon); i++)
            {
                if (b.HUD.MenuState == BattleMenuState.Message || b.HUD.MenuState == BattleMenuState.LearnMove) say("  " + b.CurrentMessage);
                Confirm(b);
                Skip(0.5);
            }
            if (b.IsBattleOver)
                for (int i = 0; i < 1500 && !FieldAgain(); i++) Frames(1);
            return;
        }
        string last = null;
        for (int i = 0; i < 3000; i++)
        {
            var box = game.Dialogue;
            if (game.Choice.IsOpen) break;
            if (box.IsActive)
            {
                if (box.CurrentLine != last) { say("  " + box.CurrentLine); last = box.CurrentLine; }
                ReadOneFrame();
            }
            else if (game.ScriptRunning && game.State is GameState.Overworld or GameState.Dialogue) Frames(1);
            else break;
        }
        Frames(2);
    }

    // A battle the game began, waited for through its way in
    string? IntoTheBattle()
    {
        for (int i = 0; i < 900 && game.State != GameState.Battle; i++) Frames(1);
        return game.State == GameState.Battle ? null : "the battle never began";
    }
}
