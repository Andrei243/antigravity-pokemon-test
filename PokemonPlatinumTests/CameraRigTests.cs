using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>
/// The camera kit (plan 09 · L4; style guide, "Story scenes", "The camera kit"): the rig as a function of the time
/// handed to it (a pan, a follow, a path, the lens, the bars, the field's pace), what the renderer makes of its lens,
/// and the script's words for it.
/// </summary>
public class CameraRigTests
{
    private const float Frame = 1f / 60f;

    private static readonly Vector2 Player = new(20.5f, 30.5f);

    /// <summary>Runs the rig for a time a frame at a time, looking at the player each frame as the renderer does.</summary>
    private static Vector2 Run(CameraRig rig, float seconds, Vector2? player = null)
    {
        int frames = (int)MathF.Round(seconds / Frame);
        for (int i = 0; i < frames; i++)
        {
            rig.Advance(Frame);
            rig.Look(player ?? Player);
        }
        return rig.Look(player ?? Player);
    }

    private static void Near(Vector2 expected, Vector2 actual, float within = 0.01f) =>
        Assert.True(Vector2.Distance(expected, actual) <= within, $"expected {expected}, was {actual}");

    // ------------------------------------------------------------------ at rest

    [Fact]
    public void ARigNobodyToldAnythingLeavesTheCameraAsItIs()
    {
        var rig = new CameraRig();
        Run(rig, 1f);
        // Exactly: the pictures of every scene that uses none of it must not move by a texel
        Assert.Equal(Player, rig.Look(Player));
        Assert.Equal(1f, rig.Zoom);
        Assert.Equal(MapScene.OutdoorFovYDeg, rig.Fov(MapScene.OutdoorFovYDeg));
        Assert.Equal(MapScene.IndoorFovYDeg, rig.Fov(MapScene.IndoorFovYDeg));
        Assert.Equal(0f, rig.Bars);
        Assert.Equal(1f, rig.Pace);
        Assert.True(rig.AtRest);
        Assert.False(rig.Moving);
        Assert.False(rig.Away);
    }

    [Fact]
    public void TheKitsNumbersAreTheStyleGuides()
    {
        // Style guide, "Story scenes", "The camera kit": the table's numbers, and the title's own bars
        Assert.Equal((0.8f, 0.6f, 0.8f, 0.8f, 0.4f), (CameraRig.PanSeconds, CameraRig.ReleaseSeconds, CameraRig.FollowSeconds, CameraRig.ZoomSeconds, CameraRig.BarSeconds));
        Assert.Equal((0.5f, 4f), (CameraRig.WidestZoom, CameraRig.TightestZoom));
        Assert.Equal((4f, 0.25f, 16f, 0.5f, 8), (CameraRig.PathPace, CameraRig.SlowestPath, CameraRig.FastestPath, CameraRig.PathEase, CameraRig.MostPathTiles));
        Assert.Equal((2f, 60f, 0.2f, 0.1f), (CameraRig.SlowSeconds, CameraRig.LongestSlow, CameraRig.SlowEase, CameraRig.SlowestPace));
        Assert.Equal(120f, CameraRig.BarHeight);
    }

    // ------------------------------------------------------------------ where it looks

    [Fact]
    public void APanEasesOutOfThePlayerAndIntoItsSpot()
    {
        var rig = new CameraRig();
        var spot = new Vector2(30.5f, 30.5f);
        rig.Pan(spot.X, spot.Y, 1f);
        Assert.True(rig.Away);

        // A tenth of the way through its time it has gone under three hundredths of the way (3t² − 2t³), half way at
        // half time, and as little is left a tenth before its end
        Assert.InRange(Run(rig, 0.1f).X - Player.X, 0.2f, 0.32f);
        Near(new Vector2(25.5f, 30.5f), Run(rig, 0.4f), 0.05f);
        Assert.InRange(spot.X - Run(rig, 0.4f).X, 0.2f, 0.32f);
        Assert.True(rig.Moving);
        Near(spot, Run(rig, 0.2f), 0.0001f);
        Assert.False(rig.Moving);

        // There it stays, wherever the player goes
        Near(spot, Run(rig, 2f, new Vector2(2.5f, 3.5f)), 0.0001f);
    }

