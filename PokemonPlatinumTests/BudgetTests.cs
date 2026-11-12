using System.Diagnostics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

[CollectionDefinition("Budgets", DisableParallelization = true)]
public class BudgetsCollection { }

/// <summary>
/// Performance budgets (plan 16 · T10): each operation that makes the game start slowly or a chunk late is run several
/// times and its median held to a budget. A budget is the figure measured on the day (in the test, with its date)
/// times three, the headroom for a slower machine, a busy one and a test run beside others.
/// <c>BUDGETS=off</c> skips them all; <c>BUDGETS=strict</c> cuts the headroom to one and a half (the nightly).
/// What needs a window (<c>MapScene.Prepare</c>, which looks up GPU textures) is the harness's <c>budgets</c> mode.
/// </summary>
[Collection("Budgets")]
public class BudgetTests
{
    private readonly ITestOutputHelper output;
    public BudgetTests(ITestOutputHelper output) => this.output = output;

    internal static string Mode => (Environment.GetEnvironmentVariable("BUDGETS") ?? "").Trim().ToLowerInvariant();

    /// <summary>The headroom over the measured figure: three times, or one and a half in the nightly's strict mode.</summary>
    internal static double Headroom => Mode == "strict" ? 1.5 : 3.0;

    /// <summary>The median, in milliseconds, of <paramref name="runs"/> runs after one that is not counted.</summary>
    internal static double MedianMs(int runs, Action run)
    {
        run();
        var times = new List<double>();
        for (int i = 0; i < runs; i++)
        {
            var watch = Stopwatch.StartNew();
            run();
            times.Add(watch.Elapsed.TotalMilliseconds);
        }
        times.Sort();
        return times[times.Count / 2];
    }

    private void Hold(string what, double measuredMs, int runs, Action run, string measuredOn = "2026-10-26")
    {
        if (Mode == "off") return;
        double median = MedianMs(runs, run), budget = measuredMs * Headroom;
        output.WriteLine($"{what}: median {median:F1} ms, budget {budget:F1} ms (measured {measuredMs} ms on {measuredOn})");
        Record(what, median);
        Assert.True(median <= budget, $"{what} took {median:F1} ms (median of {runs}); its budget is {budget:F1} ms, {Headroom}x the {measuredMs} ms measured on {measuredOn}.");
    }

    /// <summary>
    /// The nightly's record (plan 16 · T10): with <c>BUDGETS_JSON</c> naming a file, each median measured is added to it
    /// as a line of JSON (<c>{"what": …, "ms": …}</c>), which <c>tools/dev/budgets-month.sh</c> holds to the month's.
    /// </summary>
    internal static void Record(string what, double ms)
    {
        string? file = Environment.GetEnvironmentVariable("BUDGETS_JSON");
        if (string.IsNullOrWhiteSpace(file)) return;
        string line = System.Text.Json.JsonSerializer.Serialize(new { what, ms = Math.Round(ms, 3) }) + "\n";
        lock (RecordGate) File.AppendAllText(file, line);
    }

    private static readonly object RecordGate = new();
    private static readonly Lazy<Map> Sinnoh = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh").BuildMaps().Single(m => m.Name == "Sinnoh"));

    [Fact]
    public void AChunksGroundBakesInTime()
    {
        if (Mode == "off") return;
        var map = Sinnoh.Value;
        var window = MapScene.ChunkWindow(map, 3, 27);
        Hold("PixelGround.Bake of one chunk", 250, 5, () => PixelGround.Bake(map, window, PixelGround.ChunkPad, worldSeeds: true, out _));
    }

    [Fact]
    public void ThePokemonAndTheCharactersMeshInTime()
    {
        if (Mode == "off") return;
        bool was = SdfCache.Enabled;
        SdfCache.Enabled = false; // the mesher itself, not the cache's file
        try
        {
            Hold("SdfMesher.Mesh of Turtwig (PokemonModels.Build)", 400, 3, () => PokemonModels.Build("Turtwig"));
            Hold("SdfMesher.Mesh of a rig (CharacterModels.Build)", 1200, 3, () => CharacterModels.Build("PLAYER"));
        }
        finally { SdfCache.Enabled = was; }
    }

    /// <summary>
    /// The open world's baker (plan 32 · OW10: <c>budgets.json</c>'s bake times, GPU-free): the open test world's
    /// descriptions read and turned into a land, the hill's chunk (the tunnel's and the cavern's carves in it) meshed at
    /// level 0 alone and at all three levels (level 0 thinned twice), and the navigation mesh of the lab's sixteen chunks.
    /// </summary>
    [Fact]
    public void TheOpenWorldsBakerBakesInTime()
    {
        if (Mode == "off") return;
        string folder = OpenWorldBakeTests.LabFolder;
        Hold("OpenWorldBake: the lab's descriptions read and made a land", 1, 3,
            () => OpenWorldBake.RegionDescription.Read(folder).Land(), "2026-11-09");
        var description = OpenWorldBake.RegionDescription.Read(folder);
        var land = description.Land();
        Hold("OpenWorldBake: the hill's chunk at level 0 (RegionBake.Finest)", 210, 3,
            () => OpenWorldBake.RegionBake.Finest(land, description.World, 0, 0), "2026-11-09");
        Hold("OpenWorldBake: the hill's chunk at three levels (RegionBake.Chunk)", 280, 3,
            () => OpenWorldBake.RegionBake.Chunk(land, description.World, 0, 0), "2026-11-09");
        var level0 = OpenWorldBake.RegionBake.ChunksOf(description.World).Select(c => OpenWorldBake.RegionBake.Finest(land, description.World, c.X, c.Z)).ToList();
        var (solids, waters) = OpenWorldBake.RegionBake.PlacedOf(description);
        Hold("OpenWorldBake: the lab's navigation mesh (NavBake.Build of 16 chunks)", 720, 2,
            () => OpenWorldBake.NavBake.Build(level0, solids, waters), "2026-11-09");
    }

