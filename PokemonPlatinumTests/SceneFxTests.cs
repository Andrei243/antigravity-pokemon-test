using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The story's scene effects (plan 09 · L11; style guide, "Life", "Scene effects"): what each kind draws as a
/// function of its age and seed, the ripple's rows, the field keeping them, and the script's <c>effect</c>.
/// </summary>
public class SceneFxTests
{
    private const float Frame = 1f / 60f;

    /// <summary>The kinds that draw pieces in the world: all but the ripple, which wavers the picture itself.</summary>
    private static readonly SceneEffect[] Drawn = Enum.GetValues<SceneEffect>().Where(k => k != SceneEffect.Ripple).ToArray();

    private static List<SceneQuad> At(SceneEffect kind, float age, int seed = 7, Direction way = Direction.Right, float? ends = null)
    {
        var into = new List<SceneQuad>();
        float seconds = SceneEffects.Length(kind);
        SceneFx.Quads(kind, age, seconds, ends ?? seconds, seed, way, Vector3.Zero, into);
        return into;
    }

    private static void Run(FieldLife life, float seconds)
    {
        for (float t = 0f; t < seconds - 0.0001f; t += Frame) life.Advance(Frame);
    }

    private static List<SceneQuad> Quads(FieldLife life)
    {
        var into = new List<SceneQuad>();
        life.SceneQuads(into);
        return into;
    }

    private static bool OnTheGrid(float v) => MathF.Abs(v * 32f - MathF.Round(v * 32f)) < 1e-3f;

    [Fact]
    public void EachKindIsTheSameForTheSameAgeAndSeed()
    {
        foreach (var kind in Drawn)
            foreach (float t in new[] { 0f, 0.07f, SceneEffects.Height(kind), SceneEffects.Fading(kind) })
                Assert.Equal(At(kind, t), At(kind, t));
    }

    [Fact]
    public void TheSameEffectOnTwoTilesDiffers()
    {
        // Seeded by the tile, what is scattered lies otherwise; and wherever they stand, two tiles' effects are apart
        int one = SceneFx.SeedOf(3, 4), other = SceneFx.SeedOf(11, 9);
        Assert.NotEqual(one, other);
        foreach (var kind in new[] { SceneEffect.Burst, SceneEffect.Beam, SceneEffect.Sparkles })
            Assert.NotEqual(At(kind, SceneEffects.Height(kind), one), At(kind, SceneEffects.Height(kind), other));
        Assert.NotEqual(At(SceneEffect.Rift, 1f, one), At(SceneEffect.Rift, 1f, other));

        var map = new Map(16, 16) { Name = "Lab" };
        foreach (var kind in Drawn)
        {
            var life = new FieldLife();
            life.Effect(map, kind, 3, 4, SceneEffects.Length(kind), Direction.Right, hold: false);
            Run(life, SceneEffects.Height(kind));
            var first = Quads(life);
            var elsewhere = new FieldLife();
            elsewhere.Effect(map, kind, 11, 9, SceneEffects.Length(kind), Direction.Right, hold: false);
            Run(elsewhere, SceneEffects.Height(kind));
            Assert.NotEmpty(first);
            Assert.NotEqual(first, Quads(elsewhere));
        }
    }

    [Fact]
    public void AnEffectIsGoneAfterItsLength()
    {
        foreach (var kind in Drawn)
        {
            float length = SceneEffects.Length(kind);
            Assert.NotEmpty(At(kind, SceneEffects.Height(kind)));
            Assert.Empty(At(kind, length));
            Assert.Empty(At(kind, length + 0.5f));
            Assert.Empty(At(kind, -0.1f));
        }
        Assert.False(SceneFx.Ripple(SceneEffect.Ripple, SceneEffects.Length(SceneEffect.Ripple), SceneEffects.Length(SceneEffect.Ripple)).Plays);
        Assert.Equal(0f, SceneFx.Flash(SceneEffect.Burst, 0.6f, 0.6f));

        var life = new FieldLife();
        life.Effect(new Map(9, 9) { Name = "Lab" }, SceneEffect.Rift, 4, 4, 1f, Direction.Up, hold: false);
        Run(life, 0.5f);
        Assert.Equal(1, life.EffectCount);
        Run(life, 0.6f);
        Assert.Equal(0, life.EffectCount);
        Assert.Empty(Quads(life));
    }