    [Fact]
    public void APanFromASpotEasesOnFromThere()
    {
        var rig = new CameraRig();
        var first = new Vector2(30.5f, 30.5f);
        var second = new Vector2(30.5f, 20.5f);
        rig.Pan(first.X, first.Y, 0.5f);
        Run(rig, 0.6f);

        // Sent on, it neither jumps to the new spot nor goes back by way of the player
        rig.Pan(second.X, second.Y, 1f);
        Near(first, rig.Look(Player), 0.0001f);
        var half = Run(rig, 0.5f);
        Near(new Vector2(30.5f, 25.5f), half, 0.05f);
        Near(second, Run(rig, 0.6f), 0.0001f);

        // ... and the same in the middle of a pan, from wherever it had got to
        var third = new Vector2(10.5f, 20.5f);
        rig.Pan(third.X, third.Y, 1f);
        var onTheWay = Run(rig, 0.5f);
        rig.Pan(first.X, first.Y, 1f);
        Near(onTheWay, rig.Look(Player), 0.0001f);
        Near(first, Run(rig, 1.1f), 0.0001f);
    }

    [Fact]
    public void AFollowKeepsOnSomeonesFeet()
    {
        var rig = new CameraRig();
        var walker = new NPC { Name = "Walker", GridX = 25, GridY = 28 };
        rig.Follow(walker, 0.5f);
        Near(new Vector2(25.5f, 28.5f), Run(rig, 0.6f), 0.0001f);

        // Wherever they go, in the middle of a step too
        walker.GridX = 28;
        walker.StepOffsetX = -0.5f;
        Near(new Vector2(28f, 28.5f), Run(rig, Frame), 0.0001f);
        walker.StepOffsetX = 0f;
        walker.GridY = 40;
        Near(new Vector2(28.5f, 40.5f), Run(rig, Frame), 0.0001f);

        // The player followed is the camera home again, with the lens and the bars left as they are
        rig.ZoomTo(2f, 0f);
        rig.Letterbox(true, 0f);
        rig.FollowPlayer(0.5f);
        Near(Player, Run(rig, 0.6f), 0.0001f);
        Assert.False(rig.Away);
        Assert.Null(rig.Following);
        Assert.Equal(2f, rig.Zoom);
        Assert.Equal(CameraRig.BarHeight, rig.Bars);

        // A scene that warps away from whoever it kept on leaves them behind at once, the lens and the bars as they are
        rig.Follow(walker, 0f);
        Run(rig, 0.1f);
        Assert.Same(walker, rig.Following);
        rig.ComeHome();
        Assert.Null(rig.Following);
        Assert.Equal(Player, rig.Look(Player));
        Assert.Equal(2f, rig.Zoom);
        Assert.Equal(CameraRig.BarHeight, rig.Bars);
        // A spot or a path is nobody
        rig.Pan(3.5f, 3.5f, 0f);
        Assert.Null(rig.Following);
    }

    [Fact]
    public void APathsEndsAreEased()
    {
        const float length = 12f, pace = 4f;
        float whole = CameraRig.PathSeconds(length, pace);
        // The length at its pace, and the half second the pace takes to gather
        Assert.Equal(length / pace + CameraRig.PathEase, whole, 4);
        Assert.Equal(0f, CameraRig.Along(0f, length, pace));
        Assert.Equal(length, CameraRig.Along(whole, length, pace), 4);
        Assert.Equal(length, CameraRig.Along(whole + 5f, length, pace));

        // It never goes back, and never faster than its pace
        float before = 0f;
        for (float t = Frame; t <= whole + Frame; t += Frame)
        {
            float at = CameraRig.Along(t, length, pace);
            Assert.InRange(at - before, 0f, pace * Frame + 1e-4f);
            before = at;
        }

        // It leaves and arrives at next to no speed, and holds its pace between
        float Speed(float t) => (CameraRig.Along(t + Frame, length, pace) - CameraRig.Along(t, length, pace)) / Frame;
        Assert.InRange(Speed(0f), 0f, pace * 0.01f);
        Assert.InRange(Speed(whole - Frame), 0f, pace * 0.01f);
        Assert.Equal(pace, Speed(whole / 2f), 2);
        Assert.Equal(pace / 2f, Speed(CameraRig.PathEase / 2f - Frame / 2f), 1);
        // The two ends are each other's mirror
        Assert.Equal(length, CameraRig.Along(0.3f, length, pace) + CameraRig.Along(whole - 0.3f, length, pace), 3);

        // A path too short to gather its pace and lose it in half a second each does both in the time it has
        Assert.Equal(2f * 1f / pace, CameraRig.PathSeconds(1f, pace), 4);
        Assert.Equal(0.5f, CameraRig.Along(CameraRig.PathSeconds(1f, pace) / 2f, 1f, pace), 3);
        Assert.Equal(0f, CameraRig.PathSeconds(0f, pace));
    }

