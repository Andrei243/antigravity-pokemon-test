using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW1, the rules with no game: the region's word on its field (a region with no word is the grid's), places
/// in whole millimetres and yaws in 65,536ths of a turn, the field's clock of 120 ticks a second whatever the frames,
/// an open map's markers and chunks, and the save's open place (to the millimetre, left out on the grid, and every
/// older save brought up to a version that stands on the grid).
/// </summary>
public class OpenWorldTests
{
    // ------------------------------------------------------------------ the region's word

    [Fact]
    public void ARegionWithNoWordIsWalkedOnTheGrid()
    {
        Assert.Equal(FieldKind.Grid, new RegionFile { Id = "Somewhere" }.FieldKind);
        Assert.Equal(FieldKind.Grid, new Region { Id = "Nowhere", Name = "Nowhere", Generation = 0 }.Field);
        // Every region of today is the grid's, whatever its file, but Galar, the first open region (plan 33 · GL1), and
        // Paldea (plan 34 · PD1)
        static bool Open(Region r) => r.Id is RegionDatabase.Galar or RegionDatabase.Paldea;
        Assert.All(RegionDatabase.All, r => Assert.Equal(Open(r) ? FieldKind.Open : FieldKind.Grid, r.Field));
        Assert.All(RegionDatabase.All.Where(r => !Open(r)), r => Assert.True(r.File?.Field == null, $"{r.Id}'s file names a field"));

        Assert.Equal(FieldKind.Grid, new RegionFile { Field = "grid" }.FieldKind);
        Assert.Equal(FieldKind.Open, new RegionFile { Field = "open" }.FieldKind);
        Assert.Equal(FieldKind.Open, new RegionFile { Field = " Open " }.FieldKind);
        Assert.Equal(FieldKind.Open, new Region { Id = "Galar", Name = "Galar", Generation = 8, File = new RegionFile { Field = "open" } }.Field);
        // A misspelling is refused, never read as the grid
        var refused = Assert.Throws<FormatException>(() => new RegionFile { Id = "Galar", Field = "opne" }.FieldKind);
        Assert.Contains("opne", refused.Message);
    }

    [Fact]
    public void TheWordIsReadFromARegionsFile()
    {
        var file = GameDataFiles.Deserialize<RegionFile>("{ \"id\": \"Galar\", \"field\": \"open\" }");
        Assert.Equal(FieldKind.Open, file.FieldKind);
        Assert.Null(GameDataFiles.Deserialize<RegionFile>("{ \"id\": \"Galar\" }").Field);
    }

    // ------------------------------------------------------------------ places and yaws

    [Fact]
    public void APlaceIsWholeMillimetres()
    {
        Assert.Equal(new OpenPos(1500, -250, 3), OpenPos.FromMetres(1.5, -0.25, 0.003));
        // Half a millimetre goes away from nought, either way
        Assert.Equal(new OpenPos(2, -2, 0), OpenPos.FromMetres(0.0015, -0.0015, 0.0004));
        Assert.Equal(1.5, new OpenPos(1500, 0, 0).MetresX);
        Assert.Equal("(1500 mm, -250 mm, 3 mm)", new OpenPos(1500, -250, 3).ToString());
        // ±2,147 km, and no further
        Assert.Equal(int.MaxValue, OpenPos.Millimetres(int.MaxValue / 1000.0));
        Assert.Throws<OverflowException>(() => OpenPos.Millimetres(2_200_000));
        // Distances are whole square millimetres, to 2,000 km apart, and refused further
        var a = new OpenPos(-1_000_000_000, 0, 0);
        var b = new OpenPos(1_000_000_000, 3, 4);
        Assert.Equal(2_000_000_000L * 2_000_000_000L + 16, a.GroundDistanceSquared(b));
        Assert.Throws<OverflowException>(() => new OpenPos(int.MinValue, 0, int.MinValue).GroundDistanceSquared(new OpenPos(int.MaxValue, 0, int.MaxValue)));
        Assert.Equal(25, new OpenPos(0, 0, 0).DistanceSquared(new OpenPos(0, 3, 4)));
        Assert.Equal(16, new OpenPos(0, 0, 0).GroundDistanceSquared(new OpenPos(0, 3, 4)));
        Assert.Equal(new OpenPos(10, 20, 30), new OpenPos(0, 0, 0).Plus(10, 20, 30));
    }