    [Fact]
    public void TheDataFilesLoadInTime()
    {
        // The static constructors of the databases run once a process, so what is held is what they do: read their file
        Hold("species.json (PokemonDatabase)", 55, 3, () => GameDataFiles.Load<List<PokemonSpecies>>(PokemonDatabase.FileName));
        Hold("moves.json (MoveDatabase)", 4, 3, () => GameDataFiles.Load<List<MoveData>>(MoveDatabase.FileName));
        Hold("abilities.json (AbilityDatabase)", 1, 3, () => GameDataFiles.Load<List<AbilityDatabase.AbilityRecord>>(AbilityDatabase.FileName));
        Hold("items.json (ItemDatabase)", 3, 3, () => GameDataFiles.Load<List<ItemData>>(ItemDatabase.FileName));
        Hold("trainers.json (TrainerDatabase's first read)", 4, 3, () => GameDataFiles.Load<List<TrainerRecord>>(TrainerDatabase.FileName));
    }

    [Fact]
    public void TheWorldLoadsAndBuildsInTime()
    {
        Hold("World.LoadAll and WorldMapBuilder.Build of Sinnoh", 1000, 2, () => World.LoadAll().Single(w => w.Index.Region == "Sinnoh").BuildMaps());
    }

    [Fact]
    public void TheMapsAndTheScriptsLoadInTime()
    {
        Hold("MapDatabase.Initialize", 4700, 2, MapDatabase.Initialize);
        Hold("ScriptLibrary.Load", 180, 3, () => ScriptLibrary.Load(), "2026-11-03");
    }

    [Fact]
    public void EverySongParsesInTime()
    {
        var files = Directory.GetFiles(GameDataFiles.PathOf("music"), "*.mml", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        var texts = files.Select(f => (Path.GetFileNameWithoutExtension(f), File.ReadAllText(f))).ToList();
        Hold($"Mml.Parse of {texts.Count} songs", 72, 3, () => { foreach (var (id, text) in texts) Mml.Parse(id, text); }, "2026-11-03");
    }

    [Fact]
    public void ASaveRoundTripsInTime()
    {
        string folder = Path.Combine(Repository.Root, "PokemonPlatinumTests", "Fixtures", "saves");
        string biggest = Directory.GetFiles(folder, "*.json").OrderByDescending(f => new FileInfo(f).Length).First();
        string json = File.ReadAllText(biggest);
        Hold("SaveManager.Read and Write of " + Path.GetFileName(biggest), 2.6, 5, () => SaveManager.Write(SaveManager.Read(json)!), "2026-11-03");
    }

    /// <summary>
    /// The start-up's phases (plan 16 · T3) as a game with no window goes through them (plan 16 · T12), written into the
    /// nightly's file a line a phase (<c>start-up: moves</c>, <c>start-up: maps</c>, …) for the month's rule to hold
    /// (plan 16 · T10). No budget of its own: nothing here was measured on the Windows machine, and in a test process
    /// the databases' first reads may have happened already, so these are the start-up as a runner repeats it, not a
    /// player's cold one; the screen's phases (render targets, menu sprites) are the harness's and stay out.
    /// </summary>
    [Fact]
    public void TheHeadlessStartUpsPhasesGoIntoTheNightlysFile()
    {
        if (Mode == "off") return;
        string folder = Path.Combine(Path.GetTempPath(), "budget-startup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            int before = PokemonPlatinumEngine.Core.Diagnostics.Log.Phases.Count;
            var watch = Stopwatch.StartNew();
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            double all = watch.Elapsed.TotalMilliseconds;
            var phases = PokemonPlatinumEngine.Core.Diagnostics.Log.Phases.Skip(before).ToList();
            engine.Close();
            Assert.Contains(phases, p => p.Name == "maps");
            Assert.Contains(phases, p => p.Name == "scripts");
            foreach (var phase in phases)
            {
                output.WriteLine($"start-up: {phase.Name}: {phase.Milliseconds:F1} ms");
                Record($"start-up: {phase.Name}", phase.Milliseconds);
            }
            output.WriteLine($"headless start-up, in all: {all:F1} ms");
            Record("headless start-up, in all", all);
        }
        finally
        {
            SavePaths.Current = paths;
            Directory.Delete(folder, recursive: true);
        }
    }

}