    [Fact]
    public void APathGoesThroughItsTilesFromWhereTheCameraLooks()
    {
        var rig = new CameraRig();
        rig.Look(Player);
        var corner = new Vector2(24.5f, 30.5f);
        var end = new Vector2(24.5f, 27.5f);
        float seconds = rig.Path(new[] { corner, end }, 2f);
        // Four tiles east from the player and three north, at two tiles a second
        Assert.Equal(7f / 2f + CameraRig.PathEase, seconds, 3);

        // It begins where the camera looked, and keeps to the legs: east first, then north
        Near(Player, rig.Look(Player), 0.0001f);
        bool turned = false;
        var last = Player;
        for (float t = 0f; t < seconds; t += Frame)
        {
            var at = Run(rig, Frame);
            if (at.Y < 30.5f - 0.001f) turned = true;
            if (!turned) Assert.Equal(30.5f, at.Y, 3);
            else Assert.Equal(24.5f, at.X, 3);
            Assert.True(at.X >= last.X - 0.0001f && at.Y <= last.Y + 0.0001f);
            last = at;
        }
        Assert.True(turned);
        Assert.True(rig.Moving || Vector2.Distance(end, last) < 0.01f);
        Near(end, Run(rig, 0.2f), 0.0001f);
        Assert.False(rig.Moving);
        // It stays at the last, wherever the player is
        Near(end, Run(rig, 1f, new Vector2(3.5f, 3.5f)), 0.0001f);

        // A pace out of bounds is brought within them
        Assert.Equal(CameraRig.PathSeconds(3f, CameraRig.FastestPath), new CameraRig().Path(new[] { corner, end }, 99f), 4);
        Assert.Equal(0f, new CameraRig().Path(Array.Empty<Vector2>()));
    }

    // ------------------------------------------------------------------ the lens

    [Fact]
    public void AZoomChangesTheFieldOfViewAlone()
    {
        var rig = new CameraRig();
        rig.ZoomTo(2f, 1f);
        // Eased on the factor's ratio: half way through its time it is the square root of where it is going
        Run(rig, 0.5f);
        Assert.Equal(MathF.Sqrt(2f), rig.Zoom, 2);
        Assert.True(rig.Moving);
        Run(rig, 0.6f);
        Assert.Equal(2f, rig.Zoom);
        Assert.False(rig.Moving);

        // Twice as large is half the tangent of half the field of view
        float own = MapScene.OutdoorFovYDeg, zoomed = rig.Fov(own);
        Assert.Equal(MathF.Tan(own * MathF.PI / 360f) / 2f, MathF.Tan(zoomed * MathF.PI / 360f), 5);
        Assert.Equal(MathF.Tan(own * MathF.PI / 360f) * 2f, MathF.Tan(CameraRig.FovAt(own, 0.5f) * MathF.PI / 360f), 5);

        // What it is on its way to is known before it gets there: a wider view is made ready ahead
        rig.ZoomTo(0.5f, 1f);
        Assert.Equal(0.5f, rig.WidestAhead);
        Run(rig, 1.1f);
        rig.ZoomTo(1f, 1f);
        Assert.Equal(0.5f, rig.WidestAhead);

        // The lens stays within its bounds
        rig.ZoomTo(100f, 0f);
        Assert.Equal(CameraRig.TightestZoom, rig.Zoom);
        rig.ZoomTo(0.01f, 0f);
        Assert.Equal(CameraRig.WidestZoom, rig.Zoom);
    }

