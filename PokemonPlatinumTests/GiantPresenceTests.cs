using System;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The giant's arrival (plan 25 · YD1; style guide, "The giant's arrival" and "Camera (G8)"): the giant shot frames the
/// whole body as it grows and looks up at it, the growth holds at its burst and still ends on time, the call draws the
/// Pokémon back and brings it out again, only the first Dynamax of a single battle is called, the sky's wash comes and
/// goes with a giant and stays under Y16's caps, the cloud banks keep their cap, the cry has its mode, and nothing of
/// it plays with the scene off but what lasts.
/// </summary>
public class GiantPresenceTests
{
    private const float Frame = 1f / 60f;
    private const float Step = 0.05f;

    private static Pokemon Mon(string species) => new(PokemonDatabase.Get(species)!, 50);

    private static BattleAnimator Standing(BattleSide side, string species = "Snorlax")
    {
        var anim = new BattleAnimator();
        anim.Appear(BattleSide.Player, Mon("Blissey"));
        anim.Appear(BattleSide.Enemy, Mon("Blissey"));
        anim.Appear(side, Mon(species));
        return anim;
    }

    private static Vector3 Feet(BattleSide side) =>
        (side == BattleSide.Player ? BattleStage.PlayerSpot : BattleStage.EnemySpot) + new Vector3(0, BattleStage.PlatformHeight, 0);

    /// <summary>Where a point falls on the camera's picture: across and up as a share of the half-picture (1 is the edge), and how far in front.</summary>
    private static (float X, float Y, float Depth) Project(CameraShot shot, Vector3 p)
    {
        var forward = Vector3.Normalize(shot.Target - shot.Position);
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        var up = Vector3.Cross(right, forward);
        var d = p - shot.Position;
        float depth = Vector3.Dot(d, forward);
        float half = MathF.Tan(shot.Fov * MathF.PI / 360f);
        return (Vector3.Dot(d, right) / depth / (half * 16f / 9f), Vector3.Dot(d, up) / depth / half, depth);
    }

    // ------------------------------------------------------------------ the giant shot

    [Theory]
    [InlineData(BattleSide.Enemy, 0.6f), InlineData(BattleSide.Enemy, 2.2f), InlineData(BattleSide.Enemy, 3.6f), InlineData(BattleSide.Enemy, 4.6f)]
    [InlineData(BattleSide.Player, 0.5f), InlineData(BattleSide.Player, 1.2f), InlineData(BattleSide.Player, 2.2f), InlineData(BattleSide.Player, 3.2f)]
    public void TheGiantShotFramesTheWholeBodyAsItGrowsAndLooksUpAtIt(BattleSide side, float own)
    {
        // Heights that span the body plans' (a foe's model is sized to its platform: from a small serpent's 0.6 to a
        // tall biped's 4.6; past about 5 the shot in front of the player's platform runs out of lens at 40° and the
        // feet go under the message box)
        var anim = Standing(side);
        var cue = anim.Dynamax(side, 0, gigantamax: false)!;
        var v = anim[side, 0];
        var places = new FxPlaces();
        bool player = side == BattleSide.Player;
        for (float age = 0f; age < cue.Length - BattleCamera.GiantLetGo; age += Step)
        {
            float h = own * BattleAnimator.GiantOf(v, player);
            places.Set(side, 0, Feet(side), h);
            var shot = BattleCamera.GiantShot(anim, places, cue);
            Assert.True(float.IsFinite(shot.Position.X + shot.Position.Y + shot.Position.Z + shot.Fov), $"not finite at {age:0.00}");
            Assert.InRange(shot.Fov, BattleCamera.GiantNarrowest - 0.01f, BattleCamera.WidestFov + 0.01f);
            foreach (var p in new[] { Feet(side), Feet(side) + Vector3.UnitY * h })
            {
                var (x, y, depth) = Project(shot, p);
                Assert.True(depth > 0.5f, $"behind the lens at {age:0.00}");
                // The head under the picture's top, the feet over the message box (the picture's lowest fifth)
                Assert.InRange(y, -0.6f, 1f);
                Assert.InRange(x, -1f, 1f);
            }
            // Low, looking up the body
            Assert.True(shot.Position.Y < shot.Target.Y, $"not looking up at {age:0.00}");
            Assert.True(shot.Position.Y > Feet(side).Y, $"under the platform at {age:0.00}");
            if (!player) Assert.True(shot.Position.Z <= BattleCamera.FoeShotLimit + 0.01f, "a foe's shot stands in front of the player's platform");
            anim.Update(Step, (_, _) => null);
        }
    }