    [Fact]
    public void AYawIsSixtyFiveThousandthsOfATurnFromTheSouth()
    {
        Assert.Equal(0, OpenYaw.Of(Direction.Down));
        Assert.Equal(16384, OpenYaw.Of(Direction.Right));
        Assert.Equal(32768, OpenYaw.Of(Direction.Up));
        Assert.Equal(49152, OpenYaw.Of(Direction.Left));
        foreach (var d in new[] { Direction.Down, Direction.Right, Direction.Up, Direction.Left })
            Assert.Equal(d, OpenYaw.ToDirection(OpenYaw.Of(d)));
        // The nearest facing, an eighth either side
        Assert.Equal(Direction.Down, OpenYaw.ToDirection(8191));
        Assert.Equal(Direction.Right, OpenYaw.ToDirection(8192));
        Assert.Equal(Direction.Down, OpenYaw.ToDirection(65000));
        Assert.Equal(Direction.Left, OpenYaw.ToDirection(-16384));
        Assert.Equal(65535, OpenYaw.Normalize(-1));
        Assert.Equal(1, OpenYaw.Normalize(65537));
        // Toward a way on the ground: east is a quarter turn, north a half
        Assert.Equal(OpenYaw.East, OpenYaw.Toward(1, 0));
        Assert.Equal(OpenYaw.North, OpenYaw.Toward(0, -1));
        Assert.Equal(OpenYaw.West, OpenYaw.Toward(-1, 0));
        Assert.Equal(OpenYaw.South, OpenYaw.Toward(0, 0));
        Assert.Equal(OpenYaw.Turn / 8, OpenYaw.FromRadians(Math.PI / 4));
        Assert.Equal(Math.PI, OpenYaw.Radians(OpenYaw.North), 12);
    }

    // ------------------------------------------------------------------ the clock

    [Fact]
    public void TheClockRunsAHundredAndTwentyTicksASecondWhateverTheFrames()
    {
        // Sixty frames a second, as the game's tests and the harness feed them: two ticks a frame, exactly, for ten minutes
        var clock = new OpenClock();
        for (int f = 0; f < 60 * 600; f++) Assert.Equal(2, clock.Advance(1f / 60f));
        Assert.Equal(120L * 600, clock.Tick);
        Assert.Equal(600.0, clock.Seconds);

        // Frames of 144, 30 and an uneven mixture: whatever a second is cut into, it is 120 ticks, with nothing carried
        // but what the last frame left short of a tick
        foreach (float frame in new[] { 1f / 144f, 1f / 30f, 1f / 75f })
        {
            var at = new OpenClock();
            int frames = (int)Math.Round(60 / frame);
            for (int f = 0; f < frames; f++) at.Advance(frame);
            Assert.InRange(at.Tick, 120 * 60 - 1, 120 * 60);
        }

        // The same frames give the same ticks, and the ticks are those of the frames' units summed
        var random = new Random(7);
        var frames2 = Enumerable.Range(0, 5000).Select(_ => (float)(0.004 + random.NextDouble() * 0.03)).ToList();
        var one = new OpenClock();
        var two = new OpenClock();
        var ticksOne = frames2.Select(f => one.Advance(f)).ToList();
        var ticksTwo = frames2.Select(f => two.Advance(f)).ToList();
        Assert.Equal(ticksOne, ticksTwo);
        Assert.Equal(frames2.Sum(f => OpenClock.UnitsOf(f)) / OpenClock.Units, one.Tick);
        Assert.Equal(one.Pending, two.Pending);
        Assert.InRange(one.Between, 0f, 1f);
    }

    [Fact]
    public void AFrameTooLongRunsAQuarterOfASecondAndDropsTheRest()
    {
        var clock = new OpenClock();
        // A tenth of a second is twelve ticks, run whole (OW3's test of nothing tunnelling through a wall is at that frame)
        Assert.Equal(12, clock.Advance(0.1));
        Assert.Equal(0, clock.Dropped);
        // Three seconds held up: a quarter of a second runs, and the rest is dropped as a hitch
        Assert.Equal(OpenClock.MostTicksAFrame, clock.Advance(3.0));
        Assert.Equal(360 - OpenClock.MostTicksAFrame, clock.Dropped);
        Assert.Equal(12 + OpenClock.MostTicksAFrame, clock.Tick);
        // Nothing, or time running backwards, brings no tick
        Assert.Equal(0, clock.Advance(0));
        Assert.Equal(0, clock.Advance(-1));
        Assert.Equal(0, clock.Advance(double.NaN));
        // Half a tick is half a tick between
        var half = new OpenClock();
        Assert.Equal(0, half.Advance(0.5 / OpenClock.TicksPerSecond));
        Assert.Equal(0.5f, half.Between);
        Assert.Equal(1, half.Advance(0.5 / OpenClock.TicksPerSecond));
        Assert.Equal(0f, half.Between);
        Assert.Equal(240, OpenClock.TicksIn(2));
    }