    [Fact]
    public void UprightThingsStandAsStraightAtEveryZoom()
    {
        // The straightening is worked out from where the camera stands and how steeply it looks down, which a zoom
        // never touches: it is the same whatever the lens
        var target = new Vector3(20f, 0f, 30f);
        float pitch = MapScene.OutdoorPitchDeg * MathF.PI / 180f;
        var from = target + new Vector3(0f, MathF.Sin(pitch), MathF.Cos(pitch)) * MapScene.OutdoorDistance;
        foreach (float zoom in new[] { 0.5f, 1f, 1.5f, 4f })
        {
            var camera = new Camera3D(from, target, Vector3.UnitY, CameraRig.FovAt(MapScene.OutdoorFovYDeg, zoom), CameraProjection.Perspective);
            var plain = new Camera3D(from, target, Vector3.UnitY, MapScene.OutdoorFovYDeg, CameraProjection.Perspective);
            foreach (var (x, y, z) in new[] { (12f, 2f, 26f), (29f, 1.5f, 34f), (20f, 3f, 30f) })
                Assert.Equal(WorldRenderer.Straighten(plain, MapScene.OutdoorPitchDeg, x, y, z), WorldRenderer.Straighten(camera, MapScene.OutdoorPitchDeg, x, y, z));
        }
    }

    [Fact]
    public void WhatIsInViewFollowsTheLens()
    {
        var renderer = new WorldRenderer(new RenderContext(3840, 2160));
        var target = new Vector3(480.5f, 1.2f, 480.5f);
        var sun = Vector3.Normalize(new Vector3(-0.6f, 0.8f, 0.42f));
        var own = renderer.VisibleRects(target, sun).View;

        // A lens zoomed out shows more ground each way, about twice as much across at half the factor
        renderer.Camera.ZoomTo(0.5f, 0f);
        var wide = renderer.VisibleRects(target, sun).View;
        Assert.True(wide.MinX < own.MinX && wide.MaxX > own.MaxX && wide.MinZ < own.MinZ && wide.MaxZ > own.MaxZ);
        Assert.InRange((wide.MaxX - wide.MinX) / (own.MaxX - own.MinX), 1.7f, 2.3f);

        // ... and one zoomed in less
        renderer.Camera.ZoomTo(2f, 0f);
        var tight = renderer.VisibleRects(target, sun).View;
        Assert.True(tight.MinX > own.MinX && tight.MaxX < own.MaxX && tight.MinZ > own.MinZ && tight.MaxZ < own.MaxZ);

        // Released, it is the place's own again, to the last bit
        renderer.Camera.Release(0f);
        renderer.Camera.Advance(1f);
        Assert.Equal(own, renderer.VisibleRects(target, sun).View);
    }

    // ------------------------------------------------------------------ the bars and the pace

    [Fact]
    public void TheBarsAreTheTitlesOwn()
    {
        var rig = new CameraRig();
        rig.Letterbox(true, 0.4f);
        Run(rig, 0.2f);
        Assert.Equal(60f, rig.Bars, 0);
        Assert.True(rig.Moving);
        // In whole units all the way
        for (int i = 0; i < 12; i++)
        {
            rig.Advance(Frame);
            Assert.Equal(MathF.Round(rig.Bars), rig.Bars);
        }
        Assert.Equal(CameraRig.BarHeight, rig.Bars);
        Assert.Equal(120f, CameraRig.BarHeight);
        Assert.False(rig.Moving);
        Assert.False(rig.AtRest);

        rig.Letterbox(false, 0.4f);
        Run(rig, 0.5f);
        Assert.Equal(0f, rig.Bars);
        Assert.True(rig.AtRest);
    }

    [Fact]
    public void SlowMotionSlowsTheFieldForItsTimeAndComesBackByItself()
    {
        var rig = new CameraRig();
        rig.SlowMotion(0.25f, 2f);
        // It comes on over a fifth of a second, never past its share
        Run(rig, 0.1f);
        Assert.InRange(rig.Pace, 0.3f, 0.95f);
        Run(rig, 0.15f);
        Assert.Equal(0.25f, rig.Pace, 3);
        Run(rig, 1.4f);
        Assert.Equal(0.25f, rig.Pace, 3);
        Assert.False(rig.AtRest);
        // ... and has eased back by the time its time is up
        Run(rig, 0.25f);
        Assert.InRange(rig.Pace, 0.3f, 0.95f);
        Run(rig, 0.15f);
        Assert.Equal(1f, rig.Pace);
        Assert.True(rig.AtRest);

        // A share of 1 ends it, eased from wherever it was; a share too small is the slowest the field may run
        rig.SlowMotion(0.01f, 30f);
        Run(rig, 1f);
        Assert.Equal(CameraRig.SlowestPace, rig.Pace, 3);
        rig.SlowMotion(1f, 2f);
        Assert.Equal(CameraRig.SlowestPace, rig.Pace, 3);
        Run(rig, 0.1f);
        Assert.InRange(rig.Pace, 0.2f, 0.9f);
        Run(rig, 0.15f);
        Assert.Equal(1f, rig.Pace);

        // One too short to come on all the way goes as far as it can and back
        rig.SlowMotion(0.5f, 0.2f);
        float slowest = 1f;
        for (int i = 0; i < 20; i++)
        {
            rig.Advance(Frame);
            slowest = MathF.Min(slowest, rig.Pace);
        }
        Assert.InRange(slowest, 0.7f, 0.8f);
        Assert.Equal(1f, rig.Pace);
    }

