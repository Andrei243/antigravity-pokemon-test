using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Graphics;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 45 · BC3: the picture split in two. The band's halves, the moments the animator notes, which manners split at
/// which, the halves' timing (in, held, cut short, out) and each half's subjects framed in their own half.
/// </summary>
[Collection("Shared clocks and dice")]
public class BattleCameraSplitTests
{
    /// <summary>Who stands where in a battle of these many places a side, everyone out and standing.</summary>
    private static (BattleAnimator Anim, FxPlaces Places) Battle(int foes, int own, float foeHeight, float ownHeight, bool leads = true)
    {
        var anim = new BattleAnimator { LeadsMayDivide = leads };
        if (foes > 1 || own > 1) anim.SetSlots(own, foes);
        var places = new FxPlaces();
        for (int slot = 0; slot < foes; slot++)
        {
            places.Set(BattleSide.Enemy, slot, BattlePlaces.Spot(BattleSide.Enemy, slot, foes) + Vector3.UnitY * BattleStage.PlatformHeight, foeHeight * BattlePlaces.Fit(foes));
            anim[BattleSide.Enemy, slot].Present = true;
        }
        for (int slot = 0; slot < own; slot++)
        {
            places.Set(BattleSide.Player, slot, BattlePlaces.Spot(BattleSide.Player, slot, own) + Vector3.UnitY * BattleStage.PlatformHeight, ownHeight * BattlePlaces.Fit(own));
            anim[BattleSide.Player, slot].Present = true;
        }
        return (anim, places);
    }

    private static void Advance(BattleAnimator anim, float seconds)
    {
        for (float t = 0f; t < seconds - 1e-4f; t += 1f / 60f) anim.Update(1f / 60f, null, null);
    }

    private static CameraManner Galar => CameraManners.Find("galar")!;

    [Fact]
    public void TheHalvesCoverThePictureOnceInAndNothingBefore()
    {
        float whole = SplitBand.Width * SplitBand.Height;
        Assert.Empty(SplitBand.Region(true, 0f));
        Assert.Empty(SplitBand.Region(false, 0f));
        float both = SplitBand.Area(SplitBand.Region(true, 1f)) + SplitBand.Area(SplitBand.Region(false, 1f));
        Assert.InRange(both, whole - 1f, whole + 1f);
        float last = 0f;
        for (float a = 0.1f; a <= 1f; a += 0.1f)
        {
            float area = SplitBand.Area(SplitBand.Region(true, a)) + SplitBand.Area(SplitBand.Region(false, a));
            Assert.True(area >= last, "the halves only grow as they come in");
            last = area;
        }
        // The foe's half is the upper left, and the HP boxes lie over their own halves
        Assert.True(SplitBand.InRegion(true, 1f, new Vector2(200, 200)));
        Assert.True(SplitBand.InRegion(false, 1f, new Vector2(1700, 900)));
        foreach (var p in IdleFrame.Panels(1, 1).Take(1))
            Assert.True(SplitBand.InRegion(true, 1f, new Vector2(p.X + p.Width, p.Y + p.Height), SplitBand.BandWidth / 2f));
        foreach (var p in IdleFrame.Panels(1, 1).Skip(1))
            Assert.True(SplitBand.InRegion(false, 1f, new Vector2(p.X, p.Y), SplitBand.BandWidth / 2f));
    }

    [Theory]
    [InlineData(PokemonPlatinumEngine.Core.GraphicsQuality.High)]
    [InlineData(PokemonPlatinumEngine.Core.GraphicsQuality.Medium)]
    [InlineData(PokemonPlatinumEngine.Core.GraphicsQuality.Low)]
    public void ASplitHeldDrawsNoMorePixelsThanTheScene(PokemonPlatinumEngine.Core.GraphicsQuality quality)
    {
        // Decision 4, as a count: the two halves' boxes, held whole, in their targets, against the scene target's pixels
        var profile = PokemonPlatinumEngine.Core.QualityProfile.For(quality);
        int sw = (int)(1920 * profile.SceneScale), sh = (int)(1080 * profile.SceneScale);
        var (w, h) = RenderContext.SplitSize(sw, sh);
        Assert.InRange(w, 1, RenderContext.SplitMostWidth);
        Assert.InRange(h, 1, RenderContext.SplitMostHeight);
        Assert.InRange((float)w / h, 16f / 9f - 0.01f, 16f / 9f + 0.01f);
        double halves = 0;
        foreach (bool foe in new[] { true, false })
        {
            var box = SplitBand.Box(SplitBand.Region(foe, 1f));
            halves += Math.Ceiling(box.Width * w / SplitBand.Width) * Math.Ceiling(box.Height * h / SplitBand.Height);
        }
        Assert.True(halves <= (double)sw * sh * 1.002, $"{quality}: the halves draw {halves:N0} pixels, the scene {sw * sh:N0}");
    }

