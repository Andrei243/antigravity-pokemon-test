using System;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 09 · L10, reflections (style guide, "Water", "Puddles", "Quality presets"): what is mirrored where, worked out
/// without a GPU (<see cref="Reflections"/>).
/// </summary>
public class ReflectionTests
{
    private static Map Ground(int w = 40, int h = 30)
    {
        var map = new Map(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                map.SetGroundTile(x, y, TileType.Sand);
        return map;
    }

    private static Map WithPond(int x0, int y0, int w, int h)
    {
        var map = Ground();
        for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
                map.SetGroundTile(x, y, TileType.Water);
        return map;
    }

    private static readonly GroundRect WholeView = new(0, 0, 39, 29);

    // The field's camera: pitched down at 59.05°, as Platinum's
    private static Camera3D FieldCamera()
    {
        float pitch = 59.05f * MathF.PI / 180f;
        var target = new Vector3(12.5f, 0.6f, 20f);
        var position = target + new Vector3(0, MathF.Sin(pitch), MathF.Cos(pitch)) * 40f;
        return new Camera3D(position, target, Vector3.UnitY, 16.18f, CameraProjection.Perspective);
    }

    private static Matrix4x4 ViewOf(Camera3D c) => Matrix4x4.CreateLookAt(c.Position, c.Target, c.Up);

    [Fact]
    public void TheMirroredViewIsTheViewReflectedAboutTheWaterPlane()
    {
        var camera = FieldCamera();
        const float plane = 0.5f;
        var mirror = Reflections.Mirror(camera, plane);

        // Its place and what it looks at are reflected; it looks through the same lens with its up still up
        Assert.Equal(camera.Position.X, mirror.Position.X, 5);
        Assert.Equal(camera.Position.Z, mirror.Position.Z, 5);
        Assert.Equal(2f * plane - camera.Position.Y, mirror.Position.Y, 4);
        Assert.Equal(2f * plane - camera.Target.Y, mirror.Target.Y, 4);
        Assert.Equal(camera.FovY, mirror.FovY);
        Assert.Equal(Vector3.UnitY, mirror.Up);
        Assert.True(mirror.Position.Y < plane, "the mirrored camera looks up from under the water");

        // Every point is seen by the mirrored camera where the field's camera sees its reflection: at the same
        // distance along the view, as far across, and upside down
        var real = ViewOf(camera);
        var mirrored = ViewOf(mirror);
        foreach (var p in new[] { new Vector3(10f, 0.5f, 18f), new Vector3(14f, 2.3f, 15f), new Vector3(9f, 4f, 22f) })
        {
            var a = Vector3.Transform(Reflections.Reflect(p, plane), real);
            var b = Vector3.Transform(p, mirrored);
            Assert.Equal(a.Z, b.Z, 3);
            Assert.Equal(a.X, b.X, 3);
            Assert.Equal(-a.Y, b.Y, 3);
        }
    }

    [Fact]
    public void ThePlanesOwnPointsLieWhereTheFieldsCameraSeesThem()
    {
        var camera = FieldCamera();
        const float plane = 0f;
        var mirror = Reflections.Mirror(camera, plane);
        var p = new Vector3(11.25f, plane, 17.5f);
        var a = Vector3.Transform(p, ViewOf(camera));
        var b = Vector3.Transform(p, ViewOf(mirror));
        Assert.Equal(a.Z, b.Z, 3);
        Assert.Equal(a.X, b.X, 3);
        Assert.Equal(-a.Y, b.Y, 3);
    }

    [Fact]
    public void TheMirrorStraightensAsTheFieldStraightensTheReflection()
    {
        // The scenery's vertex shader divides x by w + (y · pitchSin + uprightBase) · upright: in the mirror pass that
        // must be what the field's camera uses for the reflected point, w + (2·plane − y) · sin · upright
        const float pitchDeg = 59.05f, plane = 1f, upright = 1f;
        float sin = MathF.Sin(pitchDeg * MathF.PI / 180f);
        var (mirroredSin, b) = Reflections.Straightening(pitchDeg, plane);
        foreach (float y in new[] { 1f, 1.7f, 3.2f, 5f })
        {
            float w = 37.5f;
            float field = w + (2f * plane - y) * sin * upright;
            float mirror = w + (y * mirroredSin + b) * upright;
            Assert.Equal(field, mirror, 4);
        }
        // Nought where the plane is at nought and nothing stands: the base alone is twice the plane's height
        Assert.Equal(0f, Reflections.Straightening(pitchDeg, 0f).Base);
    }

    [Fact]
    public void ThePassDrawsNothingWhenNoWaterIsInView()
    {
        var map = Ground();
        Assert.Null(Reflections.PlaneIn(map, WholeView, new Vector2(20, 15)));
        Assert.False(Reflections.Wanted(QualityProfile.For(GraphicsQuality.High), map, null));

        // Water out of view is not in view
        var pond = WithPond(30, 2, 6, 4);
        Assert.Null(Reflections.PlaneIn(pond, new GroundRect(0, 10, 20, 29), new Vector2(10, 20)));

        // A ditch of a few tiles mirrors nothing either
        var ditch = WithPond(10, 10, Reflections.FewestWaterTiles - 1, 1);
        Assert.Null(Reflections.PlaneIn(ditch, WholeView, new Vector2(20, 15)));
    }

    [Fact]
    public void WaterInViewGivesTheMirrorItsPlane()
    {
        var pond = WithPond(10, 10, 6, 5);
        var plane = Reflections.PlaneIn(pond, WholeView, new Vector2(20, 15));
        Assert.Equal(0f, plane);
        Assert.True(Reflections.Wanted(QualityProfile.For(GraphicsQuality.High), pond, plane));
        Assert.True(Reflections.Wanted(QualityProfile.For(GraphicsQuality.Medium), pond, plane));
    }

    [Fact]
    public void LowDrawsNone()
    {
        var pond = WithPond(10, 10, 6, 5);
        var plane = Reflections.PlaneIn(pond, WholeView, new Vector2(20, 15));
        Assert.False(QualityProfile.For(GraphicsQuality.Low).Reflections);
        Assert.True(QualityProfile.For(GraphicsQuality.Medium).Reflections);
        Assert.True(QualityProfile.For(GraphicsQuality.High).Reflections);
        Assert.False(Reflections.Wanted(QualityProfile.For(GraphicsQuality.Low), pond, plane));
    }

    [Fact]
    public void RoomsMirrorNothingInTheWaterPass()
    {
        var pond = WithPond(10, 10, 6, 5);
        pond.Interior = InteriorStyle.House;
        Assert.False(Reflections.Wanted(QualityProfile.For(GraphicsQuality.High), pond, 0f));
    }

    [Fact]
    public void TheMirrorReachesNorthOfTheViewForWhatStandsTallThere()
    {
        var view = new GroundRect(2, 10, 30, 25);
        var reach = Reflections.Reach(view);
        Assert.Equal(view.MinZ - Reflections.Tallest, reach.MinZ);
        Assert.Equal(view.MaxZ, reach.MaxZ);
        Assert.Equal(view.MinX, reach.MinX);
        Assert.Equal(view.MaxX, reach.MaxX);
    }

    [Fact]
    public void AWalkerOnAPolishedFloorIsMirroredAsOnAPuddleWithTheFloorsTint()
    {
        var map = Ground();
        for (int y = 5; y < 9; y++) map.SetBehaviour(10, y, TileBehavior.ShinyFloor);
        map.SetBehaviour(20, 5, TileBehavior.Puddle);
        map.SetBehaviour(20, 6, TileBehavior.Puddle);

        Assert.True(Reflections.MirrorsWalker(TileBehavior.ShinyFloor));
        Assert.True(Reflections.MirrorsWalker(TileBehavior.Puddle));
        Assert.True(Reflections.MirrorsWalker(TileBehavior.StillPuddle));
        Assert.False(Reflections.MirrorsWalker(TileBehavior.None));

        // Mirrored as far south as the floor runs, at most three tiles past the feet's own, as on a puddle
        var onFloor = new Vector3(10.5f, 0f, 5.5f);
        Assert.Equal(3.5f, Reflections.WalkerReach(map, onFloor));
        var inPuddle = new Vector3(20.5f, 0f, 5.5f);
        Assert.Equal(1.5f, Reflections.WalkerReach(map, inPuddle));
        Assert.Null(Reflections.WalkerReach(map, new Vector3(15.5f, 0f, 5.5f)));

        // In the floor's own tint, not the puddle's
        Assert.Equal(Reflections.FloorTint, Reflections.TintOf(TileBehavior.ShinyFloor));
        Assert.Equal(Reflections.PuddleTint, Reflections.TintOf(TileBehavior.Puddle));
        Assert.NotEqual(Reflections.PuddleTint, Reflections.FloorTint);
        // The puddle's stays what plan 01 · M7 gave it (style guide, "Puddles")
        Assert.Equal(new Reflections.Tint(150, 170, 196, 115), Reflections.PuddleTint);
        Assert.Equal(new Reflections.Tint(176, 182, 222, 90), Reflections.FloorTint);
    }
}
