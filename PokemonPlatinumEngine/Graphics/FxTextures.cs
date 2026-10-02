using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>The shapes in the effects atlas (<see cref="FxTextures"/>), in atlas order: eight to a row.</summary>
internal enum FxShape
{
    Glow, Spark, Star, Ring, Flame, Drop, Bubble, Leaf,
    Shard, Rock, Smoke, Heart, Note, Streak, Wisp, Petal,
    Snowflake, Arrow, Feather, Orb, Claw, Fang, Impact, Zzz,
    Swirl, Beam, Dot, Sword, Wave, Ember, Cross, Spore
}

/// <summary>
/// The battle effects' shapes (plan 04 · G8), painted in code into one smooth, mipmapped atlas: soft glows, sparks
/// and rings for light; flames, drops, leaves, shards, rocks and feathers for matter; hearts, notes, stars and
/// letters for the odd status move. Shapes are white or grey (shading as brightness), so each sprite takes its
/// colour from the move's type. Everything but the upload is GPU-free.
/// </summary>
internal static class FxTextures
{
    public const int Columns = 8, Rows = 4, Cell = 128;

    // Shapes are drawn in the middle of their cell with this much space round them, so mipmaps don't bleed
    private const float Inner = 56f;

    private static Texture2D? atlas;

    public static Texture2D Atlas => atlas ??= Bake().ToTexture(repeat: false);

    /// <summary>The square of the atlas a shape occupies, in texture coordinates (v down).</summary>
    public static (Vector2 Min, Vector2 Max) UV(FxShape shape)
    {
        int i = (int)shape, col = i % Columns, row = i / Columns;
        float w = 1f / Columns, h = 1f / Rows;
        // Half a texel inside the cell
        float iu = 0.5f / (Columns * Cell), iv = 0.5f / (Rows * Cell);
        return (new Vector2(col * w + iu, row * h + iv), new Vector2((col + 1) * w - iu, (row + 1) * h - iv));
    }

    /// <summary>Paints every shape; alpha is coverage, colour is white shaded by the shape's own light.</summary>
    public static SoftCanvas Bake()
    {
        var canvas = new SoftCanvas(Columns * Cell, Rows * Cell);
        foreach (FxShape shape in Enum.GetValues<FxShape>())
        {
            int i = (int)shape, ox = i % Columns * Cell, oy = i / Columns * Cell;
            for (int y = 0; y < Cell; y++)
                for (int x = 0; x < Cell; x++)
                {
                    // Cell coordinates: -1..1 across the inner square, y up
                    var p = new Vector2((x + 0.5f - Cell / 2f) / Inner, -(y + 0.5f - Cell / 2f) / Inner);
                    var (a, l) = Shape(shape, p);
                    a = Math.Clamp(a, 0f, 1f);
                    l = Math.Clamp(l, 0f, 1f);
                    // Transparent texels keep the colour of the shape, so filtering doesn't darken its rim
                    canvas.SetPixel(ox + x, oy + y, new Vector4(l, l, l, a));
                }
        }
        return canvas;
    }

    /// <summary>Coverage of a shape at one point of its cell (for tests and the bake).</summary>
    public static float Coverage(FxShape shape, Vector2 p) => Math.Clamp(Shape(shape, p).A, 0f, 1f);

    // ------------------------------------------------------------------ shapes

    private const float Px = 1f / Inner;

    /// <summary>Anti-aliased fill of a signed distance (negative inside), in cell units.</summary>
    private static float Fill(float d, float soft = 1.2f) => Math.Clamp(0.5f - d / (Px * soft), 0f, 1f);

    private static float Sat(float x) => Math.Clamp(x, 0f, 1f);

    private static float Circle(Vector2 p, Vector2 c, float r) => (p - c).Length() - r;