    [Fact]
    public void ReleaseBringsEverythingBack()
    {
        var rig = new CameraRig();
        rig.Pan(40.5f, 12.5f, 0.2f);
        rig.ZoomTo(1.5f, 0.2f);
        rig.Letterbox(true, 0.2f);
        rig.SlowMotion(0.3f, 30f);
        Run(rig, 0.5f);
        Assert.False(rig.AtRest);

        rig.Release(0.6f);
        var half = Run(rig, 0.3f);
        Near(Vector2.Lerp(new Vector2(40.5f, 12.5f), Player, 0.5f), half, 0.05f);
        Assert.InRange(rig.Zoom, 1.01f, 1.49f);
        Assert.InRange(rig.Bars, 1f, 119f);
        Run(rig, 0.4f);
        Assert.True(rig.AtRest);
        Assert.Equal(Player, rig.Look(Player));
        Assert.Equal(1f, rig.Zoom);
        Assert.Equal(0f, rig.Bars);
        Assert.Equal(1f, rig.Pace);

        // And at once, when a game is left behind
        rig.Follow(new NPC { Name = "Someone", GridX = 3, GridY = 3 }, 0f);
        rig.ZoomTo(3f, 0f);
        rig.Letterbox(true, 0f);
        rig.SlowMotion(0.2f, 5f);
        Run(rig, 0.3f);
        rig.Reset();
        Assert.True(rig.AtRest);
        Assert.Equal(Player, rig.Look(Player));
        Assert.Equal(1f, rig.Pace);
    }

    [Fact]
    public void AHeldLensIsTakenAtOnceAndLetGoToTheBit()
    {
        // Plan 09 · L14: a way into a battle holds a lens over whatever the rig's own zoom is, steps the point looked
        // at, and lets go at the midpoint, after which the camera is what it would have been had nothing been held
        var rig = new CameraRig();
        rig.ZoomTo(1.5f, 0f);
        Run(rig, 0.1f);
        rig.Hold(2f, new Vector2(1.25f, -0.5f), 0.7f);
        Assert.True(rig.Holding);
        Assert.Equal(1.5f, rig.Zoom);
        Assert.Equal(3f, rig.Lens, 4);
        Assert.Equal(CameraRig.FovAt(MapScene.OutdoorFovYDeg, 3f), rig.Fov(MapScene.OutdoorFovYDeg), 4);
        Assert.Equal(new Vector2(1.25f, -0.5f), rig.HeldStep);
        // The step is the renderer's to add to the camera: the point the rig looks at is the player's still
        Assert.Equal(Player, rig.Look(Player));
        // What the widest held lens will show is made ready ahead
        Assert.Equal(1.5f * 0.7f, rig.WidestAhead, 4);
        Assert.False(rig.AtRest);
        Assert.False(rig.Moving);

        // Held within the kit's own bounds
        rig.Hold(10f, Vector2.Zero, 10f);
        Assert.Equal(1.5f * CameraRig.TightestZoom, rig.Lens, 4);

        rig.LetGo();
        Assert.False(rig.Holding);
        Assert.Equal(1.5f, rig.Lens);
        Assert.Equal(Vector2.Zero, rig.HeldStep);

        // A rig that held something and let it go is the rig nobody told anything, to the bit
        var fresh = new CameraRig();
        Run(fresh, 0.2f);
        fresh.Hold(0.8f, new Vector2(0.5f, 0.5f), 0.8f);
        Run(fresh, 0.2f);
        fresh.LetGo();
        Assert.True(fresh.AtRest);
        Assert.Equal(1f, fresh.Lens);
        Assert.Equal(Player, fresh.Look(Player));
        Assert.Equal(MapScene.OutdoorFovYDeg, fresh.Fov(MapScene.OutdoorFovYDeg));
        Assert.Equal(1f, fresh.WidestAhead);

        // And a game left behind lets go too
        fresh.Hold(2f, Vector2.One, 2f);
        fresh.Reset();
        Assert.False(fresh.Holding);
        Assert.True(fresh.AtRest);
    }