    [Fact]
    public void TheLeadsAreNotedOnceBothSidesStandAfterTheSweep()
    {
        var (anim, _) = Battle(1, 1, 1.2f, 1f);
        anim.Update(1f, null, null);
        Assert.Empty(anim.SplitMoments);
        anim.Update(1f, null, null);
        var moment = Assert.Single(anim.SplitMoments);
        Assert.Equal(SplitReason.Leads, moment.Reason);
        Advance(anim, 3f);
        Assert.Single(anim.SplitMoments);

        // Nothing where the leads may not divide (a wild battle against no legendary)
        var (wild, _) = Battle(1, 1, 1.2f, 1f, leads: false);
        Advance(wild, 3f);
        Assert.Empty(wild.SplitMoments);

        // A last Pokémon's moment comes once it is out of its ball
        var (last, _) = Battle(1, 1, 1.2f, 1f, leads: false);
        Advance(last, 2f);
        last[BattleSide.Enemy, 0].SendOutAge = 0f;
        last.NoteLastPokemon(BattleSide.Enemy, 0, boss: true);
        Advance(last, 0.5f);
        Assert.Empty(last.SplitMoments);
        Advance(last, 0.6f);
        var lastMoment = Assert.Single(last.SplitMoments);
        Assert.Equal(SplitReason.LastPokemon, lastMoment.Reason);
        Assert.True(lastMoment.Boss);
    }

    [Fact]
    public void OnlyTheMannersThatSplitAtAMomentSplit()
    {
        var leads = new SplitMoment(SplitReason.Leads, 0f, false);
        var last = new SplitMoment(SplitReason.LastPokemon, 0f, false);
        var bossLast = new SplitMoment(SplitReason.LastPokemon, 0f, true);
        Assert.False(BattleCamera.SplitsAt(CameraManners.Classic, leads));
        Assert.False(BattleCamera.SplitsAt(CameraManners.Classic, bossLast));
        Assert.False(BattleCamera.SplitsAt(CameraManners.Sinnoh, leads));
        Assert.False(BattleCamera.SplitsAt(CameraManners.Sinnoh, last));
        Assert.True(BattleCamera.SplitsAt(CameraManners.Sinnoh, bossLast));
        Assert.True(BattleCamera.SplitsAt(CameraManners.Find("unova")!, leads));
        Assert.True(BattleCamera.SplitsAt(Galar, leads));
        Assert.True(BattleCamera.SplitsAt(Galar, last));

        var (anim, places) = Battle(1, 1, 1.2f, 1f);
        Advance(anim, 2.1f);
        Assert.NotNull(BattleCamera.SplitNow(anim, places, Galar));
        Assert.Null(BattleCamera.SplitNow(anim, places, CameraManners.Classic));
        Assert.Null(BattleCamera.SplitNow(anim, places, CameraManners.Sinnoh));
        Assert.Null(BattleCamera.SplitNow(anim, places, null));
        // Never with the Battle Scene off
        Assert.Null(BattleCamera.SplitNow(anim, places, Galar, scene: false));
        // Never under REDUCE MOTION
        float was = BattleCamera.Motion;
        try
        {
            BattleCamera.Motion = 0f;
            Assert.Null(BattleCamera.SplitNow(anim, places, Galar));
        }
        finally { BattleCamera.Motion = was; }
    }

