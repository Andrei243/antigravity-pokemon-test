partial class Harness
{
    // V10, the run timer: wy_timer_field (the corner over the field in front of the house, with the options' RUN TIMER
    // row on the Story category and the Coal Badge's split taken) and wy_timer_battle (the same corner over a wild
    // battle's commands). The harness fixes the wall clock, so the real time would stand at nought: the timer is set to
    // readings of its own before the Badge is given, as an hour of a run would leave it. It prints the timer, its splits
    // and the file the row wrote, and puts the row back as it was
    void TimerMode()
    {
        var before = engine.Settings.RunTimer;
        engine.Settings.RunTimer = TimerCategory.Story;
        BeginRun(RunRules.Normal);
        var timer = game.RunState.Timer!;
        timer.RealSeconds = 3723.4;
        timer.GameSeconds = 3598.2;
        game.Story.GiveBadge(RegionDatabase.Sinnoh, "Coal");
        Frames(2);
        timer.RealSeconds += 61.3;
        timer.GameSeconds += 58.9;
        Frames(10);
        Console.WriteLine($"timer: real {RunTimer.Format(timer.RealSeconds)}, game {RunTimer.Format(timer.GameSeconds)}; splits {string.Join(", ", timer.Splits.Select(s => $"{s.Id} at {RunTimer.Format(s.RealSeconds)}"))}");
        string file = System.IO.Path.Combine(outDir, SavePaths.SplitsName);
        Console.WriteLine($"timer: {SavePaths.SplitsName} {(System.IO.File.Exists(file) ? "written, " + new System.IO.FileInfo(file).Length + " bytes" : "not written")}");
        Shot("wy_timer_field");

        var b = StartBattle("Shinx", 5);
        ToMainMenu(b);
        Shot("wy_timer_battle");
        game.Battle = null;
        game.State = GameState.Overworld;
        engine.Settings.RunTimer = before;
    }
}