    [Fact]
    public void TheGiantShotTakesTheCameraThroughTheGrowthAndLetsGoBeforeItsEnd()
    {
        var anim = Standing(BattleSide.Enemy);
        var places = new FxPlaces();
        anim.Update(2f, (_, _) => null);
        var cue = anim.Dynamax(BattleSide.Enemy, 0, gigantamax: false)!;
        Assert.Equal(ShotKind.Giant, BattleCamera.Direct(anim, places).Kind);
        anim.Update(cue.Length - BattleCamera.GiantLetGo - 0.05f, (_, _) => null);
        Assert.Equal(ShotKind.Giant, BattleCamera.Direct(anim, places).Kind);
        anim.Update(0.1f, (_, _) => null);
        Assert.Equal(ShotKind.Overview, BattleCamera.Direct(anim, places).Kind);
        // While the player chooses, the overview as always
        anim.Undynamax(BattleSide.Enemy, 0);
        Assert.Equal(ShotKind.Overview, BattleCamera.Direct(anim, places, choosing: true).Kind);
        Assert.Equal(ShotKind.Giant, BattleCamera.Direct(anim, places).Kind);
    }

    // ------------------------------------------------------------------ the hold at the burst

    [Fact]
    public void TheGrowthHoldsAtItsBurstAndStillEndsOnTime()
    {
        foreach (float lead in new[] { 0f, BattleAnimator.DynamaxCallTime })
        {
            var cue = new EffectCue { Kind = CueKind.Dynamax, Lead = lead };
            float burst = lead + BattleAnimator.DynamaxGrown;
            Assert.Equal(burst, Directions.ImpactOf(cue));
            float last = -1f;
            for (float age = 0f; age <= cue.Length; age += 0.005f)
            {
                cue.Age = age;
                float shown = cue.ShownAge;
                Assert.True(shown >= last - 1e-5f, $"backward at {age}");
                last = shown;
                if (age <= burst) Assert.Equal(age, shown);
                else if (age < burst + Directions.GiantStop) Assert.Equal(burst, shown);
            }
            cue.Age = cue.Length;
            Assert.Equal(cue.Length, cue.ShownAge, 4);
        }
        // An end, a move and a Max Move hold nothing here
        var end = new EffectCue { Kind = CueKind.DynamaxEnd, Age = 0.6f };
        Assert.Equal(0.6f, end.ShownAge);
    }

    [Fact]
    public void EveryGrowthIsUnderTheCapsAtEveryAgeAndPure()
    {
        foreach (bool gmax in new[] { false, true })
            foreach (float lead in new[] { 0f, BattleAnimator.DynamaxCallTime })
                foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
                {
                    var places = new FxPlaces();
                    var a = new BattleAnimator().Cue(new EffectCue { Kind = CueKind.Dynamax, Gigantamax = gmax, Lead = lead, FromSide = side, ToSide = side, Move = "Transform" });
                    var b = new BattleAnimator().Cue(new EffectCue { Kind = CueKind.Dynamax, Gigantamax = gmax, Lead = lead, FromSide = side, ToSide = side, Move = "Transform" });
                    b.Seed = a.Seed;
                    float flashing = 0f, most = 0f;
                    for (float age = 0f; age < a.Length; age += 0.01f)
                    {
                        a.Age = b.Age = age;
                        var fx = new FxList();
                        MoveFx.Play(a, places, fx);
                        var again = new FxList();
                        MoveFx.Play(b, places, again);
                        Assert.True(fx.Count <= MoveFx.MaxQuads, $"{fx.Count} quads at {age:0.00}");
                        Assert.InRange(fx.Flash, 0f, 0.35f);
                        Assert.Equal(0f, fx.Shake);
                        Assert.Equal(fx.Count, again.Count);
                        Assert.Equal(fx.Flash, again.Flash);
                        if (fx.Flash > 0f) flashing += 0.01f;
                        most = Math.Max(most, fx.Count);
                        // Nothing of the growth before the call is over
                        if (age < lead - 0.01f) Assert.Equal(0, fx.Count);
                    }
                    Assert.True(flashing <= 1f / 3f + 0.02f, $"a flash of {flashing:0.00} s");
                    Assert.True(most > 0f);
                    // It lands at the burst, after the call
                    a.Age = lead + BattleAnimator.DynamaxGrown;
                    var landed = new FxList();
                    MoveFx.Play(a, places, landed);
                    Assert.Equal(lead + BattleAnimator.DynamaxGrown, landed.Landed, 3);
                }
    }

