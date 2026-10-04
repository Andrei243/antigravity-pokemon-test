using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumTests;

/// <summary>What must not run beside the other tests: it sets things the whole process shares.</summary>
[CollectionDefinition("Shared clocks and dice", DisableParallelization = true)]
public class SharedClocksAndDice;

/// <summary>
/// Plan 04 · G11: the tools that measure a frame and make a run repeat (the profiler, the clock and the dice
/// the screenshot harness sets), and the faster ways of baking a chunk's ground, which must bake exactly what
/// the slower ways did.
/// </summary>
[Collection("Shared clocks and dice")]
public class FrameTests
{
    // ------------------------------------------------------------------ the profiler

    [Fact]
    public void TheProfilerGivesEachSectionTheTimeSinceTheLastLap()
    {
        try
        {
            FrameProfiler.Enabled = true;
            FrameProfiler.Reset();
            for (int frame = 0; frame < 3; frame++)
            {
                Thread.Sleep(4);
                FrameProfiler.LapCpu(FrameSection.Update);
                FrameProfiler.Count(1200);
                FrameProfiler.Count(800);
                Thread.Sleep(8);
                FrameProfiler.LapCpu(FrameSection.Scene);
                FrameProfiler.EndFrame();
            }

            Assert.Equal(3, FrameProfiler.Frames);
            double update = FrameProfiler.Average(FrameSection.Update), scene = FrameProfiler.Average(FrameSection.Scene);
            Assert.InRange(update, 3.0, 80.0);
            Assert.InRange(scene, 7.0, 80.0);
            Assert.Equal(0.0, FrameProfiler.Average(FrameSection.Shadows));
            // The sections add up to the frame
            Assert.Equal(update + scene, FrameProfiler.AverageFrame, 6);

            // What was drawn belongs to the section that ends after it
            Assert.Equal((2.0, 2000.0), FrameProfiler.Drawn(FrameSection.Scene));
            Assert.Equal((0.0, 0.0), FrameProfiler.Drawn(FrameSection.Update));

            // One line with every section in a frame's order, and the meshes of those that draw
            string report = FrameProfiler.Report();
            Assert.Contains("update", report);
            Assert.Contains("scene", report);
            Assert.Contains("(2 meshes, 2k tris)", report);
            Assert.True(report.IndexOf("update", StringComparison.Ordinal) < report.IndexOf("shadows", StringComparison.Ordinal));
            Assert.True(report.IndexOf("composite", StringComparison.Ordinal) < report.IndexOf("swap", StringComparison.Ordinal));
            foreach (var section in Enum.GetValues<FrameSection>()) Assert.Contains(section.ToString().ToLowerInvariant(), report);

            FrameProfiler.Reset();
            Assert.Equal(0, FrameProfiler.Frames);
            Assert.Equal(0.0, FrameProfiler.AverageFrame);
        }
        finally
        {
            FrameProfiler.Enabled = false;
            FrameProfiler.Reset();
        }
    }

    [Fact]
    public void SwitchedOffTheProfilerMeasuresNothing()
    {
        FrameProfiler.Enabled = false;
        FrameProfiler.Reset();
        Thread.Sleep(2);
        FrameProfiler.LapCpu(FrameSection.Update);
        FrameProfiler.Count(500);
        FrameProfiler.LapCpu(FrameSection.Scene);
        FrameProfiler.EndFrame();
        Assert.Equal(0, FrameProfiler.Frames);
        Assert.Equal(0.0, FrameProfiler.AverageFrame);
        Assert.Equal((0.0, 0.0), FrameProfiler.Drawn(FrameSection.Scene));
    }

    // ------------------------------------------------------------------ a run that repeats

