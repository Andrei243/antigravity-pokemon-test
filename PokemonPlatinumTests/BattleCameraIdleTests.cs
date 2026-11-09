using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 45 · BC2: the menu filmed by a manner's idle cycle, the picture's free part, and the player's own side from the
/// front, cut to and from.
/// </summary>
public class BattleCameraIdleTests
{
    private static readonly CameraManner[] Moving = CameraManners.All.Where(m => !m.IsStill).ToArray();

    public static IEnumerable<object[]> Manners => Moving.Select(m => new object[] { m.Name });

    /// <summary>Who stands where in a battle of these many places a side, each of this height.</summary>
    private static (BattleAnimator Anim, FxPlaces Places) Battle(int foes, int own, float foeHeight, float ownHeight)
    {
        var anim = new BattleAnimator();
        if (foes > 1 || own > 1) anim.SetSlots(own, foes);
        var places = new FxPlaces();
        for (int slot = 0; slot < foes; slot++)
            places.Set(BattleSide.Enemy, slot, BattlePlaces.Spot(BattleSide.Enemy, slot, foes) + Vector3.UnitY * BattleStage.PlatformHeight, foeHeight * BattlePlaces.Fit(foes));
        for (int slot = 0; slot < own; slot++)
            places.Set(BattleSide.Player, slot, BattlePlaces.Spot(BattleSide.Player, slot, own) + Vector3.UnitY * BattleStage.PlatformHeight, ownHeight * BattlePlaces.Fit(own));
        anim.Update(2f, null, null);
        anim.NoteMenu(true);
        return (anim, places);
    }

    private static void Advance(BattleAnimator anim, float seconds)
    {
        for (float t = 0f; t < seconds - 1e-4f; t += 1f / 60f) anim.Update(1f / 60f, null, null);
    }

