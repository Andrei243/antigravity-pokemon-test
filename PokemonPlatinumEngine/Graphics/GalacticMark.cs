using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>What a point of the Galactic mark is part of.</summary>
public enum MarkPart
{
    /// <summary>Outside the lozenge.</summary>
    None,
    /// <summary>One of the six facets.</summary>
    Facet,
    /// <summary>A seam between two facets.</summary>
    Seam,
    /// <summary>The dark core.</summary>
    Core,
    /// <summary>The spark of teal at the core's heart.</summary>
    Heart
}

/// <summary>
/// Team Galactic's mark as this game draws it, which is not the original's (plan 09 · L13, decision 6; style guide,
/// "Life", "The Galactic mark"): a lozenge cut like a stone into six facets round a dark core. This is its one
/// description, for whoever shows it: the facets as shapes in a space of their own (<see cref="Facet"/>, for
/// whoever draws with shapes: the ways into a grunt's and a Commander's battle, <c>SceneTransition</c>), what any
/// point of it is (<see cref="At"/>), and a painter that lays the same shapes down in whole texels
/// (<see cref="Paint"/>: the sign over their buildings' doors, and the grunts' uniforms of plan 11 · C2). No GPU
/// calls.
/// </summary>
public static class GalacticMark
{
    /// <summary>How many facets the lozenge is cut into.</summary>
    public const int Facets = 6;

    /// <summary>
    /// How far the core and its heart reach from the middle, along the lozenge's own axes (the lozenge reaches 1),
    /// and half the width of a seam.
    /// </summary>
    public const float CoreReach = 1f / 3f, HeartReach = 0.12f, SeamHalf = 0.03f;

    /// <summary>The sizes, in texels across, from which the painter draws the facets' shades and seams, and the heart's spark.</summary>
    public const int SeamsFrom = 24, HeartFrom = 48;

    /// <summary>The facets' three flat shades of the yellow band, lit from above: the upper two, the two at the sides, the lower two.</summary>
    public static readonly Color Light = new(248, 228, 128, 255), Base = new(232, 204, 76, 255), Shade = new(196, 164, 48, 255);

    /// <summary>The seams, the rim and the core where the mark lies on nothing darker, and the teal of its heart.</summary>
    public static readonly Color Ink = new(52, 44, 62, 255), Teal = new(120, 230, 220, 255);

    // The lozenge's four points and the middles of its four sides, in its own space: it reaches 1 from its middle
    // along each axis, and y grows downward as on the screen
    private static readonly Vector2 Top = new(0f, -1f), Right = new(1f, 0f), Bottom = new(0f, 1f), Left = new(-1f, 0f);
    private static readonly Vector2 UpperRight = new(0.5f, -0.5f), LowerRight = new(0.5f, 0.5f), LowerLeft = new(-0.5f, 0.5f), UpperLeft = new(-0.5f, -0.5f);

    // The facets, clockwise from the one right of the top point; each starts at the middle and goes round its rim
    private static readonly Vector2[][] Shapes =
    {
        new[] { Vector2.Zero, Top, UpperRight },
        new[] { Vector2.Zero, UpperRight, Right, LowerRight },
        new[] { Vector2.Zero, LowerRight, Bottom },
        new[] { Vector2.Zero, Bottom, LowerLeft },
        new[] { Vector2.Zero, LowerLeft, Left, UpperLeft },
        new[] { Vector2.Zero, UpperLeft, Top }
    };

    /// <summary>
    /// A facet's corners in the mark's own space, its middle first and then round its rim: three for the four small
    /// facets, four for the two at the sides. The facets go clockwise from the one to the right of the top point.
    /// </summary>
    public static ReadOnlySpan<Vector2> Facet(int facet) => Shapes[facet];

    /// <summary>The middle of a facet: the mean of its corners.</summary>
    public static Vector2 MiddleOf(int facet)
    {
        var sum = Vector2.Zero;
        foreach (var p in Shapes[facet]) sum += p;
        return sum / Shapes[facet].Length;
    }

    /// <summary>A facet's shade: the upper two are lit, the lower two in shade.</summary>
    public static Color ShadeOf(int facet) => facet is 0 or 5 ? Light : facet is 1 or 4 ? Base : Shade;