    // ------------------------------------------------------------------ an open map

    [Fact]
    public void AnOpenMapKnowsItsMarkersAndItsChunks()
    {
        var map = new OpenMap("OpenLab", "Galar", new[] { new OpenMarker("start", new OpenPos(1000, 0, 2000), 70000) });
        Assert.Equal(70000 - 65536, map.Marker("START")!.Yaw);
        Assert.Null(map.Marker("nowhere"));
        Assert.Throws<ArgumentException>(() => map.Add(new OpenMarker("Start", new OpenPos(0, 0, 0))));
        Assert.Single(map.Markers);
        Assert.Throws<ArgumentException>(() => new OpenMap(" ", "Galar"));

        Assert.Equal((0, 0), OpenMap.ChunkOf(new OpenPos(0, 0, 0)));
        Assert.Equal((0, 0), OpenMap.ChunkOf(new OpenPos(31_999, 500_000, 31_999)));
        Assert.Equal((1, 0), OpenMap.ChunkOf(new OpenPos(32_000, 0, 0)));
        Assert.Equal((-1, -1), OpenMap.ChunkOf(new OpenPos(-1, 0, -32_000)));
        Assert.Equal((-2, 0), OpenMap.ChunkOf(new OpenPos(-32_001, 0, 5)));
    }

    [Fact]
    public void AnOpenFieldCountsItsTicksAndRunsEach()
    {
        var field = new OpenField(new OpenMap("OpenLab", "Galar"), new OpenPos(5, 6, 7), -1);
        Assert.Equal(65535, field.Player.Yaw);
        int ran = 0;
        Assert.Equal(2, field.Advance(1f / 60f, _ => ran++));
        Assert.Equal(2, ran);
        Assert.Equal(new OpenPlace("OpenLab", new OpenPos(5, 6, 7), 65535), field.Place);
    }

    // ------------------------------------------------------------------ the save

    [Fact]
    public void AnOpenPlaceRoundTripsThroughTheSaveToTheMillimetre()
    {
        var place = new OpenPlace("OpenLab", new OpenPos(-1_234_567, 89_012, 2_000_000_001), 40_000);
        var save = new SaveData { SaveVersion = SaveData.CurrentSave, OpenPlace = SavedOpenPlace.From(place) };
        var back = SaveManager.Read(SaveManager.Write(save))!;
        Assert.Equal(place, back.OpenPlace!.ToPlace());
        Assert.Equal(SaveData.CurrentSave, back.SaveVersion);
        // On the grid it is left out of the file, so a save of the grid is written as it was
        Assert.DoesNotContain("OpenPlace", SaveManager.Write(new SaveData { SaveVersion = SaveData.CurrentSave }));
        // A yaw out of range read from a file is brought into a turn
        Assert.Equal(1, new SavedOpenPlace { Map = "OpenLab", Yaw = 65537 }.ToPlace().Yaw);
    }

    [Fact]
    public void EveryOlderSaveIsBroughtUpToAVersionThatStandsOnTheGrid()
    {
        Assert.Equal(3, SaveData.OpenPlaces);
        Assert.Equal(SaveData.OpenPlaces, SaveData.CurrentSave);
        foreach (int version in new[] { 0, SaveData.PokemonIdsGiven, SaveData.FreePositions })
        {
            var old = new SaveData
            {
                SaveVersion = version, CurrentMapName = "Sinnoh", PlayerGridX = 116, PlayerGridY = 886,
                // (a field no older game wrote, as a hand-edited file might have it)
                OpenPlace = new SavedOpenPlace { Map = "OpenLab", X = 1 }
            };
            SaveMigration.Upgrade(old);
            Assert.Equal(SaveData.CurrentSave, old.SaveVersion);
            Assert.Null(old.OpenPlace);
            Assert.Equal(("Sinnoh", 116, 886), (old.CurrentMapName, old.PlayerGridX, old.PlayerGridY));
        }
        // And a save of today keeps its open place
        var today = new SaveData { SaveVersion = SaveData.CurrentSave, OpenPlace = new SavedOpenPlace { Map = "OpenLab", Z = 5 } };
        SaveMigration.Upgrade(today);
        Assert.Equal(5, today.OpenPlace!.Z);
    }
}