    [Fact]
    public void AHeldEffectPlaysUntilItIsStopped()
    {
        var map = new Map(9, 9) { Name = "Lab" };
        foreach (var kind in Enum.GetValues<SceneEffect>())
        {
            var life = new FieldLife();
            life.Effect(map, kind, 4, 4, SceneEffects.Length(kind), Direction.Down, hold: true);
            // Long past its own length, every frame still shows it
            for (int i = 0; i < 600; i++)
            {
                life.Advance(Frame);
                if (kind == SceneEffect.Ripple) Assert.True(life.Ripple.Plays, $"a held ripple stopped at {life.Now}");
                else Assert.True(Quads(life).Count > 0, $"a held {kind} showed nothing at {life.Now:0.000}");
            }
            life.StopEffect(4, 4);
            Run(life, SceneEffects.Length(kind) + 0.1f);
            Assert.Equal(0, life.EffectCount);
            Assert.Empty(Quads(life));
            Assert.False(life.Ripple.Plays);
        }
    }

    [Fact]
    public void EveryPieceIsOnTheGridOfTexels()
    {
        foreach (var kind in Drawn)
            foreach (var way in new[] { Direction.Up, Direction.Right, Direction.Down, Direction.Left })
                for (float t = 0f; t < SceneEffects.Length(kind); t += 0.05f)
                    foreach (var q in At(kind, t, 11, way))
                    {
                        Assert.True(OnTheGrid(q.At.X) && OnTheGrid(q.At.Z), $"{kind} at {t}: {q.At}");
                        Assert.True(OnTheGrid(q.Lift) && OnTheGrid(q.Width) && OnTheGrid(q.Height), $"{kind} at {t}: {q}");
                        Assert.InRange(q.Cell, 0, SceneFx.Columns * SceneFx.Rows - 1);
                    }
    }

    [Fact]
    public void AShadowCrossesTheWayItIsSent()
    {
        float Middle(Direction way, float age, Func<Vector3, float> axis) => At(SceneEffect.Shadow, age, way: way).Average(q => axis(q.At));
        Assert.True(Middle(Direction.Right, 1.8f, p => p.X) > Middle(Direction.Right, 0.2f, p => p.X));
        Assert.True(Middle(Direction.Left, 1.8f, p => p.X) < Middle(Direction.Left, 0.2f, p => p.X));
        Assert.True(Middle(Direction.Down, 1.8f, p => p.Z) > Middle(Direction.Down, 0.2f, p => p.Z));
        Assert.True(Middle(Direction.Up, 1.8f, p => p.Z) < Middle(Direction.Up, 0.2f, p => p.Z));
        Assert.All(At(SceneEffect.Shadow, 1f, way: Direction.Down), q => Assert.True(q.Flat && q.Turn == 2));
    }

    [Fact]
    public void TheRippleMovesRowsByWholeTexelsAndNothingWhenNoneWavers()
    {
        Assert.False(default(SceneRipple).Plays);
        for (int row = 0; row < SceneFx.RippleRows; row++) Assert.Equal(0, SceneFx.RippleOffset(row, default));

        float length = SceneEffects.Length(SceneEffect.Ripple);
        for (float t = 0f; t < length; t += 0.05f)
        {
            var ripple = SceneFx.Ripple(SceneEffect.Ripple, t, length);
            Assert.InRange(ripple.Reach, 1, 3);
            for (int row = 0; row < SceneFx.RippleRows; row++)
                Assert.InRange(SceneFx.RippleOffset(row, ripple), -ripple.Reach, ripple.Reach);
        }
        // It reaches three texels at its height, and somewhere a row is pushed that far
        var height = SceneFx.Ripple(SceneEffect.Ripple, SceneEffects.Height(SceneEffect.Ripple), length);
        Assert.Equal(3, height.Reach);
        Assert.Contains(Enumerable.Range(0, SceneFx.RippleRows), row => Math.Abs(SceneFx.RippleOffset(row, height)) == 3);
        // Nothing else ripples
        Assert.False(SceneFx.Ripple(SceneEffect.Burst, 0.1f, 0.6f).Plays);

        var life = new FieldLife();
        Assert.False(life.Ripple.Plays);
        life.Effect(new Map(9, 9) { Name = "Lab" }, SceneEffect.Burst, 4, 4, 0.6f, Direction.Right, hold: false);
        Assert.False(life.Ripple.Plays);
        Assert.Equal(0.85f, life.Flash);
    }

    [Fact]
    public void ComingToAnotherPlaceClearsEveryEffect()
    {
        var map = new Map(9, 9) { Name = "Lab" };
        var life = new FieldLife();
        foreach (var kind in Enum.GetValues<SceneEffect>()) life.Effect(map, kind, (int)kind, 2, 1f, Direction.Right, hold: kind == SceneEffect.Beam);
        Assert.Equal(6, life.EffectCount);
        life.ClearEffects();
        Assert.Equal(0, life.EffectCount);
        Assert.Empty(Quads(life));
        Assert.Equal(0f, life.Flash);

        // And going through a door forgets them with the rest of life
        life.Effect(map, SceneEffect.Beam, 4, 4, 1f, Direction.Right, hold: true);
        life.Clear();
        Assert.Equal(0, life.EffectCount);
    }

