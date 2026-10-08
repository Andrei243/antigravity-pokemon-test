partial class Harness
{
    // ---------------------------------------------------------------- times of day
    public void TimesMode()
    {
        // Twinleaf, Route 201 and a battle at each of Platinum's five times of day, then each quality preset
        foreach (var time in new[] { TimeOfDay.Morning, TimeOfDay.Day, TimeOfDay.Twilight, TimeOfDay.Night, TimeOfDay.LateNight })
        {
            engine.Settings.TimeOfDay = time;
            engine.ApplySettings(window: false);
            string tag = time.ToString().ToLowerInvariant();
            GoTo("TwinleafTown", 11, 8, Direction.Down); Frames(2); Shot($"time_{tag}_twinleaf");
            GoTo("SandgemTown", 14, 8, Direction.Up); Frames(2); Shot($"time_{tag}_sandgem");
            var tb = StartBattle("Shinx", 5);
            ToMainMenu(tb);
            Shot($"time_{tag}_battle");
        }
        engine.Settings.TimeOfDay = TimeOfDay.Day;

        foreach (var quality in new[] { GraphicsQuality.Low, GraphicsQuality.Medium, GraphicsQuality.High })
        {
            engine.Settings.Quality = quality;
            engine.ApplySettings(window: false);
            string tag = quality.ToString().ToLowerInvariant();
            GoTo("TwinleafTown", 11, 8, Direction.Down); Frames(2); Shot($"quality_{tag}_twinleaf");
            Timing($"{tag} field");
            var qb = StartBattle("Shinx", 5);
            ToMainMenu(qb);
            Shot($"quality_{tag}_battle");
            Timing($"{tag} battle");
        }
    }
}