    private static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        var pa = p - a;
        var ba = b - a;
        float h = Sat(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).Length() - r;
    }

    /// <summary>A pointed lens (a leaf, a petal, a feather): two arcs meeting at (0, ±length).</summary>
    private static float Lens(Vector2 p, float length, float width)
    {
        // Circles of radius r through both tips, their centres off to each side
        float r = (length * length + width * width) / (2f * width);
        float off = r - width;
        return MathF.Max(Circle(p, new Vector2(-off, 0), r), Circle(p, new Vector2(off, 0), r));
    }

    /// <summary>A star with <paramref name="points"/> points between the two radii (distance along the radius, a good approximation).</summary>
    private static float StarDist(Vector2 p, int points, float outer, float inner, float turn = 0f)
    {
        float a = MathF.Atan2(p.X, p.Y) + turn;
        float sector = MathF.Tau / points;
        float f = MathF.Abs(((a % sector) + sector) % sector / sector * 2f - 1f); // 1 at a point, 0 between
        float edge = inner + (outer - inner) * f * f;
        return (p.Length() - edge) * 0.8f;
    }

    private static float Polygon(Vector2 p, ReadOnlySpan<Vector2> v)
    {
        float d = Vector2.DistanceSquared(p, v[0]);
        float s = 1f;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
        {
            var e = v[j] - v[i];
            var w = p - v[i];
            var b = w - e * Sat(Vector2.Dot(w, e) / Vector2.Dot(e, e));
            d = MathF.Min(d, Vector2.Dot(b, b));
            bool c1 = p.Y >= v[i].Y, c2 = p.Y < v[j].Y, c3 = e.X * w.Y > e.Y * w.X;
            if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
        }
        return s * MathF.Sqrt(d);
    }

    /// <summary>A flame or a drop: round at the bottom, drawn up to a point.</summary>
    private static float Teardrop(Vector2 p, float radius, float tip)
    {
        // Below the widest point a circle; above it the sides close in toward the tip
        float y = p.Y + 0.25f;
        if (y <= 0f) return new Vector2(p.X, y).Length() - radius;
        float t = Sat(y / (tip + 0.25f));
        float half = radius * MathF.Sqrt(MathF.Max(0f, 1f - t)) * (1f - 0.15f * t);
        return MathF.Max(MathF.Abs(p.X) - half, y - (tip + 0.25f)) * 0.9f;
    }

    private static (float A, float L) Shape(FxShape shape, Vector2 p)
    {
        float r = p.Length();
        switch (shape)
        {
            case FxShape.Glow:
            {
                float a = Sat(1f - r);
                return (a * a, 1f);
            }
            case FxShape.Spark:
            {
                // A bright point with four long rays and four short ones
                float core = MathF.Exp(-r * r * 26f);
                float rays = MathF.Exp(-p.Y * p.Y * 700f) * Sat(1f - MathF.Abs(p.X)) + MathF.Exp(-p.X * p.X * 700f) * Sat(1f - MathF.Abs(p.Y));
                var q = new Vector2(p.X + p.Y, p.X - p.Y) * 0.7071f;
                float diag = MathF.Exp(-q.Y * q.Y * 1200f) * Sat(1f - MathF.Abs(q.X) * 1.7f) + MathF.Exp(-q.X * q.X * 1200f) * Sat(1f - MathF.Abs(q.Y) * 1.7f);
                return (core + rays * 0.9f + diag * 0.55f, 1f);
            }
            case FxShape.Star:
            {
                float d = StarDist(p, 5, 0.95f, 0.42f);
                // Lit from the upper left, with a pale bevel
                return (Fill(d), 0.82f + 0.18f * Sat(0.5f - (p.X - p.Y) * 0.6f));
            }
            case FxShape.Ring:
            {
                float d = MathF.Abs(r - 0.82f);
                return (Sat(1f - d / 0.14f) * Sat(1f - d / 0.14f), 1f);
            }
            case FxShape.Flame:
            {
                // A flame licking upward: brightest low in its body, soft at its edges
                var q = new Vector2(p.X + 0.12f * MathF.Sin(p.Y * 3.2f + 0.5f) * Sat(p.Y + 0.3f), p.Y);
                float d = Teardrop(q, 0.5f, 0.85f);
                float body = Sat(-d / 0.35f);
                return (body * (0.75f + 0.25f * Sat(0.6f - p.Y)), 1f);
            }
            case FxShape.Drop:
            {
                float d = Teardrop(p, 0.55f, 0.75f);
                // Darker toward the lower right, a white glint upper left
                float l = 0.72f + 0.2f * Sat(0.4f - p.X * 0.5f + p.Y * 0.3f);
                float glint = Fill(Circle(p, new Vector2(-0.2f, 0.05f), 0.13f), 2f);
                return (Fill(d), MathF.Max(l, glint));
            }
            case FxShape.Bubble:
            {
                float rim = Sat(1f - MathF.Abs(r - 0.82f) / 0.12f);
                float inside = r < 0.82f ? 0.22f : 0f;
                float glint = Fill(Circle(p, new Vector2(-0.34f, 0.36f), 0.17f), 2f);
                return (MathF.Max(rim, MathF.Max(inside, glint)), 1f);
            }
            case FxShape.Leaf:
            {
                float d = Lens(p, 0.95f, 0.42f);
                float vein = Sat(1f - MathF.Abs(p.X) / 0.035f) * Sat(0.9f - MathF.Abs(p.Y));
                float l = 0.8f + 0.2f * Sat(0.5f - p.X) - 0.25f * vein;
                return (Fill(d), l);
            }
            case FxShape.Shard:
            {
                // A long crystal with two faces: one lit, one in shade
                Span<Vector2> v = stackalloc Vector2[] { new(0, 0.98f), new(0.3f, 0.1f), new(0.12f, -0.95f), new(-0.2f, -0.6f), new(-0.32f, 0.2f) };
                float d = Polygon(p, v);
                return (Fill(d), p.X < 0.02f - p.Y * 0.08f ? 1f : 0.72f);
            }
            case FxShape.Rock:
            {
                Span<Vector2> v = stackalloc Vector2[] { new(-0.35f, 0.82f), new(0.42f, 0.74f), new(0.86f, 0.15f), new(0.62f, -0.66f), new(-0.1f, -0.86f), new(-0.78f, -0.42f), new(-0.86f, 0.3f) };
                float d = Polygon(p, v);
                // A lit top face and a shaded flank
                float l = p.Y > 0.25f - p.X * 0.3f ? 0.95f : p.X > 0.1f ? 0.55f : 0.72f;
                return (Fill(d), l);
            }
            case FxShape.Smoke:
            {
                float d = MathF.Min(Circle(p, new(-0.3f, -0.1f), 0.48f), MathF.Min(Circle(p, new(0.28f, -0.15f), 0.45f), Circle(p, new(0.02f, 0.3f), 0.5f)));
                float a = Sat(-d / 0.4f + 0.15f);
                return (a * 0.85f, 0.85f + 0.15f * Sat(p.Y + 0.5f));
            }
            case FxShape.Heart:
            {
                var q = new Vector2(MathF.Abs(p.X), p.Y + 0.15f);
                float d = MathF.Min(Circle(q, new Vector2(0.38f, 0.28f), 0.42f), Polygon(q, stackalloc Vector2[] { new(0f, -0.85f), new(0.72f, 0.05f), new(0f, 0.45f) }));
                return (Fill(d), 0.85f + 0.15f * Sat(p.Y + 0.2f - p.X * 0.4f));
            }
            case FxShape.Note:
            {
                float head = Circle(new Vector2((p.X + 0.22f) * 0.8f, p.Y + 0.55f), Vector2.Zero, 0.28f);
                float stem = Capsule(p, new Vector2(0.08f, -0.5f), new Vector2(0.08f, 0.75f), 0.07f);
                float flag = Capsule(p, new Vector2(0.08f, 0.75f), new Vector2(0.48f, 0.4f), 0.08f);
                return (Fill(MathF.Min(head, MathF.Min(stem, flag))), 1f);
            }
            case FxShape.Streak:
            {
                // A soft horizontal line, brightest in the middle
                float a = MathF.Exp(-p.Y * p.Y * 60f) * Sat(1f - p.X * p.X);
                return (a, 1f);
            }
            case FxShape.Wisp:
            {
                // A ghostly flame bent into a curl at the top
                var q = new Vector2(p.X + 0.3f * MathF.Sin(p.Y * 2.6f) * Sat(p.Y + 0.4f), p.Y);
                float d = Teardrop(q, 0.42f, 0.9f);
                return (Sat(-d / 0.3f) * 0.95f, 1f);
            }
            case FxShape.Petal:
            {
                float d = MathF.Max(Lens(p, 0.9f, 0.5f), -Circle(p, new Vector2(0, 1.02f), 0.22f));
                return (Fill(d), 0.85f + 0.15f * Sat(0.5f - r));
            }
            case FxShape.Snowflake:
            {
                float d = 1f;
                for (int i = 0; i < 6; i++)
                {
                    float a = i * MathF.PI / 3f;
                    var dir = new Vector2(MathF.Sin(a), MathF.Cos(a));
                    var side = new Vector2(dir.Y, -dir.X);
                    d = MathF.Min(d, Capsule(p, Vector2.Zero, dir * 0.85f, 0.07f));
                    d = MathF.Min(d, Capsule(p, dir * 0.5f, dir * 0.72f + side * 0.2f, 0.05f));
                    d = MathF.Min(d, Capsule(p, dir * 0.5f, dir * 0.72f - side * 0.2f, 0.05f));
                }
                return (Fill(d), 1f);
            }
            case FxShape.Arrow:
            {
                Span<Vector2> v = stackalloc Vector2[] { new(0, 0.95f), new(0.7f, 0.2f), new(0.26f, 0.2f), new(0.26f, -0.9f), new(-0.26f, -0.9f), new(-0.26f, 0.2f), new(-0.7f, 0.2f) };
                return (Fill(Polygon(p, v)), 0.8f + 0.2f * Sat(p.Y + 0.5f));
            }
            case FxShape.Feather:
            {
                var q = new Vector2(p.X - 0.08f * p.Y * p.Y, p.Y);
                float vane = Lens(q, 0.95f, 0.36f);
                // Notches along the vane, and the quill down the middle
                float notch = MathF.Abs(MathF.Sin((q.Y - q.X * 0.6f) * 11f)) < 0.12f && MathF.Abs(q.X) > 0.12f ? 0.04f : 0f;
                float quill = Sat(1f - MathF.Abs(q.X) / 0.03f);
                return (Fill(vane + notch), 0.9f - 0.2f * quill);
            }
            case FxShape.Orb:
            {
                // A glowing sphere: bright rim and centre, a glint upper left
                float body = Sat(1f - r / 0.75f);
                float rim = Sat(1f - MathF.Abs(r - 0.68f) / 0.1f);
                float halo = Sat(1f - r) * 0.4f;
                return (MathF.Max(MathF.Max(body * 0.85f, rim), halo), 1f);
            }
            case FxShape.Claw:
            {
                // Three curved slash marks
                float d = 1f;
                for (int i = -1; i <= 1; i++)
                {
                    float off = i * 0.36f;
                    var q = new Vector2(p.X - off, p.Y);
                    float bend = 0.18f * q.Y * q.Y;
                    d = MathF.Min(d, Capsule(new Vector2(q.X + bend - 0.1f, q.Y), new Vector2(0, -0.8f), new Vector2(0, 0.8f), 0.07f * (1f - MathF.Abs(q.Y) * 0.8f)));
                }
                return (Fill(d, 2f), 1f);
            }
            case FxShape.Fang:
            {
                // One fang pointing down, curved
                Span<Vector2> v = stackalloc Vector2[] { new(-0.5f, 0.8f), new(0.5f, 0.8f), new(0.25f, 0f), new(0.02f, -0.9f), new(-0.2f, -0.1f) };
                return (Fill(Polygon(p, v)), 0.75f + 0.25f * Sat(0.4f - p.X));
            }
            case FxShape.Impact:
            {
                // A jagged burst: irregular points round a bright middle
                float d = StarDist(p, 9, 0.98f, 0.5f, 0.2f) + 0.06f * MathF.Sin(MathF.Atan2(p.Y, p.X) * 5f);
                float core = Sat(1f - r / 0.5f);
                return (MathF.Max(Fill(d, 2f) * 0.8f, core), 1f);
            }
            case FxShape.Zzz:
            {
                Span<Vector2> v = stackalloc Vector2[] { new(-0.62f, 0.72f), new(0.62f, 0.72f), new(0.62f, 0.46f), new(-0.2f, -0.46f), new(0.62f, -0.46f), new(0.62f, -0.72f), new(-0.62f, -0.72f), new(-0.62f, -0.46f), new(0.2f, 0.46f), new(-0.62f, 0.46f) };
                return (Fill(Polygon(p, v)), 1f);
            }
            case FxShape.Swirl:
            {
                // An Archimedean spiral line, fading toward its outer end
                float a = MathF.Atan2(p.Y, p.X);
                float best = 1f;
                for (int k = 0; k < 3; k++)
                {
                    float target = (a + MathF.PI + k * MathF.Tau) / (3f * MathF.Tau) * 0.9f;
                    best = MathF.Min(best, MathF.Abs(r - target));
                }
                return (Sat(1f - best / 0.06f) * Sat(1.1f - r), 1f);
            }
            case FxShape.Beam:
            {
                // Across the cell: a bright core with broad soft sides (ribbons run along u, so the beam fills most of
                // its width)
                float a = MathF.Exp(-p.Y * p.Y * 2.2f) * 0.7f + MathF.Exp(-p.Y * p.Y * 14f) * 0.3f;
                return (a, 1f);
            }
            case FxShape.Dot:
                return (Fill(r - 0.6f, 6f), 1f);
            case FxShape.Sword:
            {
                Span<Vector2> blade = stackalloc Vector2[] { new(0, 0.98f), new(0.14f, 0.7f), new(0.14f, -0.35f), new(-0.14f, -0.35f), new(-0.14f, 0.7f) };
                float d = MathF.Min(Polygon(p, blade), Capsule(p, new Vector2(-0.4f, -0.42f), new Vector2(0.4f, -0.42f), 0.07f));
                d = MathF.Min(d, Capsule(p, new Vector2(0, -0.45f), new Vector2(0, -0.9f), 0.07f));
                return (Fill(d), p.X < 0f ? 1f : 0.78f);
            }
            case FxShape.Wave:
            {
                // An arc of a ring, like a sound wave, opening to the right
                float d = MathF.Abs(r - 0.75f);
                float a = MathF.Abs(MathF.Atan2(p.Y, p.X));
                return (Sat(1f - d / 0.12f) * Sat((1.1f - a) / 0.4f), 1f);
            }
            case FxShape.Ember:
            {
                float d = Teardrop(p * 1.4f, 0.5f, 0.6f);
                return (Sat(-d / 0.25f), 1f);
            }
            case FxShape.Cross:
            {
                float d = MathF.Min(Capsule(p, new Vector2(-0.8f, -0.8f), new Vector2(0.8f, 0.8f), 0.08f),
                    Capsule(p, new Vector2(-0.8f, 0.8f), new Vector2(0.8f, -0.8f), 0.08f));
                return (Fill(d, 2.5f), 1f);
            }
            case FxShape.Spore:
            {
                // A soft ball of light with a firmer middle
                return (MathF.Max(Sat(1f - r) * Sat(1f - r) * 0.6f, Fill(r - 0.3f, 4f)), 1f);
            }
        }
        return (0f, 1f);
    }
}
