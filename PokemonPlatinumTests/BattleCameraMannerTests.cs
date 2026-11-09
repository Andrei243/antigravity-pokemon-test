using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 45 · BC1: the battle's manners as data, the options' BATTLE CAMERA row, and Classic held to the director as it
/// was, to the bit.
/// </summary>
public class BattleCameraMannerTests
{
    private static readonly FxPlaces Places = new();

    /// <summary>A manner with every switch off but a name of its own: it must film as Classic does.</summary>
    private static readonly CameraManner AllOff = new("all off");

    private static void Advance(BattleAnimator anim, float seconds)
    {
        for (float t = 0f; t < seconds - 1e-4f; t += 1f / 60f) anim.Update(1f / 60f, null, null);
    }

    /// <summary>
    /// A battle's moments, one after another, each a step of the clock with what the director is asked at it: the intro,
    /// the overview, a menu, both sides' send-outs, a move's attacker, cut and critical punch-in, a move on its user, the
    /// signature shots, a giant's growth, a ball thrown and caught, a faint.
    /// </summary>
    private static IEnumerable<(string Moment, BattleAnimator Anim, bool Choosing)> Moments()
    {
        var anim = new BattleAnimator();
        for (float t = 0f; t < 2f; t += 0.25f) { yield return ($"intro {t}", anim, false); Advance(anim, 0.25f); }
        for (int i = 0; i < 4; i++) { yield return ($"overview {i}", anim, false); yield return ($"menu {i}", anim, true); Advance(anim, 0.7f); }

        anim.SendOut(BattleSide.Enemy, new Pokemon(PokemonDatabase.Get("Shinx")!, 5));
        for (int i = 0; i < 12; i++) { yield return ($"foe out {i}", anim, false); Advance(anim, 0.1f); }
        anim.SendOut(BattleSide.Player, new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
        for (int i = 0; i < 12; i++) { yield return ($"own out {i}", anim, false); Advance(anim, 0.1f); }

        foreach (var (move, category, critical, self) in new (string, MoveCategory, bool, bool)[]
        {
            ("Tackle", MoveCategory.Physical, false, false), ("Slash", MoveCategory.Physical, true, false),
            ("Growl", MoveCategory.Status, false, false), ("Swords Dance", MoveCategory.Status, false, true),
            ("Hyper Beam", MoveCategory.Special, false, false), ("Roar of Time", MoveCategory.Special, false, false),
            ("Close Combat", MoveCategory.Physical, false, false), ("Hurricane", MoveCategory.Special, false, false),
        })
            foreach (var (from, to) in new[] { (BattleSide.Player, BattleSide.Enemy), (BattleSide.Enemy, BattleSide.Player) })
            {
                anim.Cue(new EffectCue { Move = move, Category = category, Critical = critical, FromSide = from, ToSide = self ? from : to });
                for (int i = 0; i < 16; i++) { yield return ($"{move} {from} {i}", anim, false); Advance(anim, 0.08f); }
                Advance(anim, 0.6f);
                yield return ($"{move} {from} after", anim, true);
            }

        anim.Dynamax(BattleSide.Enemy, 0, gigantamax: false);
        for (int i = 0; i < 20; i++) { yield return ($"giant {i}", anim, false); Advance(anim, 0.15f); }

        anim.ThrowBall("Poké Ball", 0, 3);
        for (float t = 0f; t < BattleAnimator.BallThrowTime(3) + 0.6f; t += 0.1f) { yield return ($"ball {t:F1}", anim, false); Advance(anim, 0.1f); }

        anim.Faint(BattleSide.Player);
        for (int i = 0; i < 16; i++) { yield return ($"faint {i}", anim, false); Advance(anim, 0.1f); }
        yield return ("menu at the end", anim, true);
    }

    [Fact]
    public void EveryShotUnderAMannerWithItsSwitchesOffIsClassicsToTheBit()
    {
        Assert.True(CameraManners.Classic.IsStill);
        Assert.True(AllOff.IsStill);
        var kinds = new HashSet<ShotKind>();
        foreach (var (moment, anim, choosing) in Moments())
        {
            var classic = BattleCamera.Direct(anim, Places, choosing);
            var named = BattleCamera.Direct(anim, Places, choosing, CameraManners.Classic);
            var off = BattleCamera.Direct(anim, Places, choosing, AllOff);
            Assert.True(classic.Shot.Equals(named.Shot) && classic.Kind == named.Kind, $"{moment}: Classic named films otherwise");
            Assert.True(classic.Shot.Equals(off.Shot) && classic.Kind == off.Kind, $"{moment}: a manner with every switch off films otherwise");
            kinds.Add(classic.Kind);
        }
        // The walk saw the director's shots, not one of them over and over
        foreach (var kind in new[] { ShotKind.Intro, ShotKind.Overview, ShotKind.SendOut, ShotKind.Attacker, ShotKind.Target,
                     ShotKind.Self, ShotKind.Critical, ShotKind.Push, ShotKind.Follow, ShotKind.Punch, ShotKind.Orbit, ShotKind.Giant,
                     ShotKind.Throw, ShotKind.Ball, ShotKind.BallClose, ShotKind.Faint })
            Assert.Contains(kind, kinds);
    }

    [Fact]
    public void ADirectorGivenAMannerWithItsSwitchesOffEasesAndCutsAsClassic()
    {
        var classic = new BattleCamera();
        var off = new BattleCamera { Manner = AllOff };
        foreach (var (moment, anim, choosing) in Moments())
        {
            var a = classic.Update(1f / 60f, anim, Places, 0.5f, choosing);
            var b = off.Update(1f / 60f, anim, Places, 0.5f, choosing);
            Assert.True(a.Position.Equals(b.Position) && a.Target.Equals(b.Target) && a.FovY.Equals(b.FovY), $"{moment}: the camera stands otherwise");
            Assert.Equal(classic.Kind, off.Kind);
        }
    }

    [Fact]
    public void TheRowChoosesTheMannerABattleIsFilmedIn()
    {
        var settings = new GameSettings();
        // Decision 1: each region in its own manner unless chosen
        Assert.Equal(BattleCameraChoice.Region, settings.BattleCamera);
        Assert.Equal(OptionCategory.Battle, OptionsScreen.CategoryOf(OptionRow.BattleCamera));
        var sinnoh = RegionDatabase.Get("Sinnoh")!;
        Assert.Same(CameraManners.Sinnoh, CameraManners.For(settings.BattleCamera, sinnoh.BattleCamera));

        var screen = new OptionsScreen();
        screen.Select(OptionRow.BattleCamera);
        Assert.Equal(OptionRow.BattleCamera, screen.Selected);
        OptionsScreen.Change(settings, OptionRow.BattleCamera, 1);
        Assert.Equal(BattleCameraChoice.Lively, settings.BattleCamera);
        Assert.Equal(CameraManners.Find("galar") ?? CameraManners.Sinnoh, CameraManners.For(settings.BattleCamera, sinnoh.BattleCamera));
        OptionsScreen.Change(settings, OptionRow.BattleCamera, 1);
        Assert.Equal(BattleCameraChoice.Classic, settings.BattleCamera);
        Assert.Same(CameraManners.Classic, CameraManners.For(settings.BattleCamera, sinnoh.BattleCamera));

        // The row's words, for every value
        foreach (var choice in Enum.GetValues<BattleCameraChoice>())
        {
            settings.BattleCamera = choice;
            var (label, value, help) = OptionsScreen.Describe(settings, OptionRow.BattleCamera);
            Assert.False(string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(help));
            Assert.DoesNotContain("options.", value + help);
        }
    }

    /// <summary>
    /// Alola's manner alone keeps the trainers in the picture (plan 45 · BC6's last part): the player stands wholly off
    /// the overview's line, beside the west of their platform and on the far side from the side shot's camera.
    /// </summary>
    [Fact]
    public void OnlyAlolasMannerKeepsTheTrainersInThePicture()
    {
        Assert.Equal(new[] { "alola" }, CameraManners.All.Where(m => m.TrainersIn).Select(m => m.Name));
        Assert.False(CameraManners.Classic.TrainersIn);
        var spot = BattleRenderer.StandSpot;
        Assert.True(spot.X < BattleStage.PlayerSpot.X - BattleStage.PlayerPlatformRadius - 1f, "the player stands well west of their platform");
        Assert.True(spot.Z <= BattleStage.PlayerSpot.Z, "and no nearer the lens than its middle");
    }

    [Fact]
    public void EveryRegionsFileNamesAMannerOfTheTable()
    {
        foreach (var region in RegionDatabase.All)
            CameraManners.OfRegion(region.BattleCamera);
        Assert.Same(CameraManners.Sinnoh, CameraManners.OfRegion(null));
        Assert.Equal("sinnoh", RegionDatabase.Get("Sinnoh")!.BattleCamera);
        Assert.Throws<FormatException>(() => CameraManners.OfRegion("galar's own"));
        // A name the table lacks films as Sinnoh does in the middle of a battle rather than throwing there
        Assert.Same(CameraManners.Sinnoh, CameraManners.For(BattleCameraChoice.Region, "nowhere"));
        // Every manner's name is its own
        Assert.Equal(CameraManners.All.Count, CameraManners.All.Select(m => m.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