    public static IEnumerable<object[]> Formats()
    {
        foreach (var m in Moving)
            foreach (var (foes, own) in new[] { (1, 1), (2, 2), (3, 1), (5, 1) })
                foreach (var (foeH, ownH) in new[] { (0.6f, 0.6f), (1.4f, 1.2f), (2.6f, 2.2f) })
                    yield return new object[] { m.Name, foes, own, foeH, ownH };
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void EveryIdleShotHoldsItsSubjectsInThePicturesFreePart(string name, int foes, int own, float foeH, float ownH)
    {
        var manner = CameraManners.Find(name)!;
        var (anim, places) = Battle(foes, own, foeH, ownH);
        var panels = IdleFrame.Panels(foes, own);
        foreach (var idle in manner.Cycle.Where(s => s != IdleShot.Overview))
        {
            if (BattleCamera.IdleShotOf(idle, anim, places, manner) is not { } shot) continue;
            for (int side = 0; side < 2; side++)
                for (int slot = 0; slot < anim.SlotsOf((BattleSide)side); slot++)
                {
                    var box = IdleFrame.BoxOf(shot, places, (BattleSide)side, slot);
                    // The shoulder's foreground is the player's own Pokémon's back, which the panels may cover
                    if (idle == IdleShot.Shoulder && (BattleSide)side == BattleSide.Player) continue;
                    bool subject = idle switch
                    {
                        IdleShot.FoeFace or IdleShot.Shoulder => (BattleSide)side == BattleSide.Enemy,
                        IdleShot.OwnFace => (BattleSide)side == BattleSide.Player,
                        _ => true
                    };
                    if (subject)
                        Assert.True(box is { } b && IdleFrame.Holds(b, panels), $"{name} {idle}, {(BattleSide)side} {slot} of {foes} v {own}: {box}");
                    else
                        Assert.True(IdleFrame.Outside(box) || box is { } b2 && IdleFrame.Holds(b2, panels), $"{name} {idle}, {(BattleSide)side} {slot} stands half under a panel: {box}");
                }
        }
        // A single of ordinary bodies films every shot its manner names
        if (foes == 1 && own == 1 && foeH < 2f)
            Assert.Equal(Math.Max(1, manner.Cycle.Count), BattleCamera.CycleFor(anim, places, manner).Count);
    }

    [Theory]
    [MemberData(nameof(Manners))]
    public void NoEaseOfTheCyclePassesThePlayersPlatform(string name)
    {
        var manner = CameraManners.Find(name)!;
        foreach (var (foes, own) in new[] { (1, 1), (2, 2), (5, 1) })
        {
            var (anim, places) = Battle(foes, own, 1.2f, 0.8f);
            var shots = BattleCamera.CycleFor(anim, places, manner);
            for (int i = 0; i < shots.Count; i++)
            {
                var (a, aKind) = shots[i];
                var (b, bKind) = shots[(i + 1) % shots.Count];
                if (BattleCamera.Eases(a, aKind, b, bKind))
                {
                    Assert.False(BattleCamera.CrossesPlayerSide(a.Position, b.Position), $"{name}: {aKind} to {bKind}");
                    Assert.NotEqual(ShotKind.OwnFront, aKind);
                    Assert.NotEqual(ShotKind.OwnFront, bKind);
                }
            }
            // And a director following the cycle through two of its rounds frame by frame stays clear of the platform,
            // cutting to and from the player's own front
            var camera = new BattleCamera { Manner = manner };
            var platform = BattleStage.PlayerSpot + new Vector3(0, 1.2f, 0);
            float rounds = 2f * (manner.Cycle.Count > 0 ? manner.Cycle.Count * (manner.CycleHold + manner.CycleEase) : manner.DriftPeriod);
            var last = ShotKind.Overview;
            for (float t = 0f; t < rounds; t += 1f / 60f)
            {
                anim.Update(1f / 60f, null, null);
                camera.Update(1f / 60f, anim, places, 0f, choosing: true);
                Assert.True(Vector3.Distance(camera.Current.Position, platform) > BattleCamera.Clearance,
                    $"{name} at {anim.MenuAge:F2} s: the camera came within {Vector3.Distance(camera.Current.Position, platform):F2} of the player's platform");
                if ((camera.Kind == ShotKind.OwnFront) != (last == ShotKind.OwnFront))
                    Assert.Equal(BattleCamera.Direct(anim, places, true, manner).Shot, camera.Current);
                last = camera.Kind;
            }
        }
    }

    [Theory]
    [MemberData(nameof(Manners))]
    public void TheCycleIsAFunctionOfTheMenusOpenTime(string name)
    {
        var manner = CameraManners.Find(name)!;
        // Opened at once, the menu is the overview, as it always was
        var (fresh, places) = Battle(1, 1, 1.2f, 1f);
        var first = BattleCamera.Direct(fresh, places, true, manner);
        Assert.Equal(ShotKind.Overview, first.Kind);
        Assert.Equal(BattleCamera.Overview(fresh.Time), first.Shot);

        // Two battles stepped at different rates to the same moments film the same
        var (a, _) = Battle(1, 1, 1.2f, 1f);
        var (b, _) = Battle(1, 1, 1.2f, 1f);
        // (off the cycle's boundaries, which fall on whole tenths, so the two clocks' last bits decide no cut)
        a.Update(0.013f, null, null);
        b.Update(0.013f, null, null);
        for (int i = 0; i < 400; i++)
        {
            for (int k = 0; k < 2; k++) a.Update(1f / 60f, null, null);
            b.Update(1f / 30f, null, null);
            var sa = BattleCamera.Direct(a, places, true, manner);
            var sb = BattleCamera.Direct(b, places, true, manner);
            Assert.Equal(sa.Kind, sb.Kind);
            Assert.True(Vector3.Distance(sa.Shot.Position, sb.Shot.Position) < 0.02f, $"{name} at {a.MenuAge:F2}");
        }

        // The menu closed and opened again begins on the overview
        a.NoteMenu(false);
        Assert.True(a.MenuAge < 0f);
        Assert.Equal(ShotKind.Overview, BattleCamera.Direct(a, places, true, manner).Kind);
        a.NoteMenu(true);
        Assert.Equal(BattleCamera.Overview(a.Time), BattleCamera.Direct(a, places, true, manner).Shot);
    }

    [Fact]
    public void ACycleFilmsTheMenuAndTheTargetMenuBringsTheOverviewBack()
    {
        var galar = CameraManners.Find("galar")!;
        var (anim, places) = Battle(1, 1, 1.2f, 1f);
        var kinds = new HashSet<ShotKind>();
        for (float t = 0f; t < galar.Cycle.Count * (galar.CycleHold + galar.CycleEase); t += 0.25f)
        {
            kinds.Add(BattleCamera.Direct(anim, places, true, galar).Kind);
            Advance(anim, 0.25f);
        }
        Assert.Contains(ShotKind.Idle, kinds);
        Assert.Contains(ShotKind.OwnFront, kinds);
        // No menu of the commands or the move cards open (a target, the bag, a question): the overview
        anim.NoteMenu(false);
        Assert.Equal(ShotKind.Overview, BattleCamera.Direct(anim, places, true, galar).Kind);
        // Classic never leaves the overview at a menu
        anim.NoteMenu(true);
        Advance(anim, 6f);
        Assert.Equal(ShotKind.Overview, BattleCamera.Direct(anim, places, true, CameraManners.Classic).Kind);
    }

    [Fact]
    public void SinnohBreathesRoundTheOverviewAndNeverLeavesItsSide()
    {
        var sinnoh = CameraManners.Sinnoh;
        var (anim, places) = Battle(1, 1, 1.2f, 1f);
        var overview = BattleCamera.Overview(0f);
        float furthest = 0f;
        for (float t = 0f; t < sinnoh.DriftPeriod * 2f; t += 0.1f)
        {
            var (shot, kind, _) = BattleCamera.Direct(anim, places, true, sinnoh);
            Assert.Equal(ShotKind.Overview, kind);
            Assert.True(shot.Position.Z > BattleStage.PlayerSpot.Z + BattleCamera.Clearance, "behind the player's platform, on the overview's side");
            furthest = Math.Max(furthest, Vector3.Distance(shot.Position, overview.Position));
            Advance(anim, 0.1f);
        }
        Assert.InRange(furthest, 0.4f, 3f);
    }
}
