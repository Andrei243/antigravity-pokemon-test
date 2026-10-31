using System;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q9, light and motion: the options' REDUCE FLASHES and REDUCE MOTION, which the whole process holds
/// (<see cref="Accessibility"/>, <see cref="BattleCamera.Motion"/>), so the class runs with the other shared state and
/// puts both back.
/// </summary>
[Collection("Shared clocks and dice")]
public class AccessibilityTests : IDisposable
{
    public void Dispose() => Accessibility.Use(false, false);

    private static readonly FxPlaces Places = new();

    [Fact]
    public void BothRowsAreOffUnlessChosenAndStandAtTheEndOfTheSettings()
    {
        var settings = new GameSettings();
        Assert.False(settings.ReduceFlashes);
        Assert.False(settings.ReduceMotion);
        // A settings file from before the rows reads as the game always ran
        var json = new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
        var old = JsonSerializer.Deserialize<GameSettings>("{ \"Quality\": \"Medium\" }", json)!;
        Assert.Equal((false, false), (old.ReduceFlashes, old.ReduceMotion));

        var rows = Enum.GetValues<OptionRow>();
        int flashes = Array.IndexOf(rows, OptionRow.ReduceFlashes), motion = Array.IndexOf(rows, OptionRow.ReduceMotion);
        Assert.Equal(Array.IndexOf(rows, OptionRow.Rumble) + 1, flashes);
        Assert.Equal(flashes + 1, motion);
        // (plan 13 · V9 put MEDAL NOTICES between Reduce motion and Controls, and V10 the RUN TIMER after it, and plan 12 · Q26 BOX TOOLS after that)
        Assert.Equal(motion + 4, Array.IndexOf(rows, OptionRow.Controls));
        Assert.True(motion < Array.IndexOf(rows, OptionRow.Help));
    }

    [Fact]
    public void EachRowGoesOnAndOffAndSaysWhatItDoes()
    {
        var s = new GameSettings();
        OptionsScreen.Change(s, OptionRow.ReduceFlashes, 1);
        OptionsScreen.Change(s, OptionRow.ReduceMotion, -1);
        Assert.Equal((true, true), (s.ReduceFlashes, s.ReduceMotion));
        foreach (var row in new[] { OptionRow.ReduceFlashes, OptionRow.ReduceMotion })
        {
            var (label, value, help) = OptionsScreen.Describe(s, row);
            Assert.DoesNotContain("options.", label + value + help);
            var (_, offValue, offHelp) = OptionsScreen.Describe(new GameSettings(), row);
            Assert.NotEqual(value, offValue);
            Assert.NotEqual(help, offHelp);
        }
        OptionsScreen.Change(s, OptionRow.ReduceFlashes, 1);
        OptionsScreen.Change(s, OptionRow.ReduceMotion, 1);
        Assert.Equal((false, false), (s.ReduceFlashes, s.ReduceMotion));

        // A reset takes both back
        s.ReduceFlashes = s.ReduceMotion = true;
        s.ResetToDefaults();
        Assert.Equal((false, false), (s.ReduceFlashes, s.ReduceMotion));
    }

    [Fact]
    public void TheBattleCamerasOneNumberFollowsTheMotionRow()
    {
        Assert.Equal(1f, BattleCamera.Motion);
        Accessibility.Use(false, true);
        Assert.Equal(0f, BattleCamera.Motion);
        Assert.Equal(0f, Accessibility.Motion);
        Accessibility.Use(false, false);
        Assert.Equal(1f, BattleCamera.Motion);
        Assert.Equal(1f, Accessibility.Motion);
    }

    [Fact]
    public void AMovesFlashIsNeverMoreThanAQuarter()
    {
        Accessibility.Use(true, false);
        var fx = new FxList();
        fx.ScreenFlash(Raylib_cs.Color.White, 0.5f);
        Assert.Equal(Accessibility.FlashCap, fx.Flash);
        fx.ScreenFlash(Raylib_cs.Color.White, 0.1f);
        Assert.Equal(Accessibility.FlashCap, fx.Flash);

        // Every move's look, critical and super effective, at every age
        float most = 0f;
        foreach (var move in MoveDatabase.GetAll().Take(500))
        {
            var cue = new BattleAnimator().Cue(new EffectCue
            {
                Move = move.Name, Type = move.Type, Category = move.Category, FromSide = BattleSide.Player, ToSide = BattleSide.Enemy,
                Critical = true, SuperEffective = true
            });
            for (float age = 0f; age < 1.4f; age += 0.02f)
            {
                cue.Age = age;
                var list = new FxList();
                MoveFx.Play(cue, Places, list);
                most = Math.Max(most, list.Flash);
            }
        }
        Assert.InRange(most, 0.01f, Accessibility.FlashCap);

        // With the row off the flashes are as they were
        Accessibility.Use(false, false);
        var plain = new FxList();
        plain.ScreenFlash(Raylib_cs.Color.White, 0.45f);
        Assert.Equal(0.45f, plain.Flash);
    }

