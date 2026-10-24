using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// What is mirrored where (plan 09 · L10; style guide, "Water", "Puddles" and "Quality presets"), with no GPU calls:
/// the plane of the water in view and whether the mirror pass runs at all, the mirrored camera and the numbers that
/// straighten what it draws, and which tiles mirror the walker with which tint. <see cref="WorldRenderer"/> asks
/// here and draws; the tests ask here too.
/// </summary>
internal static class Reflections
{
    /// <summary>Water of fewer tiles than this in view (a ditch, a corner of a pond at the picture's edge) mirrors nothing, and the pass is not run.</summary>
    public const int FewestWaterTiles = 7;

    /// <summary>How far under the water's plane something may reach and still be drawn in the mirror: what lies lower is cut off.</summary>
    public const float Clearance = 0.02f;

    /// <summary>How far from the plane a water surface may lie and still take the reflection (a beach's sloping shallows).</summary>
    public const float OnThePlane = 0.05f;

    /// <summary>
    /// How far north of the view what stands is drawn in the mirror: a tall tree whose foot is just north of the
    /// picture's top edge throws its reflection into it.
    /// </summary>
    public const float Tallest = 6f;

    /// <summary>The mirror's target: 1920×1080 whatever the preset (half the virtual screen), read once per field texel.</summary>
    public const int TargetWidth = 1920, TargetHeight = 1080;

    // ------------------------------------------------------------------ the walker's mirror (puddles and polished floors)

    /// <summary>A walker's reflection's colour and how strong it is at the feet (0 to 255; it fades to nothing).</summary>
    public readonly record struct Tint(byte R, byte G, byte B, byte Near);

    /// <summary>A puddle's (plan 01 · M7): the sky's blue, from 45 % at the feet.</summary>
    public static readonly Tint PuddleTint = new(150, 170, 196, 115);

    /// <summary>A polished floor's (plan 09 · L10): pale on the dark stone, from 35 % at the feet.</summary>
    public static readonly Tint FloorTint = new(176, 182, 222, 90);

    /// <summary>Whether a tile mirrors whoever stands on it: a puddle, and a polished floor.</summary>
    public static bool MirrorsWalker(TileBehavior behaviour) =>
        behaviour is TileBehavior.Puddle or TileBehavior.StillPuddle or TileBehavior.ShinyFloor;

    /// <summary>The tint of the reflection of whoever stands on a tile that mirrors (the tile under their feet decides).</summary>
    public static Tint TintOf(TileBehavior behaviour) => behaviour == TileBehavior.ShinyFloor ? FloorTint : PuddleTint;

    /// <summary>
    /// How far south of the feet a walker on a tile that mirrors is mirrored: to the end of the run of mirroring
    /// tiles, at most three tiles past their own (null where the tile under them doesn't mirror).
    /// </summary>
    public static float? WalkerReach(Map map, Vector3 feet)
    {
        int tx = (int)MathF.Floor(feet.X), tz = (int)MathF.Floor(feet.Z);
        if (!map.InBounds(tx, tz) || !MirrorsWalker(map.BehaviourAt(tx, tz))) return null;
        int end = tz + 1;
        while (end < tz + 4 && map.InBounds(tx, end) && MirrorsWalker(map.BehaviourAt(tx, end))) end++;
        return end - feet.Z;
    }

    // ------------------------------------------------------------------ the water's mirror

    /// <summary>
    /// The drawn height of the water in a rectangle of ground: the height most of its open water lies at, ties going
    /// to the water nearest <paramref name="focus"/>; null where there are fewer than <see cref="FewestWaterTiles"/>
    /// tiles of it. Waterfalls are not counted: they are a sheet, not a surface.
    /// </summary>
    public static float? PlaneIn(Map map, GroundRect view, Vector2 focus)
    {
        int x0 = Math.Max(0, (int)MathF.Floor(view.MinX)), z0 = Math.Max(0, (int)MathF.Floor(view.MinZ));
        int x1 = Math.Min(map.Width - 1, (int)MathF.Floor(view.MaxX)), z1 = Math.Min(map.Height - 1, (int)MathF.Floor(view.MaxZ));
        // A handful of heights at most in one view: kept in a few slots, so nothing is made each frame
        Span<int> keys = stackalloc int[8];
        Span<int> counts = stackalloc int[8];
        Span<float> nearest = stackalloc float[8];
        int used = 0, total = 0;
        for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                if (!GroundBaker.IsWaterAt(map, x, z) || map.BehaviourAt(x, z) == TileBehavior.Waterfall) continue;
                float h = Relief.At(map, x + 0.5f, z + 0.5f);
                int key = (int)MathF.Round(h * 8f);
                float d = Vector2.DistanceSquared(new Vector2(x + 0.5f, z + 0.5f), focus);
                int slot = -1;
                for (int i = 0; i < used; i++)
                    if (keys[i] == key) { slot = i; break; }
                if (slot < 0)
                {
                    if (used == keys.Length) continue;
                    slot = used++;
                    keys[slot] = key;
                    counts[slot] = 0;
                    nearest[slot] = float.MaxValue;
                }
                counts[slot]++;
                nearest[slot] = MathF.Min(nearest[slot], d);
                total++;
            }
        if (total < FewestWaterTiles) return null;
        int best = 0;
        for (int i = 1; i < used; i++)
            if (counts[i] > counts[best] || (counts[i] == counts[best] && nearest[i] < nearest[best])) best = i;
        return keys[best] / 8f;
    }

    /// <summary>
    /// Whether the mirror pass runs for a map: on a preset that reflects, outside rooms and the sea floor (which have no
    /// water to mirror in), with water in view.
    /// </summary>
    public static bool Wanted(QualityProfile quality, Map map, float? plane) =>
        quality.Reflections && plane.HasValue && !map.IsIndoors && !map.IsUnderwater;

    /// <summary>A point mirrored about the horizontal plane at a height.</summary>
    public static Vector3 Reflect(Vector3 point, float plane) => point with { Y = 2f * plane - point.Y };

    /// <summary>
    /// The camera mirrored about the water's plane: its place and what it looks at reflected, its up still up, its lens
    /// the same. It sees the world from under the water; a point of the plane is where the field's camera sees it,
    /// upside down in the target, which the water reads by projecting its own texel through this camera.
    /// </summary>
    public static Camera3D Mirror(Camera3D camera, float plane) =>
        new(Reflect(camera.Position, plane), Reflect(camera.Target, plane), Vector3.UnitY, camera.FovY, camera.Projection);

    /// <summary>
    /// The numbers that straighten upright things in the mirror as the field's camera straightens them (CLAUDE.md,
    /// "Rendering rules that have caused bugs": x divided by the depth of the ground under each vertex). The scenery's
    /// vertex shader takes that depth as <c>w + (y · pitchSin + uprightBase) · upright</c>; seen from the mirrored
    /// camera a point at height y is where the field's camera sees one at 2·plane − y, so the mirror pass gives the
    /// pitch's sine turned round and a base of twice the plane's height times it.
    /// </summary>
    public static (float PitchSin, float Base) Straightening(float pitchDeg, float plane)
    {
        float sin = MathF.Sin(pitchDeg * MathF.PI / 180f);
        return (-sin, 2f * plane * sin);
    }

    /// <summary>The ground whose standing things the mirror draws: the view, stretched north by <see cref="Tallest"/>.</summary>
    public static GroundRect Reach(GroundRect view) => view with { MinZ = view.MinZ - Tallest };
}