    [Fact]
    public void TheScriptsWordsAreRead()
    {
        var code = ScriptParser.Parse("test", """
            script Scene
              effect burst 3 4
              effect shadow 3 4 1.5 left wait
              effect beam 5 6 hold
              effect rift 7 8 2.5
              effect stop 5 6
              end
            """)[0].Code;
        Assert.Equal(SceneEffect.Burst, code[0].Effect);
        Assert.Equal((3, 4), (code[0].X, code[0].Y));
        Assert.Equal(SceneEffects.Length(SceneEffect.Burst), code[0].Seconds);
        Assert.Null(code[0].Direction);
        Assert.Equal("", code[0].Name);
        Assert.Equal((SceneEffect.Shadow, 1.5f, Direction.Left, "wait"), (code[1].Effect, code[1].Seconds, code[1].Direction, code[1].Name));
        Assert.Equal((SceneEffect.Beam, "hold"), (code[2].Effect, code[2].Name));
        Assert.Equal(2.5f, code[3].Seconds);
        Assert.Equal(("stop", 5, 6), (code[4].Name, code[4].X, code[4].Y));
        Assert.All(code.Take(5), i => Assert.Equal(Op.Effect, i.Op));
    }

    [Theory]
    [InlineData("effect glow 3 4", "is not a scene effect")]
    [InlineData("effect burst 3", "a tile's y")]
    [InlineData("effect burst 3 4 left", "only a shadow is sent a way")]
    [InlineData("effect beam 3 4 2 hold", "is given no time")]
    [InlineData("effect rift 3 4 0", "longer than nothing")]
    [InlineData("effect sparkles 3 4 soon", "'wait' or 'hold'")]
    [InlineData("effect stop 3", "a tile's y")]
    public void AWrongEffectIsTold(string line, string message)
    {
        var wrong = Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script Scene\n  " + line + "\n  end\n"));
        Assert.Contains(message, wrong.Message);
    }

    [Fact]
    public void ASceneLightsEveryKind()
    {
        var library = ScriptLibrary.FromSources(("test", """
            script Scene
              effect ripple 10 10 wait
              effect rift 10 9
              effect burst 10 9 wait
              effect shadow 10 12 2 down
              effect beam 4 4 hold
              effect sparkles 4 6
              say "It is over."
              effect stop 4 4
              end
            """), ("common", ""));
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"), null);
        runner.RunToEnd();
        Assert.Equal(new[]
        {
            "effect Ripple 10 10", "effect Rift 10 9", "effect Burst 10 9", "effect Shadow 10 12 Down",
            "effect Beam 4 4 hold", "effect Sparkles 4 6", "effect stop 4 4"
        }, host.Log.Where(l => l.StartsWith("effect")));
        Assert.Contains(host.Transcript, t => t.Text == "It is over.");

        // A script that may only set things up shows nothing
        var quiet = new HeadlessScriptHost { ShowsNothing = true };
        var setUp = new ScriptRunner(library, quiet);
        setUp.Start(library.All.First(s => s.File == "test"), null);
        Assert.Throws<ScriptException>(() => setUp.RunToEnd());
    }

    [Fact]
    public void TheirSoundsAreInTheBank()
    {
        foreach (var kind in Enum.GetValues<SceneEffect>())
        {
            string name = SceneEffects.SoundOf(kind);
            Assert.Contains(name, SoundBank.Names);
            Assert.NotNull(SoundBank.Get(name));
        }
        Assert.Equal(6, Enum.GetValues<SceneEffect>().Select(SceneEffects.SoundOf).Distinct().Count());
    }

    [Fact]
    public void TheAtlasHoldsEveryCell()
    {
        var atlas = LifeArt.Scene();
        Assert.Equal((SceneFx.Cell * SceneFx.Columns, SceneFx.Cell * SceneFx.Rows), (atlas.Width, atlas.Height));
        bool Painted(int cell)
        {
            int ox = cell % SceneFx.Columns * SceneFx.Cell, oy = cell / SceneFx.Columns * SceneFx.Cell;
            for (int y = 0; y < SceneFx.Cell; y++)
                for (int x = 0; x < SceneFx.Cell; x++)
                    if (atlas.IsOpaque(ox + x, oy + y)) return true;
            return false;
        }
        // Every cell the burst, the sparks, the beam and the rift draw has something painted in it, and each of the
        // shadow's frames (whose wings drawn in leave a corner cell bare)
        for (int cell = 0; cell < SceneFx.Shadow; cell++) Assert.True(Painted(cell), $"cell {cell} of the scene atlas is empty");
        for (int frame = 0; frame < 2; frame++)
        {
            Assert.True(Painted(SceneFx.Shadow + frame * 6 + 1), $"the shadow's frame {frame} has no head");
            Assert.True(Painted(SceneFx.Shadow + frame * 6 + 4), $"the shadow's frame {frame} has no tail");
        }
    }
}