    [Fact]
    public void TheFlashesIntoABattleAreOneSoftSwellUnderAQuarter()
    {
        Accessibility.Use(true, false);
        foreach (var kind in Enum.GetValues<TransitionKind>().Where(k => k != TransitionKind.Fade))
        {
            float last = 0f, most = 0f;
            bool falling = false;
            for (float t = 0f; t < 1.5f; t += 0.005f)
            {
                float white = SceneTransition.Flash(kind, t);
                Assert.InRange(white, 0f, Accessibility.FlashCap);
                // Up and then down once: never a strobe
                if (white < last - 1e-4f) falling = true;
                else if (falling) Assert.False(white > last + 1e-4f, $"{kind} flashes again at {t}");
                most = Math.Max(most, white);
                last = white;
            }
            Assert.True(most > 0.2f, $"{kind}'s swell is seen");
        }
        // A band's white steps are held under a quarter too
        foreach (var kind in new[] { TransitionKind.LeaderBand, TransitionKind.EliteBand })
            for (float t = 0f; t < SceneTransition.OutSeconds(kind); t += 0.01f)
                Assert.InRange(SceneTransition.BandAt(kind, t, 1920, 1080).Flash, 0f, Accessibility.FlashCap);
    }

    [Fact]
    public void AScenesBurstAndTheLightningAreHeldToAQuarter()
    {
        Accessibility.Use(true, false);
        var life = new FieldLife();
        life.Effect(new Map(9, 9) { Name = "Lab" }, SceneEffect.Burst, 4, 4, 0.6f, Direction.Right, hold: false);
        float most = 0f;
        for (int i = 0; i < 40; i++)
        {
            life.Advance(1f / 60f);
            most = Math.Max(most, life.Flash);
        }
        Assert.InRange(most, 0.01f, Accessibility.FlashCap);
        Assert.Equal(Accessibility.FlashCap, Accessibility.Flash(0.9f));
        Assert.Equal(0.1f, Accessibility.Flash(0.1f));
    }

    [Fact]
    public void WithMotionReducedTheFieldNeitherShakesNorRipples()
    {
        Assert.NotEqual(0f, WorldRenderer.ShakeAt(0.0, 0.19f, 1f));
        Accessibility.Use(false, true);
        for (double now = 0; now < 1; now += 1.0 / 60) Assert.Equal(0f, WorldRenderer.ShakeAt(now, 0.19f, 1f));

        var life = new FieldLife();
        life.Effect(new Map(9, 9) { Name = "Lab" }, SceneEffect.Ripple, 4, 4, 1f, Direction.Right, hold: false);
        for (int i = 0; i < 30; i++)
        {
            life.Advance(1f / 60f);
            Assert.False(life.Ripple.Plays);
        }
        Accessibility.Use(false, false);
        Assert.True(life.Ripple.Plays);
    }

    [Fact]
    public void WithMotionReducedTheBattleCameraIgnoresTheShakeAndTheCriticalPunchIn()
    {
        static BattleAnimator Critical()
        {
            var anim = new BattleAnimator();
            anim.Update(2f, null, null);
            anim.Cue(new EffectCue { Move = "Slash", Category = MoveCategory.Physical, FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, Critical = true });
            for (float t = 0f; t < BattleAnimator.ImpactTime + 0.05f; t += 1f / 60f) anim.Update(1f / 60f, null, null);
            return anim;
        }

        // As ever: the punch-in, and a shake that moves the lens
        var normal = Critical();
        Assert.Equal(ShotKind.Critical, BattleCamera.Direct(normal, Places).Kind);
        var still = new BattleCamera().Update(1f / 60f, normal, Places, 0f);
        var shaken = new BattleCamera().Update(1f / 60f, normal, Places, 2f);
        Assert.NotEqual(still.Position, shaken.Position);

        Accessibility.Use(false, true);
        var reduced = Critical();
        Assert.Equal(ShotKind.Target, BattleCamera.Direct(reduced, Places).Kind);
        var calm = new BattleCamera().Update(1f / 60f, reduced, Places, 0f);
        var calmShaken = new BattleCamera().Update(1f / 60f, reduced, Places, 2f);
        Assert.Equal(calm.Position, calmShaken.Position);
        Assert.Equal(calm.Target, calmShaken.Target);
    }

    [Fact]
    public void WithMotionReducedARevealIsAtItsEndAtOnce()
    {
        var slide = new UiReveal(0.3f, 0.2f);
        slide.Open();
        slide.Update(1f / 60f);
        Assert.InRange(slide.Shown, 0.01f, 0.99f);

        Accessibility.Use(false, true);
        var snapped = new UiReveal(0.3f, 0.2f);
        snapped.Open();
        snapped.Update(1f / 60f);
        Assert.Equal(1f, snapped.Shown);
        snapped.Close();
        snapped.Update(1f / 60f);
        Assert.False(snapped.Visible);
    }
}
