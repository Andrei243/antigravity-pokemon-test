using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T13: the field fuzzer. A game with no window, past the first chapter, is set down at seeded spots all
/// over the open world of Sinnoh and walked at random with random presses among the steps (<see cref="FieldFuzzer"/>),
/// failing on an exception, a softlock, a script gone wrong or a value out of range. Two minutes of game time run with
/// the suite on every push; <c>FIELD_FUZZ_SECONDS=3600</c> is the nightly's hour, <c>FIELD_FUZZ_SEED=n</c> another walk.
/// </summary>
[Collection("Shared clocks and dice")]
public class FieldFuzzTests
{
    /// <summary>How long the fuzzer stays at one spot, in seconds of game time.</summary>
    private const double SecondsASpot = 10;

    private readonly ITestOutputHelper output;

    public FieldFuzzTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void TheFieldFuzzerWalksTheOpenWorldWithNoWindowAndFindsNothingWrong()
    {
        double seconds = double.TryParse(Environment.GetEnvironmentVariable("FIELD_FUZZ_SECONDS"), out double s) ? s : 120;
        int seed = int.TryParse(Environment.GetEnvironmentVariable("FIELD_FUZZ_SEED"), out int n) ? n : 3;
        Headless.Play(engine =>
        {
            Headless.PastTheOpening(engine);
            var game = engine.Drive;
            var sinnoh = MapDatabase.Get("Sinnoh");
            var spots = FieldFuzzer.Spots(sinnoh, (int)Math.Ceiling(seconds / SecondsASpot), seed);
            Assert.NotEmpty(spots);
            var fuzzer = new Autoplayer(engine, new FieldFuzzer(seed));
            var started = DateTime.UtcNow;
            var places = new HashSet<string>();
            string? unsettled = null;
            const float dt = 1f / 60f;
            foreach (var (x, y) in spots)
            {
                // Set down only with the field the player's: whatever the last spot left going is played out first, as
                // the bot plays (a field that never comes back is the softlock detector's to name)
                if (fuzzer.Stopped) break;
                if (!fuzzer.Settle(60)) { unsettled = $"{game.State} on {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}, script {game.ScriptNow}, line '{game.Dialogue.CurrentLine}'"; break; }
                foreach (var p in game.Party.Members) p.HealFull();
                game.Place(sinnoh, x, y, Direction.Down);
                game.ArriveOnMap();
                places.Add(sinnoh.AreaAt(x, y)?.Key ?? "?");
                for (double t = 0; t < SecondsASpot && !fuzzer.Stopped; t += dt) fuzzer.Frame(dt);
            }
            fuzzer.Release();
            var report = fuzzer.Report;
            output.WriteLine($"{report.Seconds:0} s of game time at {spots.Count} spots in {places.Count} places, in {(DateTime.UtcNow - started).TotalSeconds:0} s");
            output.WriteLine(string.Join(", ", places.Order()));
            output.WriteLine(report.ToMarkdown());
            Assert.Empty(report.Exceptions);
            Assert.Empty(report.Softlocks);
            Assert.Empty(report.OutOfRange);
            Assert.Empty(report.ScriptsGoneWrong);
            Assert.Null(unsettled);
        });
    }