    [Fact]
    public void TheShockwaveRunsOutAcrossTheGroundAfterTheBurst()
    {
        var places = new FxPlaces();
        var cue = new BattleAnimator().Cue(new EffectCue { Kind = CueKind.Dynamax, FromSide = BattleSide.Enemy, ToSide = BattleSide.Enemy, Move = "Transform" });
        float h = places.Height(BattleSide.Enemy, 0);
        float Widest(float age)
        {
            cue.Age = age;
            var fx = new FxList();
            MoveFx.Play(cue, places, fx);
            return fx.Quads.Where(q => q.Normal == Vector3.UnitY).Select(q => q.Size).DefaultIfEmpty(0f).Max();
        }
        Assert.True(Widest(BattleAnimator.DynamaxGrown + Directions.GiantStop + 0.5f) > h * 2f, "the ring has run out across the stage");
        Assert.True(Widest(BattleAnimator.DynamaxGrown + Directions.GiantStop + 0.5f) <= h * MoveFx.GiantWaveReach + 0.01f);
    }

    // ------------------------------------------------------------------ the trainer's call

    [Fact]
    public void OnlyTheFirstDynamaxOfASingleBattleIsCalledAndNeverAtTheFrontier()
    {
        var anim = Standing(BattleSide.Enemy);
        Assert.Null(anim.CallerFor(BattleSide.Enemy, 0, frontier: false)); // a wild Pokémon: no trainer to call it
        Assert.NotNull(anim.CallerFor(BattleSide.Player, 0, frontier: false));
        Assert.Null(anim.CallerFor(BattleSide.Player, 0, frontier: true));
        anim.SubstituteUp(BattleSide.Player);
        Assert.Null(anim.CallerFor(BattleSide.Player, 0, frontier: false));
        var fresh = Standing(BattleSide.Player);
        fresh.Dynamax(BattleSide.Player, 0, gigantamax: false, caller: fresh.CallerFor(BattleSide.Player, 0, false));
        Assert.Null(fresh.CallerFor(BattleSide.Enemy, 0, frontier: false));
        Assert.Null(fresh.CallerFor(BattleSide.Player, 0, frontier: false));
        // In a double, the growth alone
        var two = new BattleAnimator();
        two.SetSlots(2, 2);
        two.Appear(BattleSide.Player, Mon("Snorlax"));
        Assert.Null(two.CallerFor(BattleSide.Player, 0, frontier: false));
    }

    [Fact]
    public void TheCallDrawsThePokemonBackAndTheGrowthComesAfterIt()
    {
        var anim = Standing(BattleSide.Player);
        var v = anim.Player;
        string? caller = anim.CallerFor(BattleSide.Player, 0, false);
        var cue = anim.Dynamax(BattleSide.Player, 0, gigantamax: false, caller: caller)!;
        Assert.Equal(BattleAnimator.DynamaxCallTime, cue.Lead);
        Assert.Equal(BattleAnimator.DynamaxCallTime + BattleAnimator.DynamaxTime, cue.Length);
        Assert.NotNull(anim.Call);
        Assert.Equal((1f, 0f), BattleAnimator.CalledBack(v));
        anim.Update(0.5f, (_, _) => null);
        Assert.Equal(0f, BattleAnimator.CalledBack(v).Grow);
        Assert.Equal(1f, BattleAnimator.GiantOf(v, true));
        anim.Update(0.6f, (_, _) => null);
        // Out of the ball again, and growing
        var (grow, _) = BattleAnimator.CalledBack(v);
        Assert.InRange(grow, 0.12f, 1.2f);
        anim.Update(BattleAnimator.CallEmerge + 0.7f, (_, _) => null);
        Assert.Equal(1f, BattleAnimator.CalledBack(v).Grow);
        Assert.Equal(BattleAnimator.DynamaxScaleOwn, BattleAnimator.GiantOf(v, true));
        // The trainer has walked off
        Assert.Null(anim.Call);
        anim.Update(1f, (_, _) => null);
        Assert.Equal(-1f, v.GiantAge);
        Assert.Equal(0f, v.GiantLead);
    }

    [Fact]
    public void NothingIsPlayedWithTheSceneOffButWhatLasts()
    {
        var anim = Standing(BattleSide.Player);
        var cue = anim.Dynamax(BattleSide.Player, 0, gigantamax: false, played: false, caller: "PLAYER");
        Assert.Null(cue);
        Assert.Null(anim.Call);
        Assert.Empty(anim.Cues);
        Assert.Equal(0f, anim.Player.GiantLead);
        Assert.Equal(BattleAnimator.DynamaxScaleOwn, BattleAnimator.GiantOf(anim.Player, true));
        // What lasts: the giant's sky stands at its full
        Assert.Equal(1f, GiantSky.Presence(anim));
        Assert.True(StageLights.Of(anim).Any);
    }

    // ------------------------------------------------------------------ the sky