    // ------------------------------------------------------------------ the script's words

    [Fact]
    public void TheScriptsWordsAreRead()
    {
        var code = ScriptParser.Parse("test", """
            script Scene
              camera zoom 1.5
              camera zoom 0.5 2 nowait
              camera follow rival
              camera follow player 0.3 nowait
              camera path 31 26 31 23
              camera path 10 12 14 12 14 9 1.5 nowait
              camera path 7 8 2
              camera pan 3 4 nowait
              camera pan 3 4 0.5
              camera release nowait
              letterbox on
              letterbox off 1.2
              slowmo 0.25
              slowmo 0.5 4
              end
            """)[0].Code;
        Assert.Equal((CameraMove.Zoom, 1.5f, CameraRig.ZoomSeconds, false), (code[0].Camera, code[0].Factor, code[0].Seconds, code[0].Option));
        Assert.Equal((CameraMove.Zoom, 0.5f, 2f, true), (code[1].Camera, code[1].Factor, code[1].Seconds, code[1].Option));
        Assert.Equal((CameraMove.Follow, "rival", CameraRig.FollowSeconds, false), (code[2].Camera, code[2].Name, code[2].Seconds, code[2].Option));
        Assert.Equal((CameraMove.Follow, "player", 0.3f, true), (code[3].Camera, code[3].Name, code[3].Seconds, code[3].Option));
        Assert.Equal(new[] { (31, 26), (31, 23) }, code[4].Tiles);
        Assert.Equal((CameraMove.Path, CameraRig.PathPace, false), (code[4].Camera, code[4].Factor, code[4].Option));
        // A number left over after the pairs is the pace
        Assert.Equal(new[] { (10, 12), (14, 12), (14, 9) }, code[5].Tiles);
        Assert.Equal((1.5f, true), (code[5].Factor, code[5].Option));
        Assert.Equal(new[] { (7, 8) }, code[6].Tiles);
        Assert.Equal(2f, code[6].Factor);
        // A pan and a release as they always were, and with the new word
        Assert.Equal((CameraMove.Pan, 3, 4, ScriptParser.PanSeconds, true), (code[7].Camera, code[7].X, code[7].Y, code[7].Seconds, code[7].Option));
        Assert.Equal((0.5f, false), (code[8].Seconds, code[8].Option));
        Assert.Equal((CameraMove.Release, ScriptParser.ReleaseSeconds, true), (code[9].Camera, code[9].Seconds, code[9].Option));
        Assert.Equal((Op.Letterbox, true, CameraRig.BarSeconds), (code[10].Op, code[10].Option, code[10].Seconds));
        Assert.Equal((Op.Letterbox, false, 1.2f), (code[11].Op, code[11].Option, code[11].Seconds));
        Assert.Equal((Op.SlowMotion, 0.25f, CameraRig.SlowSeconds), (code[12].Op, code[12].Factor, code[12].Seconds));
        Assert.Equal((Op.SlowMotion, 0.5f, 4f), (code[13].Op, code[13].Factor, code[13].Seconds));
        // The kit's numbers are the ones a pan always had
        Assert.Equal((ScriptParser.PanSeconds, ScriptParser.ReleaseSeconds), (CameraRig.PanSeconds, CameraRig.ReleaseSeconds));
    }

    [Theory]
    [InlineData("camera tilt 3", "'pan' or 'release'")]
    [InlineData("camera zoom", "a zoom's factor")]
    [InlineData("camera zoom 9", "is not a zoom's factor (0.5 to 4)")]
    [InlineData("camera zoom 0.2", "is not a zoom's factor")]
    [InlineData("camera zoom 2 soon", "is more than the line needs")]
    [InlineData("camera follow", "who")]
    [InlineData("camera follow up", "is a direction where a person belongs")]
    [InlineData("camera path", "at least one tile")]
    [InlineData("camera path 3", "at least one tile")]
    [InlineData("camera path 3 4 99", "is no pace for a path")]
    [InlineData("camera path 3 4.5", "is not a tile")]
    [InlineData("camera path 1 1 2 2 3 3 4 4 5 5 6 6 7 7 8 8 9 9", "8 tiles at the most")]
    [InlineData("letterbox", "'on' or 'off'")]
    [InlineData("letterbox up", "'on' or 'off'")]
    [InlineData("slowmo", "a share of the field's pace")]
    [InlineData("slowmo 2", "is not a share of the field's pace (0.1 to 1)")]
    [InlineData("slowmo 0.5 0", "longer than nothing")]
    public void AWrongLineIsTold(string line, string message)
    {
        var wrong = Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script Scene\n  " + line + "\n  end\n"));
        Assert.Contains(message, wrong.Message);
    }