    /// <summary>
    /// Beyond Sinnoh's overworld (plan 16 · T13's leftover): one spot on each of maps drawn by the seed from every map
    /// the game has but the overworld of Sinnoh, the rooms, the caves and the other regions' maps among them, ten
    /// seconds each. <c>FIELD_FUZZ_ROOMS_SECONDS</c> is how long in all (120 unless said), <c>FIELD_FUZZ_SEED</c> the walk.
    /// </summary>
    [Fact]
    public void TheFieldFuzzerWalksTheRoomsCavesAndOtherRegionsAndFindsNothingWrong()
    {
        double seconds = double.TryParse(Environment.GetEnvironmentVariable("FIELD_FUZZ_ROOMS_SECONDS"), out double s) ? s : 120;
        int seed = int.TryParse(Environment.GetEnvironmentVariable("FIELD_FUZZ_SEED"), out int n) ? n : 3;
        Headless.Play(engine =>
        {
            Headless.PastTheOpening(engine);
            var game = engine.Drive;
            var maps = MapDatabase.MapNames.Where(name => name != "Sinnoh").OrderBy(name => name, StringComparer.Ordinal)
                .Select(MapDatabase.TryGet).OfType<Map>().ToList();
            var places = FieldFuzzer.Places(maps, (int)Math.Ceiling(seconds / SecondsASpot), seed);
            Assert.NotEmpty(places);
            var fuzzer = new Autoplayer(engine, new FieldFuzzer(seed));
            string? unsettled = null;
            const float dt = 1f / 60f;
            foreach (var (map, x, y) in places)
            {
                if (fuzzer.Stopped) break;
                if (!fuzzer.Settle(60)) { unsettled = $"{game.State} on {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}, script {game.ScriptNow}, line '{game.Dialogue.CurrentLine}'"; break; }
                foreach (var p in game.Party.Members) p.HealFull();
                game.Place(map, x, y, Direction.Down);
                game.ArriveOnMap();
                for (double t = 0; t < SecondsASpot && !fuzzer.Stopped; t += dt) fuzzer.Frame(dt);
            }
            fuzzer.Release();
            var report = fuzzer.Report;
            output.WriteLine($"{report.Seconds:0} s of game time at {places.Count} places: {string.Join(", ", places.Select(p => $"{p.Map.Name} {p.X},{p.Y}"))}");
            output.WriteLine(report.ToMarkdown());
            Assert.Empty(report.Exceptions);
            Assert.Empty(report.Softlocks);
            Assert.Empty(report.OutOfRange);
            Assert.Empty(report.ScriptsGoneWrong);
            Assert.Null(unsettled);
        });
    }

    [Fact]
    public void ThePlacesBeyondTheOverworldAreOneSpotAMapAndTheSameForASeed()
    {
        var maps = MapDatabase.MapNames.Where(name => name != "Sinnoh").OrderBy(name => name, StringComparer.Ordinal)
            .Select(MapDatabase.TryGet).OfType<Map>().ToList();
        var places = FieldFuzzer.Places(maps, 30, 4);
        Assert.Equal(30, places.Count);
        Assert.Equal(30, places.Select(p => p.Map.Name).Distinct().Count());
        Assert.Equal(places.Select(p => (p.Map.Name, p.X, p.Y)), FieldFuzzer.Places(maps, 30, 4).Select(p => (p.Map.Name, p.X, p.Y)));
        Assert.All(places, p => Assert.True(p.Map.IsWalkable(p.X, p.Y) && p.Map.GetWarpAt(p.X, p.Y) == null));
        // Rooms and maps of other regions among them, not Sinnoh's overworld alone
        Assert.Contains(places, p => !p.Map.IsStreamed);
        Assert.Contains(places, p => RegionDatabase.RegionOfMap(p.Map.Name) is { } r && r.Id != RegionDatabase.First.Id);
    }

    [Fact]
    public void TheFieldFuzzerIsTheSameForTheSameSeedAndMostlyWalks()
    {
        var a = new FieldFuzzer(5);
        var b = new FieldFuzzer(5);
        var fromA = Enumerable.Range(0, 3000).Select(_ => a.Next()).ToList();
        Assert.Equal(fromA, Enumerable.Range(0, 3000).Select(_ => b.Next()).ToList());
        Assert.NotEqual(fromA, Enumerable.Range(0, 3000).Select(_ => new FieldFuzzer(6).Next()).ToList());
        int walking = fromA.Count(f => f.IsDown(GameAction.Up) || f.IsDown(GameAction.Down) || f.IsDown(GameAction.Left) || f.IsDown(GameAction.Right));
        Assert.InRange(walking, 3000 / 2, 3000);
        Assert.DoesNotContain(fromA, f => f.IsDown(GameAction.Fullscreen) || f.IsDown(GameAction.Mute) || f.IsDown(GameAction.Screenshot));
    }

    [Fact]
    public void TheSpotsAreOpenGroundOfOpenAreas()
    {
        var sinnoh = MapDatabase.Get("Sinnoh");
        var spots = FieldFuzzer.Spots(sinnoh, 40, 9);
        Assert.Equal(40, spots.Count);
        Assert.Equal(spots, FieldFuzzer.Spots(sinnoh, 40, 9));
        Assert.All(spots, s =>
        {
            Assert.True(sinnoh.IsWalkable(s.X, s.Y));
            Assert.True(sinnoh.AreaAt(s.X, s.Y)!.Open);
            Assert.Null(sinnoh.GetWarpAt(s.X, s.Y));
        });
        // Spread over the region, not one town
        Assert.True(spots.Select(s => sinnoh.AreaAt(s.X, s.Y)!.Key).Distinct().Count() > 10);
    }
}
