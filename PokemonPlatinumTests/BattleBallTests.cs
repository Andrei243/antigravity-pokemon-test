using System;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Raylib_cs;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Each Pokémon in its own ball (plan 25 · Y4): the ball a Pokémon comes out of and goes back into is the one it was
/// caught in, every kind opens in a release of its own, and a recall's ball has left the field by its end.
/// </summary>
public class BattleBallTests
{
    private static readonly string[] Kinds =
    {
        "Poké Ball", "Great Ball", "Ultra Ball", "Master Ball", "Premier Ball", "Safari Ball", "Net Ball", "Dive Ball",
        "Nest Ball", "Repeat Ball", "Timer Ball", "Luxury Ball", "Dusk Ball", "Heal Ball", "Quick Ball", "Cherish Ball"
    };

    private static readonly Vector3 At = new(1f, 1.2f, -2f);
    private const float H = 1.6f;

    private static string Written(FxList fx) => string.Join("\n",
        fx.Quads.Select(q => $"{q.Shape} {q.Blend} {q.Depth} {q.Center} {q.Size} {q.Rotation} {q.Color.R},{q.Color.G},{q.Color.B},{q.Color.A}")
        .Concat(fx.Ribbons.Select(r => $"ribbon {r.Width} {string.Join(";", r.Points)}")));

    private static FxList Released(string ball, float t, int seed = 3)
    {
        var fx = new FxList();
        BattleBall.ReleaseOf(BattleBall.LookOf(ball)).Emit(fx, At, H, t, seed);
        return fx;
    }

    // ------------------------------------------------------------------ the ball chosen

    [Fact]
    public void ThePokemonsOwnBallIsTheOneChosen()
    {
        var luxury = Mon("Bidoof", 5, "Tackle");
        luxury.Ball = "Luxury Ball";
        Assert.Equal("Luxury Ball", BattleBall.NameFor(luxury));

        var anim = new BattleAnimator();
        anim.SendOut(BattleSide.Player, luxury);
        Assert.Equal("Luxury Ball", anim[BattleSide.Player, 0].Ball);
    }

    [Fact]
    public void APokemonNeverCaughtOrATrainersComesInAPokeBall()
    {
        var wild = Mon("Bidoof", 5, "Tackle");
        Assert.Null(wild.Ball);
        Assert.Equal("Poké Ball", BattleBall.NameFor(wild));
        Assert.Equal("Poké Ball", BattleBall.NameFor(null));

        var anim = new BattleAnimator();
        anim.SendOut(BattleSide.Enemy, wild);
        Assert.Equal("Poké Ball", anim[BattleSide.Enemy, 0].Ball);

        // A ball with no kind of its own is drawn as a Poké Ball
        Assert.Equal(BallKind.Poke, BattleBall.LookOf("Park Ball").Kind);
    }

    [Fact]
    public void ARecallGoesBackIntoTheSameBall()
    {
        var dive = Mon("Bidoof", 5, "Tackle");
        dive.Ball = "Dive Ball";
        var anim = new BattleAnimator();
        anim.Appear(BattleSide.Player, dive);
        Assert.Equal("Poké Ball", anim[BattleSide.Player, 0].Ball);
        anim.Recall(BattleSide.Player);
        Assert.Equal("Dive Ball", anim[BattleSide.Player, 0].Ball);
    }

    [Fact]
    public void APokemonThatBreaksOutOfAThrownBallKeepsThatBall()
    {
        var foe = Mon("Bidoof", 5, "Tackle");
        var anim = new BattleAnimator();
        anim.Appear(BattleSide.Enemy, foe);
        anim.ThrowBall("Ultra Ball", 0, 1);
        anim.BreakFree();
        Assert.Equal("Ultra Ball", anim[BattleSide.Enemy, 0].Ball);
    }

    // ------------------------------------------------------------------ the kinds

    [Fact]
    public void TheSixteenKindsAreDistinctLooks()
    {
        var looks = Kinds.Select(BattleBall.LookOf).ToArray();
        Assert.Equal(16, looks.Select(l => l.Kind).Distinct().Count());
        Assert.Equal(16, looks.Distinct().Count());
        for (int i = 0; i < Kinds.Length; i++) Assert.Equal((BallKind)i, looks[i].Kind);
    }

    [Fact]
    public void TheCherishBallHasALookOfItsOwn()
    {
        Assert.NotEqual(BattleBall.LookOf("Poké Ball"), BattleBall.LookOf("Cherish Ball"));
        Assert.Equal(BallKind.Cherish, BattleBall.LookOf("Cherish Ball").Kind);
    }

    [Fact]
    public void EveryKindsReleaseIsDistinct()
    {
        foreach (float t in new[] { 0.12f, 0.3f })
        {
            var written = Kinds.Select(k => Written(Released(k, t))).ToArray();
            Assert.Equal(Kinds.Length, written.Distinct().Count());
        }
        var releases = Kinds.Select(k => BattleBall.ReleaseOf(BattleBall.LookOf(k))).ToArray();
        Assert.Equal(Kinds.Length, releases.Select(r => (r.Flash, r.Light)).Distinct().Count());
    }

    [Fact]
    public void EveryReleaseIsPureAndEndsWhenTheFlashDoes()
    {
        foreach (var k in Kinds)
            foreach (float t in new[] { 0f, 0.1f, 0.25f, 0.4f, 0.55f, 0.6f })
            {
                Assert.Equal(Written(Released(k, t)), Written(Released(k, t)));
                var fx = Released(k, t);
                Assert.InRange(fx.Quads.Count + fx.Ribbons.Count, t < 0.55f ? 1 : 0, BallRelease.Cap);
            }
        foreach (var k in Kinds)
        {
            Assert.Empty(Released(k, -0.01f).Quads);
            Assert.Empty(Released(k, BallRelease.Length + 0.01f).Quads);
        }
    }