    private const string Reveal = """
        script Scene
          letterbox on
          camera path 31 26 31 23 1.5 nowait
          say "Everything is ready."
          camera zoom 1.4 2 nowait
          camera follow rival 0.5
          slowmo 0.3 1.5
          camera pan 29 21
          camera follow player
          camera shake
          letterbox off 0.2
          say "At last."
          camera release 1.0
          end
        """;

    [Fact]
    public void ASceneUsesEveryCommand()
    {
        var library = ScriptLibrary.FromSources(("test", Reveal), ("common", ""));
        var map = new Map(40, 40) { Name = "Stage" };
        var rival = new NPC { Name = "rival", GridX = 33, GridY = 24 };
        map.NPCs.Add(rival);
        var host = new HeadlessScriptHost { Map = map };
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"), null);
        runner.RunToEnd();

        Assert.Equal(new[]
        {
            "letterbox on", "camera Path 31 26 31 23 at 1.5", "camera Zoom 1.4", "camera Follow rival", "slowmo 0.3",
            "camera Pan 29 21", "camera Follow player", "camera Shake", "letterbox off", "camera Release"
        }, host.Log.Where(l => l.StartsWith("camera") || l.StartsWith("letterbox") || l.StartsWith("slowmo")));
        Assert.Equal(new[] { "Everything is ready.", "At last." }, host.Transcript.Select(t => t.Text));
        Assert.Empty(host.Problems);

        // A script that may only set things up moves no camera, slides no bars and slows nothing
        foreach (string line in new[] { "camera zoom 2", "camera follow player", "camera path 3 4", "letterbox on", "slowmo 0.5" })
        {
            var one = ScriptLibrary.FromSources(("test", "script Scene\n  " + line + "\n  end\n"), ("common", ""));
            var quiet = new ScriptRunner(one, new HeadlessScriptHost { ShowsNothing = true });
            quiet.Start(one.All.First(s => s.File == "test"), null);
            Assert.Throws<ScriptException>(() => quiet.RunToEnd());
        }

        // Whoever the camera is told to keep on is someone the script names, and is looked for like anyone else
        Assert.Contains("rival", ScriptLibrary.PeopleNamed(library.All.First(s => s.File == "test")).Select(p => p.Who));
        Assert.DoesNotContain("", ScriptLibrary.PeopleNamed(library.All.First(s => s.File == "test")).Select(p => p.Who));
    }

    [Fact]
    public void TheScriptWaitsForACamerasMoveUnlessItsLineSaysNot()
    {
        var library = ScriptLibrary.FromSources(("test", """
            script Scene
              camera zoom 2 1
              letterbox on 5
              slowmo 0.5 5
              camera path 10 10 14 10 2
              camera zoom 1 3 nowait
              camera pan 5 5 2 nowait
              camera release 0.5
              setflag FLAG_SCENE_OVER
              end
            """), ("common", ""));
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"), null);

        int Lines() => host.Log.Count(l => l.StartsWith("camera") || l.StartsWith("letterbox") || l.StartsWith("slowmo"));
        runner.Update(0f);
        // The zoom is waited for, for its own time
        Assert.Equal(1, Lines());
        runner.Update(0.9f);
        Assert.Equal(1, Lines());
        // The bars and slow motion hold nothing up; the path is waited for as long as it takes (four tiles at two a
        // second, and the half second its pace takes to gather)
        runner.Update(0.2f);
        Assert.Equal(4, Lines());
        runner.Update(CameraRig.PathSeconds(4f, 2f) - 0.1f);
        Assert.Equal(4, Lines());
        // ... and the lines that say nowait go by at once, up to the release, which waits again
        runner.Update(0.2f);
        Assert.Equal(7, Lines());
        Assert.False(host.Story.Has("FLAG_SCENE_OVER"));
        runner.Update(0.6f);
        Assert.True(host.Story.Has("FLAG_SCENE_OVER"));
        Assert.False(runner.IsRunning);
    }
}