    [Fact]
    public void TheSkyIsWashedOnlyWhileAGiantStandsComingAndGoingWithIt()
    {
        var anim = Standing(BattleSide.Enemy);
        Assert.Equal(0f, GiantSky.Presence(anim));
        Assert.False(StageLights.Of(anim).Any);
        var none = new FxList();
        GiantSky.Clouds(0f, 3f, true, none);
        Assert.Equal(0, none.Count);

        anim.Dynamax(BattleSide.Enemy, 0, gigantamax: false);
        float last = -1f;
        for (float age = 0f; age < BattleAnimator.DynamaxTime; age += Step)
        {
            float p = GiantSky.Presence(anim);
            Assert.True(p >= last - 1e-5f, "it never goes back while the giant grows");
            last = p;
            anim.Update(Step, (_, _) => null);
        }
        Assert.Equal(1f, GiantSky.Presence(anim));
        anim.Undynamax(BattleSide.Enemy, 0);
        anim.Update(BattleAnimator.DynamaxEndTime + Frame, (_, _) => null);
        Assert.Equal(0f, GiantSky.Presence(anim));
    }

    [Theory]
    [InlineData(6f), InlineData(12f), InlineData(17.5f), InlineData(20f), InlineData(2f)]
    public void TheSkysWashKeepsTheStageReadAtEveryTimeOfDay(float hour)
    {
        var light = GiantSky.Light(1f);
        Assert.True(light.Amount <= StageLights.MostDark);
        foreach (var kind in new[] { BattleArena.Grass, BattleArena.Cave, BattleArena.Indoors, BattleArena.Water })
        {
            var sky = ArtLook.ArenaRig(new ArenaSpec(kind), hour).Sky;
            foreach (var c in new[] { sky.Zenith, sky.Middle, sky.Horizon })
            {
                float before = 0.299f * c.R + 0.587f * c.G + 0.114f * c.B;
                var after = light.Apply(c);
                float now = 0.299f * after.R + 0.587f * after.G + 0.114f * after.B;
                // Never greyed to nothing: a dark stage is lifted toward the red-violet, a bright one dimmed by at most a third
                Assert.True(now >= Math.Min(before, 255f * 0.12f) * 0.66f, $"{kind} at {hour}: {before:0} to {now:0}");
            }
        }
    }

    [Fact]
    public void TheCloudBanksKeepTheirCapAndStayOutOfRooms()
    {
        for (float t = 0f; t < 60f; t += 0.7f)
        {
            var fx = new FxList();
            GiantSky.Clouds(1f, t, true, fx);
            Assert.InRange(fx.Count, 1, GiantSky.CloudCap);
            Assert.All(fx.Quads, q => Assert.True(q.Center.Z < -30f, "low on the sky, north of the stage"));
        }
        var inside = new FxList();
        GiantSky.Clouds(1f, 3f, false, inside);
        Assert.Equal(0, inside.Count);
    }

    // ------------------------------------------------------------------ the cry

    [Fact]
    public void TheGiantsCryIsAModeOfItsOwnInTheJukebox()
    {
        Assert.Contains(JukeboxScreen.Cries(), e => e.Id == nameof(CryMode.Giant));
        Assert.NotEqual("jukebox.cry.Giant", Text.Of("jukebox.cry.Giant"));
        Assert.NotEqual("jukebox.cryNote.Giant", Text.Of("jukebox.cryNote.Giant"));
        var cry = Cries.Get(PokemonDatabase.Get("Snorlax")!, null, CryMode.Normal).Samples;
        var giant = Cries.Get(PokemonDatabase.Get("Snorlax")!, null, CryMode.Giant).Samples;
        Assert.True(giant.Length > cry.Length * 1.5f, "lower and longer, in a hall");
        Assert.All(giant, s => Assert.True(float.IsFinite(s) && MathF.Abs(s) <= 1f));
    }
}

/// <summary>The giant's arrival with the motion number turned down (plan 12 · Q9's REDUCE MOTION): a static, so these run alone.</summary>
[Collection("Shared clocks and dice")]
public class GiantMotionTests
{
    [Fact]
    public void WithNoMotionAGrowthHoldsNothingAndTheCameraStaysOnTheOverview()
    {
        float was = BattleCamera.Motion;
        try
        {
            BattleCamera.Motion = 0f;
            var anim = new BattleAnimator();
            anim.Appear(BattleSide.Enemy, new Pokemon(PokemonDatabase.Get("Snorlax")!, 50));
            anim.Update(2f, (_, _) => null);
            var cue = anim.Dynamax(BattleSide.Enemy, 0, gigantamax: false)!;
            for (float age = 0f; age < cue.Length; age += 0.05f)
            {
                cue.Age = age;
                Assert.Equal(age, cue.ShownAge);
                Assert.Equal(ShotKind.Overview, BattleCamera.Direct(anim, new FxPlaces()).Kind);
            }
        }
        finally { BattleCamera.Motion = was; }
    }
}