    [Fact]
    public void ThePokeBallsReleaseIsWhatItAlwaysWas()
    {
        foreach (float t in new[] { 0f, 0.1f, 0.3f, 0.5f })
        {
            var old = new FxList();
            var color = new Color(255, 255, 255, 255);
            var light = new Color(255, 246, 200, 255);
            const int seed = 3;
            old.Sprite(FxShape.Glow, At, H * (0.5f + 0.9f * Fx.EaseOut(t / 0.25f)), Fx.Fade(color, (1f - t / 0.6f) * 0.9f));
            Fx.Shockwave(old, t, At, Vector3.Zero, 0f, 0.4f, H * 1.1f, light);
            Fx.Burst(old, seed * 977 + 13, t, At, H, 0f, 0.55f, 14, FxShape.Spark, color, light, 0.1f, 0.9f);
            Assert.Equal(Written(old), Written(Released("Poké Ball", t, seed)));
        }
    }

    [Fact]
    public void AReleaseTakesItsColoursFromItsBall()
    {
        // The Luxury Ball's flash is gold, the Poké Ball's white
        var luxury = BattleBall.ReleaseOf(BattleBall.LookOf("Luxury Ball"));
        Assert.True(luxury.Flash.B < luxury.Flash.R - 50);
        var poke = BattleBall.ReleaseOf(BattleBall.LookOf("Poké Ball"));
        Assert.Equal((byte)255, poke.Flash.B);
    }

    // ------------------------------------------------------------------ the recall

    private static readonly Vector3 Far = new(-8f, 4f, 5f);
    private static readonly Vector3 Open = new(0f, 1.4f, 0f);

    private static RecallStep Step(float age) => BallRecall.At(age, BattleAnimator.RecallTime, Far, Open, H, 0.3f, 0.17f);

    [Fact]
    public void TheRecallsBallComesFromOffTheFieldAndHasLeftItByTheEnd()
    {
        var first = Step(0f);
        Assert.True(first.Visible);
        Assert.True(Vector3.Distance(first.At, Far) < 0.01f);

        var mid = Step(BattleAnimator.RecallTime * 0.5f);
        Assert.True(mid.Visible);
        Assert.True(Vector3.Distance(mid.At, Open) < 0.1f);
        Assert.True(mid.Open > 0.9f);
        Assert.True(mid.Beam > 0.9f);

        // Shut again before it flies out, and then gone with the line
        var late = Step(BattleAnimator.RecallTime * 0.95f);
        Assert.Equal(0f, late.Open);
        Assert.True(Vector3.Distance(late.At, Far) < Vector3.Distance(Open, Far) * 0.4f);
        Assert.False(Step(BattleAnimator.RecallTime).Visible);
        Assert.False(Step(-1f).Visible);
    }

    [Fact]
    public void TheRecallIsPureAndTheBeamIsOnlyWhileTheLidIsOpen()
    {
        for (float a = 0f; a < BattleAnimator.RecallTime; a += 0.03f)
        {
            Assert.Equal(Step(a), Step(a));
            var s = Step(a);
            if (s.Beam > 0f) Assert.True(s.Open > 0f);
        }
        var fx = new FxList();
        BallRecall.Beam(fx, 0f, 1f, Open, Open, H);
        Assert.Empty(fx.Quads);
        BallRecall.Beam(fx, 1f, 1f, Open, Open + Vector3.UnitX, H);
        Assert.Equal(3, fx.Ribbons.Count);
    }

    [Fact]
    public void ThePokemonIsDrawnInDuringTheBeamAndNotBefore()
    {
        Assert.Equal(-1f, BallRecall.Shrink(-1f, BattleAnimator.RecallTime));
        Assert.Equal(0f, BallRecall.Shrink(0.1f, BattleAnimator.RecallTime));
        float last = 0f;
        for (float a = 0f; a <= BattleAnimator.RecallTime; a += 0.02f)
        {
            float s = BallRecall.Shrink(a, BattleAnimator.RecallTime);
            Assert.True(s >= last - 1e-6f);
            last = s;
        }
        Assert.Equal(1f, BallRecall.Shrink(BattleAnimator.RecallTime, BattleAnimator.RecallTime), 3);
    }

    [Fact]
    public void ARecallEndsAndTheNewPokemonStartsWithNoBallInFlight()
    {
        var p = Mon("Bidoof", 5, "Tackle");
        p.Ball = "Luxury Ball";
        var anim = new BattleAnimator();
        anim.Appear(BattleSide.Player, p);
        anim.Recall(BattleSide.Player);
        anim.Update(BattleAnimator.RecallTime + 0.01f, p, null);
        Assert.True(anim[BattleSide.Player, 0].RecallAge < 0f);
        Assert.False(anim[BattleSide.Player, 0].Present);

        // Called back and sent straight out again: the recall is over with the new send-out
        var q = Mon("Starly", 5, "Tackle");
        anim.Appear(BattleSide.Player, p);
        anim.Recall(BattleSide.Player);
        anim.Update(0.2f, p, null);
        anim.SendOut(BattleSide.Player, q);
        Assert.True(anim[BattleSide.Player, 0].RecallAge < 0f);
    }
}
