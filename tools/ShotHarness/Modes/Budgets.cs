using System.Diagnostics;
using System.Text.Json;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;

partial class Harness
{
    /// <summary>
    /// The budgets that need a window (plan 16 · T10; not part of <c>all</c>; <c>BudgetTests</c> holds the rest): the chunk
    /// ahead of the player must be prepared (<c>MapScene.Prepare</c>, ground baked and meshes laid out on a worker) well
    /// inside the time the 2.5 ms-a-frame uploads give it, and the engine's start must stay near what it was. Each chunk
    /// is prepared several times, its median held to <c>MeasuredPrepareMs</c> (three times the figure measured on the
    /// GTX 970 machine; fill it in on the first run there, the date beside it); <c>BUDGETS=strict</c> halves the headroom.
    /// It writes <c>budgets.json</c> in the out dir (the medians and the start-up's phases) for the nightly's month, and
    /// prints a line starting <c>!!</c> for every budget broken. Written where no window could be opened: not yet run.
    /// </summary>
    public void BudgetsMode()
    {
        // The figure measured per chunk, in milliseconds; 0 until the first run on the Windows machine (then no budget is held)
        const double MeasuredPrepareMs = 0;   // measured: not yet (T10 was written without a window)
        const double MeasuredStartMs = 0;     // the engine's start-up, as profile prints it
        double headroom = string.Equals(Environment.GetEnvironmentVariable("BUDGETS"), "strict", StringComparison.OrdinalIgnoreCase) ? 1.5 : 3.0;
        int broken = 0;

        var map = MapDatabase.Get("Sinnoh");
        var results = new Dictionary<string, double>();
        // A town, a route and open country, as the player walks into them
        foreach (var (name, cx, cy) in new[] { ("twinleaf", 3, 27), ("jubilife", 4, 22), ("route_204", 5, 20) })
        {
            var window = MapScene.ChunkWindow(map, cx, cy);
            MapScene.Warm(map, window);
            MapScene.Prepare(map, window); // not counted
            var times = new List<double>();
            for (int i = 0; i < 5; i++)
            {
                var watch = Stopwatch.StartNew();
                MapScene.Prepare(map, window);
                times.Add(watch.Elapsed.TotalMilliseconds);
            }
            times.Sort();
            double median = times[times.Count / 2];
            results["prepare_" + name] = median;
            double budget = MeasuredPrepareMs * headroom;
            Console.WriteLine($"MapScene.Prepare of {name} ({cx}, {cy}): median {median:F1} ms" + (MeasuredPrepareMs > 0 ? $", budget {budget:F1} ms" : ", no budget yet"));
            if (MeasuredPrepareMs > 0 && median > budget) { Console.WriteLine($"!! {name}'s chunk took {median:F1} ms to prepare; its budget is {budget:F1} ms"); broken++; }
        }

        // Plan 30 · FM3: a grown region's crowd of thirty on Aquacorde's promenade, as live rigs and impostors; its figure is
        // a mean frame, the plan's done-when 8 ms on High (judged by `profile`'s freewalk_crowd floor, never by this line)
        const double MeasuredCrowdMs = 0;     // measured: not yet (FM3 was written on a card other runs shared)
        {
            var (stood, x, y, facing) = (game.Map, game.Player.GridX, game.Player.GridY, game.Player.Facing);
            At("Kalos", CrowdX, CrowdZ, Direction.Down);
            Frames(20);
            LayTheCrowd();
            engine.RegionKitOverride = CharacterKit.Grown;
            WaitForTheCrowd();
            var watch = Stopwatch.StartNew();
            Frames(60);
            double crowd = watch.Elapsed.TotalMilliseconds / 60;
            results["freewalk_crowd_frame_ms"] = crowd;
            results["freewalk_crowd_live"] = game.World.LiveRigs.Live;
            results["freewalk_crowd_impostors"] = game.World.LiveRigs.Impostors;
            Console.WriteLine($"freewalk_crowd: {crowd:F2} ms a frame, {game.World.LiveRigs.Live} live and {game.World.LiveRigs.Impostors} impostors"
                + (MeasuredCrowdMs > 0 ? $", budget {MeasuredCrowdMs * headroom:F2} ms" : ", no budget yet (the plan's is 8 ms on High)"));
            if (MeasuredCrowdMs > 0 && crowd > MeasuredCrowdMs * headroom) { Console.WriteLine($"!! the crowd of thirty took {crowd:F2} ms a frame"); broken++; }
            engine.RegionKitOverride = null;
            LiftTheCrowd();
            game.Place(stood, x, y, facing);
        }

        results["start_ms"] = startMs;
        if (MeasuredStartMs > 0 && startMs > MeasuredStartMs * headroom)
        {
            Console.WriteLine($"!! the engine took {startMs:F0} ms to start; its budget is {MeasuredStartMs * headroom:F0} ms");
            broken++;
        }
        var phases = new Dictionary<string, double>();
        foreach (var phase in Log.Phases) phases[phase.Name] = phases.GetValueOrDefault(phase.Name) + phase.Milliseconds;
        string file = Path.Combine(outDir, "budgets.json");
        File.WriteAllText(file, JsonSerializer.Serialize(new { medians = results, phases }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"{results.Count} figures and {phases.Count} start-up phases written to {file}; {broken} budget(s) broken");
    }
}
