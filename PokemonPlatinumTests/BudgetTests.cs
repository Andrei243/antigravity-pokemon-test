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

    private void Hold(string what, double measuredMs, int runs, Action run)
    {
        if (Mode == "off") return;
        double median = MedianMs(runs, run), budget = measuredMs * Headroom;
        output.WriteLine($"{what}: median {median:F1} ms, budget {budget:F1} ms (measured {measuredMs} ms on 2026-10-26)");
        Assert.True(median <= budget, $"{what} took {median:F1} ms (median of {runs}); its budget is {budget:F1} ms, {Headroom}x the {measuredMs} ms measured on 2026-10-26.");
    }

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
        Hold("ScriptLibrary.Load", 100, 3, () => ScriptLibrary.Load());
    }

    [Fact]
    public void EverySongParsesInTime()
    {
        var files = Directory.GetFiles(GameDataFiles.PathOf("music"), "*.mml", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        var texts = files.Select(f => (Path.GetFileNameWithoutExtension(f), File.ReadAllText(f))).ToList();
        Hold($"Mml.Parse of {texts.Count} songs", 40, 3, () => { foreach (var (id, text) in texts) Mml.Parse(id, text); });
    }

    [Fact]
    public void ASaveRoundTripsInTime()
    {
        string folder = Path.Combine(Repository.Root, "PokemonPlatinumTests", "Fixtures", "saves");
        string biggest = Directory.GetFiles(folder, "*.json").OrderByDescending(f => new FileInfo(f).Length).First();
        string json = File.ReadAllText(biggest);
        Hold("SaveManager.Read and Write of " + Path.GetFileName(biggest), 1.5, 5, () => SaveManager.Write(SaveManager.Read(json)!));
    }

    [Fact]
    public void TheWorldsMapsStayUnderTheMemoryCeiling()
    {
        if (Mode == "off") return;
        MapDatabase.Initialize();
        long megabytes = GC.GetTotalMemory(true) / (1024 * 1024);
        long ceiling = (long)(190 * (Mode == "strict" ? 1.25 : 2.0));
        output.WriteLine($"managed memory after the world's maps: {megabytes} MB, ceiling {ceiling} MB (measured 190 MB on 2026-10-26)");
        Assert.True(megabytes <= ceiling, $"{megabytes} MB held after the world's maps; the ceiling is {ceiling} MB.");
    }
}