    [Fact]
    public void ASplitComesInIsHeldAndGoesOutByTheAnimatorsClock()
    {
        var (anim, places) = Battle(1, 1, 1.2f, 1f);
        Advance(anim, 1.8f);
        anim.Update(1f / 60f, null, null);
        var moment = Assert.Single(anim.SplitMoments);
        float start = moment.Start;
        Assert.Equal(0f, BattleCamera.SplitAmount(moment, start - 0.01f));
        Assert.InRange(BattleCamera.SplitAmount(moment, start + BattleCamera.SplitIn / 2f), 0.3f, 0.7f);
        Assert.Equal(1f, BattleCamera.SplitAmount(moment, start + BattleCamera.SplitIn + 0.5f));
        float outAt = start + BattleCamera.SplitIn + BattleCamera.SplitHoldLeads;
        Assert.Equal(1f, BattleCamera.SplitAmount(moment, outAt - 0.01f));
        Assert.InRange(BattleCamera.SplitAmount(moment, outAt + BattleCamera.SplitOut / 2f), 0.3f, 0.7f);
        Assert.Equal(0f, BattleCamera.SplitAmount(moment, outAt + BattleCamera.SplitOut));

        // Something that waits on the player cuts it short: out from then, once it is in
        var (cut, cutPlaces) = Battle(1, 1, 1.2f, 1f);
        Advance(cut, 1.8f);
        cut.Update(1f / 60f, null, null);
        Advance(cut, 0.5f);
        cut.NoteChoosing(true);
        var cutMoment = Assert.Single(cut.SplitMoments);
        var (cutOut, cutEnd) = BattleCamera.SplitWindow(cutMoment);
        Assert.InRange(cutOut, cut.Time - 0.001f, cut.Time + 0.001f);
        Assert.Equal(cutOut + BattleCamera.SplitOut, cutEnd);
        // And it stays cut short once the menu has closed again
        cut.NoteChoosing(false);
        Advance(cut, 0.4f);
        Assert.Null(BattleCamera.SplitNow(cut, cutPlaces, Galar));

        // A moment that comes while the menu is already open never splits
        var (late, latePlaces) = Battle(1, 1, 1.2f, 1f);
        for (int i = 0; i < 150; i++)
        {
            late.Update(1f / 60f, null, null);
            late.NoteChoosing(true);
        }
        Assert.Single(late.SplitMoments);
        Assert.Null(BattleCamera.SplitNow(late, latePlaces, Galar));
    }

    public static IEnumerable<object[]> Formats()
    {
        foreach (var (foes, own) in new[] { (1, 1), (2, 2), (3, 1), (5, 1) })
            foreach (var (foeH, ownH) in new[] { (0.6f, 0.6f), (1.4f, 1.2f), (2.6f, 2.2f) })
                yield return new object[] { foes, own, foeH, ownH };
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void EachHalfHoldsItsSideInItsOwnHalfOffThePanels(int foes, int own, float foeH, float ownH)
    {
        var (anim, places) = Battle(foes, own, foeH, ownH);
        Advance(anim, 2.4f);
        var split = BattleCamera.SplitNow(anim, places, Galar);
        Assert.True(split is { Whole: true }, "the leads' split is whole 0.3 s after it began");
        var panels = IdleFrame.Panels(foes, own);
        foreach (bool foe in new[] { true, false })
        {
            var shot = foe ? split!.Value.Foe : split!.Value.Own;
            var side = foe ? BattleSide.Enemy : BattleSide.Player;
            for (int slot = 0; slot < anim.SlotsOf(side); slot++)
            {
                var box = IdleFrame.BoxOf(shot, places, side, slot);
                Assert.True(box is { } b && IdleFrame.Holds(b, panels), $"{side} {slot} of {foes} v {own} ({foeH}, {ownH}): {box} under a panel");
                var r = box!.Value;
                foreach (var corner in new[] { new Vector2(r.X, r.Y), new Vector2(r.X + r.Width, r.Y), new Vector2(r.X, r.Y + r.Height), new Vector2(r.X + r.Width, r.Y + r.Height) })
                    Assert.True(SplitBand.InRegion(foe, 1f, corner, SplitBand.BandWidth / 2f), $"{side} {slot} of {foes} v {own} ({foeH}, {ownH}): {box} reaches over the band");
            }
            // The player's own half stands between the platforms looking south: never behind the player's platform
            if (!foe) Assert.True(shot.Position.Z < BattleStage.PlayerSpot.Z, "the player's half films them from the front");
        }
    }

    [Fact]
    public void TheDirectorsOwnShotsDoNotMoveWithAMoment()
    {
        // A moment noted changes nothing the director gives: the split is the renderer's, over the director's shot
        var (anim, places) = Battle(1, 1, 1.2f, 1f);
        var (bare, _) = Battle(1, 1, 1.2f, 1f, leads: false);
        for (int i = 0; i < 240; i++)
        {
            anim.Update(1f / 60f, null, null);
            bare.Update(1f / 60f, null, null);
            foreach (var manner in CameraManners.All)
                Assert.Equal(BattleCamera.Direct(bare, places, false, manner), BattleCamera.Direct(anim, places, false, manner));
        }
        Assert.NotEmpty(anim.SplitMoments);
    }
}