    [Fact]
    public void SeededDiceRepeatAndUnseededDiceAreChance()
    {
        try
        {
            int[] Rolls()
            {
                Dice.Seed(77);
                var first = Dice.New();
                var second = Dice.New();
                return new[] { first.Next(1000), first.Next(1000), second.Next(1000), Dice.Shared.Next(1000), Dice.New().Next(1000) };
            }
            var a = Rolls();
            var b = Rolls();
            Assert.Equal(a, b);
            // Two generators made one after the other are not the same generator
            Dice.Seed(77);
            var one = Dice.New();
            var two = Dice.New();
            Assert.NotEqual(Enumerable.Range(0, 8).Select(_ => one.Next()).ToArray(), Enumerable.Range(0, 8).Select(_ => two.Next()).ToArray());
            // Another seed, other rolls
            Dice.Seed(78);
            Assert.NotEqual(a[0..3], new[] { Dice.New().Next(1000), Dice.New().Next(1000), Dice.New().Next(1000) });

            // A new Pokémon's genes come from the dice: the same seed makes the same Pokémon
            PokemonPlatinumEngine.Models.Pokemon Rolled()
            {
                Dice.Seed(5);
                return new PokemonPlatinumEngine.Models.Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get("Starly")!, 12);
            }
            var p = Rolled();
            var q = Rolled();
            Assert.Equal((p.Gender, p.Nature, p.MaxHP, p.Personality), (q.Gender, q.Nature, q.MaxHP, q.Personality));
        }
        finally
        {
            Dice.Seed(null);
        }
        Assert.Same(Random.Shared, Dice.Shared);
        Assert.NotSame(Dice.New(), Dice.New());
    }

    [Fact]
    public void TheFrameClockIsWhateverAToolSetsIt()
    {
        try
        {
            FrameClock.Fixed = 12.5;
            Assert.Equal(12.5, FrameClock.Now);
            FrameClock.Fixed = 12.5 + 1 / 60.0;
            Assert.Equal(12.5 + 1 / 60.0, FrameClock.Now);
        }
        finally
        {
            FrameClock.Fixed = null;
        }
        Assert.Null(FrameClock.Fixed);
    }

    [Fact]
    public void APersonsSeedIsTheSameEveryRun()
    {
        // (A string's own hash code changes from run to run, which made two runs draw people in different poses)
        static float Fnv(string name)
        {
            uint hash = 2166136261;
            foreach (char c in name) hash = (hash ^ c) * 16777619;
            return (hash & 0xFFFF) / 65536f;
        }
        foreach (string name in new[] { "Barry", "Mom", "Prof. Rowan", "{assistant}", "" })
        {
            float seed = WorldRenderer.SeedOf(name);
            Assert.Equal(Fnv(name), seed);
            Assert.InRange(seed, 0f, 0.99999f);
        }
        Assert.NotEqual(WorldRenderer.SeedOf("Barry"), WorldRenderer.SeedOf("Mom"));
        // Known values, so that a change of the hash shows
        Assert.Equal(0x292C / 65536f, WorldRenderer.SeedOf("a"));
    }

    // ------------------------------------------------------------------ the ground's masks and noise

    private const int T = GroundBaker.ArtTile;

    /// <summary>The mask as it was first written: a line at a time, each texel through a clamped index.</summary>
    private static float[] SlowMask(int tw, int th, Func<int, int, bool> on, int blur)
    {
        int w = tw * T, h = th * T;
        var m = new float[w * h];
        for (int ty = 0; ty < th; ty++)
            for (int tx = 0; tx < tw; tx++)
            {
                if (!on(tx, ty)) continue;
                for (int y = 0; y < T; y++)
                    Array.Fill(m, 1f, (ty * T + y) * w + tx * T, T);
            }
        var tmp = new float[w * h];
        void Blur(float[] src, float[] dst, bool horizontal)
        {
            int lines = horizontal ? h : w, len = horizontal ? w : h;
            float inv = 1f / (blur * 2 + 1);
            for (int l = 0; l < lines; l++)
            {
                int Idx(int k) => horizontal ? l * w + Math.Clamp(k, 0, len - 1) : Math.Clamp(k, 0, len - 1) * w + l;
                float sum = 0f;
                for (int k = -blur; k <= blur; k++) sum += src[Idx(k)];
                for (int k = 0; k < len; k++)
                {
                    dst[Idx(k)] = sum * inv;
                    sum += src[Idx(k + blur + 1)] - src[Idx(k - blur)];
                }
            }
        }
        for (int pass = 0; pass < 2; pass++)
        {
            Blur(m, tmp, horizontal: true);
            Blur(tmp, m, horizontal: false);
        }
        return m;
    }

    [Theory]
    [InlineData(5, 4, 3)]
    [InlineData(7, 6, 5)]
    [InlineData(4, 9, 10)]
    public void AMaskIsBlurredToTheSameValuesTheFastWayAsTheSlow(int tw, int th, int blur)
    {
        // A scatter of tiles, some at the edges, some alone, some in blocks
        bool On(int x, int y) => (x * 7 + y * 13) % 5 < 2 || (x == 0 && y % 2 == 0) || (x == tw - 1 && y == th - 1);
        var fast = PixelGround.Mask(tw, th, On, blur)!;
        var slow = SlowMask(tw, th, On, blur);
        Assert.Equal(slow.Length, fast.Length);
        for (int i = 0; i < slow.Length; i++)
            if (slow[i] != fast[i]) Assert.Fail($"texel {i % (tw * T)},{i / (tw * T)}: {fast[i]:R} for {slow[i]:R}");

        // The same again on the thread's kept buffer, after a larger mask and a smaller one have used it
        PixelGround.Mask(tw + 2, th + 3, On, blur);
        PixelGround.Mask(2, 2, (_, _) => true, blur);
        var again = PixelGround.Mask(tw, th, On, blur)!;
        Assert.Equal(slow, again);
    }

    [Fact]
    public void AMaskOfNothingIsNoMask()
    {
        Assert.Null(PixelGround.Mask(6, 6, (_, _) => false, 5));
        var one = PixelGround.Mask(6, 6, (x, y) => x == 3 && y == 3, 5);
        Assert.NotNull(one);
        Assert.True(one![(3 * T + T / 2) * 6 * T + 3 * T + T / 2] > 0.9f);
        Assert.Equal(0f, one[0]);
    }

    [Fact]
    public void TheNoiseThatKeepsItsCellIsTheNoise()
    {
        foreach (int salt in new[] { 11, 3, 40 })
        {
            // Along rows as the ground's texels are visited, across many cells, on both sides of zero
            var run = new PixelGround.FbmRun(salt);
            for (int y = -40; y < 300; y += 7)
                for (int x = -70; x < 420; x++)
                {
                    float gx = x / 32f - 3f, gy = y / 32f + 880f;
                    float expected = SoftCanvas.Fbm(gx / 3.5f, gy / 3.5f, salt);
                    float got = run.At(gx / 3.5f, gy / 3.5f);
                    if (expected != got) Assert.Fail($"at {gx},{gy} with salt {salt}: {got:R} for {expected:R}");
                }
            // And jumping about, which leaves the cell every time
            var jumpy = new PixelGround.FbmRun(salt);
            for (int i = 0; i < 200; i++)
            {
                float x = (i * 37 % 101) * 1.37f - 50f, y = (i * 53 % 89) * 2.11f - 60f;
                Assert.Equal(SoftCanvas.Fbm(x, y, salt), jumpy.At(x, y));
            }
        }
    }

    [Fact]
    public void AChunkWithoutWaterIsBakedWithoutAWaterMask()
    {
        // A lawn with a path: no water anywhere near, so nothing is painted for it and no mask comes back
        var map = new PokemonPlatinumEngine.Overworld.Map(40, 40) { Name = "Lawn", DisplayName = "Lawn" };
        for (int x = 0; x < 40; x++) map.SetGroundTile(x, 20, PokemonPlatinumEngine.Overworld.TileType.Path);
        var ground = PixelGround.Bake(map, new TileWindow(4, 4, 32, 32), PixelGround.ChunkPad, worldSeeds: true, out var mask);
        Assert.Null(mask);
        Assert.Equal((32 * T, 32 * T), (ground.Width, ground.Height));

        // The same window baked with nothing round it cut off has the same middle: the border only adds what is cut
        var whole = PixelGround.Bake(map, new TileWindow(2, 2, 36, 36), pad: 0, worldSeeds: true, out _);
        for (int y = 0; y < 32 * T; y += 3)
            for (int x = 0; x < 32 * T; x += 3)
            {
                var a = ground.Get(x, y);
                var b = whole.Get(x + 2 * T, y + 2 * T);
                if (a.R != b.R || a.G != b.G || a.B != b.B) Assert.Fail($"texel {x},{y} differs");
            }
    }
}