    // Where the six seams end: they run from the middle to the top and bottom points and to the middle of each side
    private static readonly Vector2[] SeamEnds = { Top, UpperRight, LowerRight, Bottom, LowerLeft, UpperLeft };

    /// <summary>
    /// What the point (<paramref name="u"/>, <paramref name="v"/>) of the mark's own space is part of, and which
    /// facet when it is one (or the nearer of the two a seam runs between). <paramref name="seamHalf"/> is half a
    /// seam's width, for a painter whose texels are wider than the seam.
    /// </summary>
    public static MarkPart At(float u, float v, out int facet, float seamHalf = SeamHalf)
    {
        facet = -1;
        float reach = MathF.Abs(u) + MathF.Abs(v);
        if (reach > 1f) return MarkPart.None;

        // The seams stand a quarter turn from one another, but for the two that end on a point, which stand between
        float degrees = MathF.Atan2(v, u) * 180f / MathF.PI;
        facet = degrees switch
        {
            >= -90f and < -45f => 0,
            >= -45f and < 45f => 1,
            >= 45f and < 90f => 2,
            >= 90f and < 135f => 3,
            >= 135f or < -135f => 4,
            _ => 5
        };
        if (reach < HeartReach) return MarkPart.Heart;
        if (reach < CoreReach) return MarkPart.Core;

        var p = new Vector2(u, v);
        foreach (var end in SeamEnds)
        {
            // How far the point lies from the seam: the stretch from the middle to its end
            float along = Math.Clamp(Vector2.Dot(p, end) / end.LengthSquared(), 0f, 1f);
            if ((p - end * along).Length() < seamHalf) return MarkPart.Seam;
        }
        return MarkPart.Facet;
    }

    /// <summary>
    /// Paints the mark <paramref name="size"/> texels across and tall with its top point's row at
    /// <paramref name="top"/> and its middle column at <paramref name="cx"/>. Its outline is the same at every
    /// size: a row of the lozenge as wide as it is far from the nearer point. Under <see cref="SeamsFrom"/> texels
    /// it is the flat lozenge with a core four texels square (the sign over a door); from there on the facets take
    /// their shades with the seams and the rim between them, and from <see cref="HeartFrom"/> the core has its
    /// spark. <paramref name="ink"/> and <paramref name="yellow"/> take the place of the mark's own where it must
    /// sit in someone else's palette: the three shades are then made from the yellow given.
    /// </summary>
    public static void Paint(PixelCanvas c, int cx, int top, int size, Color? ink = null, Color? yellow = null)
    {
        var dark = ink ?? Ink;
        var mid = yellow ?? Base;
        var lit = yellow is { } y1 ? PixelCanvas.Light1(y1, 0.4f) : Light;
        var low = yellow is { } y2 ? PixelCanvas.Shadow(y2, 0.3f) : Shade;
        int half = size / 2;
        bool cut = size >= SeamsFrom;
        // A seam is never thinner than a texel
        float seam = MathF.Max(SeamHalf, 0.55f / half);
        for (int py = 0; py < size; py++)
        {
            int span = half - Math.Abs(py - half);
            int x0 = cx - span, w = Math.Max(1, span * 2);
            if (!cut)
            {
                c.Rect(x0, top + py, w, 1, mid);
                continue;
            }
            for (int px = x0; px < x0 + w; px++)
            {
                // The rim: the first and the last texel of every row, and the rows of the two points
                if (px == x0 || px == x0 + w - 1 || span <= 1)
                {
                    c.Set(px, top + py, dark);
                    continue;
                }
                // (The widest row is the lozenge's middle: its texels' middles lie on the axis)
                float u = (px + 0.5f - cx) / half, v = (py - half) / (float)half;
                var part = At(u, v, out int facet, seam);
                c.Set(px, top + py, part switch
                {
                    MarkPart.Heart => size >= HeartFrom ? Teal : dark,
                    MarkPart.Core or MarkPart.Seam or MarkPart.None => dark,
                    _ => facet is 0 or 5 ? lit : facet is 1 or 4 ? mid : low
                });
            }
        }
        if (!cut) c.Rect(cx - 2, top + half - 2, 4, 4, dark);
    }
}
