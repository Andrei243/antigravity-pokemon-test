partial class Harness
{
    // ---------------------------------------------------------------- the crash handler at work (plan 16 · T3)

    // Not part of `all`. A game in progress is run inside the game's own crash handler, exactly as Program.Main runs
    // it (Program.Guarded), and an exception is thrown in its loop on purpose: the handler is seen at work without a
    // real game crashing. What it leaves in <out dir> is what a player's crash leaves beside their save: the report in
    // logs/, the game as it stood as savegame.crash.json, the log (this mode opens one, which no other mode does) and
    // the note for the next start. The mode then does what the next start does: the save is read, the note taken up,
    // and the title's notice shot (crash_1_title_notice). It prints the report, what each file holds and that the
    // player's own save is byte for byte what it was. A second crash shows what is kept of the first. Last, the wait
    // behind another window: the game told its window isn't in front, with the option off and on, and in front again.
    //
    // `crash thread` throws on another thread instead, which nothing can catch: the handler writes the same files from
    // AppDomain.UnhandledException and the process ends there, as the game's would.
    public void CrashMode()
    {
        const float Dt = 1f / 60f;
        void Seconds(float s) => Frames((int)MathF.Round(s / Dt));
        string Size(string path) => File.Exists(path) ? $"{new FileInfo(path).Length} bytes" : "not there";
        void Leftovers()
        {
            foreach (string name in new[] { SaveManager.FileName, SaveManager.BackupOf(SaveManager.FileName), CrashReport.SaveName, SaveManager.BackupOf(CrashReport.SaveName), Log.FileName })
                Console.WriteLine($"  {name}: {Size(Path.Combine(outDir, name))}");
            string logs = Path.Combine(outDir, CrashReport.FolderName);
            var files = Directory.Exists(logs) ? Directory.GetFiles(logs) : Array.Empty<string>();
            foreach (string file in files.OrderBy(f => f, StringComparer.Ordinal))
                Console.WriteLine($"  {CrashReport.FolderName}/{Path.GetFileName(file)}: {Size(file)}");
        }

        // What an earlier run of this mode left here would be taken for this run's
        foreach (string name in new[] { SaveManager.FileName, SaveManager.BackupOf(SaveManager.FileName), CrashReport.SaveName, SaveManager.BackupOf(CrashReport.SaveName), Log.FileName, LogBook.RolledName(Log.FileName) })
            File.Delete(Path.Combine(outDir, name));
        if (Directory.Exists(Path.Combine(outDir, CrashReport.FolderName))) Directory.Delete(Path.Combine(outDir, CrashReport.FolderName), recursive: true);

        // The player's own save, written as the save screen writes it; then the game goes on, so that the game as it
        // stood when it stopped is not the game that was saved
        Console.WriteLine($"saved: {game.Save()}, at {game.Map.Name} {game.Player.GridX},{game.Player.GridY} with {game.Money} of money");
        byte[] saved = File.ReadAllBytes(Path.Combine(outDir, SaveManager.FileName));
        game.Money += 1234;
        At("Sinnoh", 112, 880, Direction.Down);
        Frames(30);

        bool opened = Log.Open(Path.Combine(outDir, Log.FileName));
        Log.Heading($"==== the harness's crash mode, {GameVersion.Describe()} ====");
        Log.Info($"A game in progress at {game.Map.Name} {game.Player.GridX},{game.Player.GridY}; the log is {(opened ? "open" : "not open")}.");

        if (args.Skip(2).Contains("thread"))
        {
            Console.WriteLine("throwing on another thread, inside the game's guard: the process ends here, and the files are left");
            Console.Out.Flush();
            PokemonPlatinumEngine.Program.Guarded(() => engine, "(the harness's crash mode, on a thread)", () =>
            {
                new Thread(() => throw new InvalidOperationException("Thrown on purpose on another thread by the harness's crash mode.")) { Name = "crash mode" }.Start();
                for (int i = 0; i < 600; i++) Frames(1);
            });
            Console.WriteLine("!! the process went on after an exception nobody caught");
            return;
        }

        int Crash(string what)
        {
            int code = PokemonPlatinumEngine.Program.Guarded(() => engine, "(the harness's crash mode)", () =>
            {
                // The game's loop, and an exception in the middle of it
                Frames(20);
                engine.Update(Dt);
                throw new InvalidOperationException(what, new ArgumentException("What it wrapped, which the report tells too."));
            });
            var outcome = CrashReport.Last;
            Console.WriteLine($"the guarded loop returned {code} ({(code == CommandLine.Crashed ? "crashed" : "not a crash")}); report {(outcome?.ReportPath is { } r ? Path.GetRelativePath(outDir, r) : "none")}, "
                + $"crash save {(outcome?.SavePath is { } s ? Path.GetRelativePath(outDir, s) : "none: " + outcome?.SaveProblem)}");
            return code;
        }

        Crash("Thrown on purpose by the harness's crash mode.");
        Console.WriteLine("left behind:");
        Leftovers();

        // The player's own save is what it was, and the crash save reads back whole as the game that was running
        byte[] after = File.ReadAllBytes(Path.Combine(outDir, SaveManager.FileName));
        Console.WriteLine($"the player's save is {(saved.AsSpan().SequenceEqual(after) ? "byte for byte what it was" : "!! CHANGED")}, with no backup made of it: {!File.Exists(Path.Combine(outDir, SaveManager.BackupOf(SaveManager.FileName)))}");
        var (crashOutcome, crashSave) = SaveManager.Load(Path.Combine(outDir, CrashReport.SaveName));
        var (ownOutcome, ownSave) = SaveManager.Load();
        Console.WriteLine($"the crash save reads as {crashOutcome}: {crashSave?.CurrentMapName} {crashSave?.PlayerGridX},{crashSave?.PlayerGridY} with {crashSave?.Money} of money; "
            + $"the player's own reads as {ownOutcome}: {ownSave?.CurrentMapName} {ownSave?.PlayerGridX},{ownSave?.PlayerGridY} with {ownSave?.Money}");

        string logs = Path.Combine(outDir, CrashReport.FolderName);
        string report = Directory.GetFiles(logs, "crash-*.txt").OrderBy(f => f, StringComparer.Ordinal).Last();
        Console.WriteLine($"---- {CrashReport.FolderName}/{Path.GetFileName(report)}");
        Console.WriteLine(File.ReadAllText(report).TrimEnd());
        Console.WriteLine("----");
        Console.WriteLine($"the note for the next start: {File.ReadAllText(Path.Combine(logs, CrashReport.PendingName)).ReplaceLineEndings(" ")}");

        // The next start, as GameEngine.Initialize does it: the save read, the note taken up, the title told
        var (loaded, save) = SaveManager.Load();
        var note = CrashReport.TakePending(CrashReport.SaveFolder);
        var title = new TitleScreen(save, TitleNotice.ForCrash(note, loaded), TitleNotice.ForSave(loaded));
        game.Title = title;
        game.State = GameState.Title;
        title.PressConfirm();
        Seconds(1.2f);
        title.PressConfirm();
        Seconds(0.5f); Shot("crash_1_title_notice");
        Console.WriteLine($"the next start: the save read as {loaded}; the title says \"{title.Notice?.Heading}\": {title.Notice?.Body}");
        title.PressConfirm();
        Seconds(1f); Shot("crash_2_title_menu");
        Console.WriteLine($"after the notice: phase {title.Phase}, entries {string.Join(", ", title.Entries)}");
        Console.WriteLine($"told once: the start after that finds {(CrashReport.TakePending(CrashReport.SaveFolder) == null ? "no note" : "!! the note again")}");
        game.State = GameState.Overworld;

        // A second crash: the first crash save becomes the second's backup, and both reports are kept
        game.Money += 1;
        Crash("Thrown on purpose a second time.");
        Console.WriteLine("left behind after a second crash:");
        Leftovers();
        Console.WriteLine($"the crash save now holds {SaveManager.Load(Path.Combine(outDir, CrashReport.SaveName)).Save?.Money} of money, the one before it "
            + $"{SaveManager.Read(File.ReadAllText(Path.Combine(outDir, SaveManager.BackupOf(CrashReport.SaveName))))?.Money}");
        CrashReport.TakePending(CrashReport.SaveFolder);

        Log.Info("The harness's crash mode is over.");
        Log.Close();
        Console.WriteLine($"---- the last lines of {Log.FileName}");
        foreach (string line in File.ReadLines(Path.Combine(outDir, Log.FileName)).TakeLast(12)) Console.WriteLine(line);
        Console.WriteLine("----");

        // The other thing Program.Main tells the game every frame (plan 16 · T3): whether its window is in front. With
        // the options' "Pause in the background" on and the window said not to be, nothing moves (the play time and the
        // field's clock stand still, the sound is turned down); said to be in front again, the game goes on. With the
        // option off, which it is unless chosen, the same word changes nothing
        float PlayTime() => game.BuildSave().PlayTimeSeconds;
        void Wait(string what)
        {
            float played = PlayTime();
            double life = game.World.Life.Now;
            Frames(30);
            Console.WriteLine($"{what}: waiting {engine.HeldForFocus}, the sound turned down {AudioManager.Away}; in 30 frames the play time moved {PlayTime() - played:0.00} s and the field's clock {game.World.Life.Now - life:0.00} s");
        }
        At("Sinnoh", 112, 880, Direction.Down);
        Frames(2);
        engine.WindowFocused = false;
        Wait("the option off, another window in front");
        engine.Settings.FocusPause = true;
        Wait("the option on, another window in front");
        engine.WindowFocused = true;
        Wait("the option on, the game's window in front again");
        engine.Settings.FocusPause = false;

        // The save this mode wrote is taken away again; the crash's own files stay to be read
        File.Delete(Path.Combine(outDir, SaveManager.FileName));
        File.Delete(Path.Combine(outDir, SaveManager.BackupOf(SaveManager.FileName)));
    }
}
