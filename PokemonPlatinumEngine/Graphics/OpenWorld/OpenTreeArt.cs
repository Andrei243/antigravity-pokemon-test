using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// The open world's trees as they are built (style guide, "Open world", "Trees"): a trunk of bark that bends and
/// tapers, boughs off it, and its leaves as cards of a painted spray cut out by the art's alpha, each card shaded as
/// part of one rounded crown and tinted by where it hangs in it (dark inside and below, lit outside and on top); a pine
/// is a straight trunk with whorls of drooping boughs of needles. One sheet (<see cref="PaintSheet"/>) holds a spray of
/// broad leaves, a bough of needles and the bark. Every number of a shape comes from <see cref="Rand"/> of its variant,
/// so a shape is the same on any machine. GPU-free.
/// </summary>
internal static class OpenTreeArt
{
    /// <summary>The sheet's side in texels: four quarters (the leaves' spray, the needles' bough, the bark, plain white).</summary>
    public const int Sheet = 512;

    private const int Quarter = Sheet / 2;

    private static uint Hash(int a, int b, int c)
    {
        unchecked
        {
            uint h = (uint)a * 0x8DA6B343u ^ (uint)b * 0xD8163841u ^ (uint)c * 0xCB1AB31Fu;
            h ^= h >> 13;
            h *= 0x5BD1E995u;
            h ^= h >> 15;
            return h;
        }
    }

    private static float Rand(int a, int b, int c) => (Hash(a, b, c) & 0xFFFFFF) / (float)0x1000000;

    private static Color Rgb(float r, float g, float b, int a = 255) =>
        new((int)Math.Clamp(r, 0, 255), (int)Math.Clamp(g, 0, 255), (int)Math.Clamp(b, 0, 255), a);

    // ------------------------------------------------------------------ the sheet

    /// <summary>
    /// The sheet: its texels are shades near white (a tree's own greens and browns are its vertices' colours), opaque
    /// or cut out, nothing between.
    /// </summary>
    public static PixelCanvas PaintSheet()
    {
        var sheet = new PixelCanvas(Sheet, Sheet);
        // What is cut out keeps a leaf's own tone, so a blended edge never darkens
        for (int y = 0; y < Sheet; y++)
            for (int x = 0; x < Sheet; x++)
                sheet.SetRaw(x, y, Rgb(200, 215, 190, 0));
        PaintSpray(sheet);
        PaintBough(sheet);
        PaintBark(sheet);
        for (int y = Quarter; y < Sheet; y++)
            for (int x = Quarter; x < Sheet; x++)
                sheet.SetRaw(x, y, Color.White);
        return sheet;
    }

    /// <summary>A leaf: a pointed oval along a direction, lighter along its vein and toward its tip, a shade of its own.</summary>
    private static void Leaf(PixelCanvas sheet, int ox, int oy, float cx, float cy, float angle, float length, float width, float shade)
    {
        float ca = MathF.Cos(angle), sa = MathF.Sin(angle);
        int reach = (int)MathF.Ceiling(length / 2 + 1);
        for (int dy = -reach; dy <= reach; dy++)
            for (int dx = -reach; dx <= reach; dx++)
            {
                float along = (dx * ca + dy * sa) / (length / 2), across = (-dx * sa + dy * ca) / (width / 2);
                // Pointed at both ends: the width falls away toward them
                float half = 1f - along * along;
                if (half <= 0f || MathF.Abs(across) > MathF.Sqrt(half) * (0.75f + 0.25f * half)) continue;
                int x = (int)MathF.Round(cx + dx), y = (int)MathF.Round(cy + dy);
                if (x < 2 || y < 2 || x >= Quarter - 2 || y >= Quarter - 2) continue;
                float vein = 1f - MathF.Min(1f, MathF.Abs(across) * 3.2f);
                float edge = MathF.Abs(across) / MathF.Max(0.05f, MathF.Sqrt(half));
                float v = shade * (0.80f + 0.12f * (along * 0.5f + 0.5f) + 0.12f * vein - 0.16f * edge * edge);
                sheet.SetRaw(ox + x, oy + y, Rgb(232 * v, 255 * v, 214 * v));
            }
    }

    /// <summary>The leaves' spray (the top left quarter): some 130 leaves strewn in a rough disc, the deeper ones darker and laid first.</summary>
    private static void PaintSpray(PixelCanvas sheet)
    {
        const int leaves = 130;
        for (int i = 0; i < leaves; i++)
        {
            float depth = i / (float)(leaves - 1);
            float a = Rand(i, 1, 71) * MathF.Tau, r = MathF.Sqrt(Rand(i, 2, 71)) * 104f;
            float cx = Quarter / 2f + MathF.Cos(a) * r, cy = Quarter / 2f + MathF.Sin(a) * r;
            // Leaves point more or less away from the middle
            float angle = a + (Rand(i, 3, 71) - 0.5f) * 2.2f;
            Leaf(sheet, 0, 0, cx, cy, angle, 26f + Rand(i, 4, 71) * 14f, 11f + Rand(i, 5, 71) * 6f, 0.62f + 0.38f * depth * (0.8f + 0.2f * Rand(i, 6, 71)));
        }
    }

    /// <summary>The needles' bough (the top right quarter): a twig from the left edge to the right with needles off both sides, shorter toward its tip, and side twigs.</summary>
    private static void PaintBough(PixelCanvas sheet)
    {
        void Needle(float x0, float y0, float angle, float length, float shade)
        {
            for (float t = 0; t <= length; t += 0.5f)
            {
                int x = (int)MathF.Round(x0 + MathF.Cos(angle) * t), y = (int)MathF.Round(y0 + MathF.Sin(angle) * t);
                for (int k = 0; k < 2; k++)
                {
                    int px = x, py = y + k;
                    if (px < 2 || py < 2 || px >= Quarter - 2 || py >= Quarter - 2) continue;
                    float v = shade * (0.78f + 0.22f * t / length);
                    sheet.SetRaw(Quarter + px, py, Rgb(226 * v, 255 * v, 226 * v));
                }
            }
        }
        void Twig(float x0, float y0, float angle, float length, int salt)
        {
            int n = (int)(length / 2.2f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n, x = x0 + MathF.Cos(angle) * length * t, y = y0 + MathF.Sin(angle) * length * t;
                float len = (15f - 9f * t) * (0.8f + 0.4f * Rand(i, salt, 91));
                float shade = 0.66f + 0.34f * Rand(i, salt, 92);
                Needle(x, y, angle + 0.95f + (Rand(i, salt, 93) - 0.5f) * 0.4f, len, shade);
                Needle(x, y, angle - 0.95f + (Rand(i, salt, 94) - 0.5f) * 0.4f, len, shade);
            }
            // The twig itself, dark under its needles
            for (float t = 0; t <= length; t += 0.5f)
            {
                int x = (int)MathF.Round(x0 + MathF.Cos(angle) * t), y = (int)MathF.Round(y0 + MathF.Sin(angle) * t);
                for (int k = -1; k <= 1; k++)
                    if (x >= 2 && y + k >= 2 && x < Quarter - 2 && y + k < Quarter - 2) sheet.SetRaw(Quarter + x, y + k, Rgb(150, 138, 120));
            }
        }
        float mid = Quarter / 2f;
        for (int s = 0; s < 9; s++)
        {
            float x = 26f + s * 24f, way = s % 2 == 0 ? 1f : -1f;
            Twig(x, mid, way * (0.62f + 0.2f * Rand(s, 1, 95)), (104f - s * 9.5f) * (0.8f + 0.3f * Rand(s, 2, 95)), 10 + s);
        }
        Twig(4f, mid, 0f, Quarter - 10f, 3);
    }

    /// <summary>The bark (the bottom left quarter): furrows running up it, opaque, coming round from side to side.</summary>
    private static void PaintBark(PixelCanvas sheet)
    {
        for (int y = 0; y < Quarter; y++)
            for (int x = 0; x < Quarter; x++)
            {
                // Ridges 16 texels apart that wander as they climb, and a grain over them
                float wander = MathF.Sin(y * 0.045f + Rand(x / 16, 0, 97) * 6f) * 3.5f + MathF.Sin(y * 0.11f) * 1.5f;
                float f = (x + wander) / 16f;
                float ridge = MathF.Abs(f - MathF.Floor(f) - 0.5f) * 2f;
                float grain = Rand(x, y / 5, 98) * 0.14f + Rand(x / 3, y / 17, 99) * 0.12f;
                float v = 0.50f + 0.42f * MathF.Pow(1f - ridge, 0.7f) + grain - 0.12f;
                sheet.SetRaw(x, Quarter + y, Rgb(255 * v, 250 * v, 240 * v));
            }
    }

    // ------------------------------------------------------------------ the shapes

    private static Vector2 Uv(int quarterX, int quarterY, float u, float v)
    {
        const float inset = 3f / Sheet;
        return new Vector2(quarterX * 0.5f + inset + u * (0.5f - 2 * inset), quarterY * 0.5f + inset + v * (0.5f - 2 * inset));
    }

    /// <summary>A length of trunk or bough: a tapering tube from one point to another, in bark, smooth round.</summary>
    private static void Tube(MeshBuilder b, Vector3 from, Vector3 to, float r0, float r1, int sides, Color colour, float v0, float v1)
    {
        var axis = Vector3.Normalize(to - from);
        var side = Vector3.Normalize(Vector3.Cross(axis, MathF.Abs(axis.Y) > 0.9f ? Vector3.UnitX : Vector3.UnitY));
        var other = Vector3.Cross(axis, side);
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * MathF.Tau / sides, a1 = (i + 1) * MathF.Tau / sides;
            var n0 = side * MathF.Cos(a0) + other * MathF.Sin(a0);
            var n1 = side * MathF.Cos(a1) + other * MathF.Sin(a1);
            float u0 = i / (float)sides * 2f % 1f, u1 = u0 + 2f / sides;
            Vector3 p00 = from + n0 * r0, p01 = from + n1 * r0, p10 = to + n0 * r1, p11 = to + n1 * r1;
            b.Vertex(p00, Uv(0, 1, u0, v0), n0, colour); b.Vertex(p10, Uv(0, 1, u0, v1), n0, colour); b.Vertex(p11, Uv(0, 1, u1, v1), n1, colour);
            b.Vertex(p00, Uv(0, 1, u0, v0), n0, colour); b.Vertex(p11, Uv(0, 1, u1, v1), n1, colour); b.Vertex(p01, Uv(0, 1, u1, v0), n1, colour);
        }
    }

    /// <summary>A card of the sheet: four corners, a normal and a colour to each (two triangles).</summary>
    private static void Card(MeshBuilder b, int quarterX, Vector3 p00, Vector3 p10, Vector3 p11, Vector3 p01,
        Vector3 n00, Vector3 n10, Vector3 n11, Vector3 n01, Color c00, Color c10, Color c11, Color c01)
    {
        Vector2 t00 = Uv(quarterX, 0, 0, 0), t10 = Uv(quarterX, 0, 1, 0), t11 = Uv(quarterX, 0, 1, 1), t01 = Uv(quarterX, 0, 0, 1);
        b.Vertex(p00, t00, n00, c00); b.Vertex(p10, t10, n10, c10); b.Vertex(p11, t11, n11, c11);
        b.Vertex(p00, t00, n00, c00); b.Vertex(p11, t11, n11, c11); b.Vertex(p01, t01, n01, c01);
    }

    private static Color Mix(Color a, Color b, float t) => PixelCanvas.Mix(a, b, Math.Clamp(t, 0f, 1f));

    /// <summary>
    /// A broad-leaved tree of a variant, standing at the origin, about 4.4 units tall at size one (the renderer's
    /// <see cref="OpenFoliage.TreeScale"/> over that): a bending trunk, five or six boughs into the crown, and some 110
    /// cards of leaves through it.
    /// </summary>
    public static MeshBuilder Broadleaf(int variant)
    {
        var b = new MeshBuilder();
        int s = 500 + variant * 37;
        var bark = Mix(Rgb(92, 76, 62), Rgb(110, 92, 70), Rand(s, 0, 1));
        float lean = (Rand(s, 0, 2) - 0.5f) * 0.5f, leanWay = Rand(s, 0, 3) * MathF.Tau;
        var drift = new Vector3(MathF.Cos(leanWay), 0, MathF.Sin(leanWay)) * lean;
        float trunkTop = 1.9f + Rand(s, 0, 4) * 0.4f;

        // The trunk in four lengths, flaring at its foot
        const int lengths = 4;
        var at = Vector3.Zero;
        float radius = 0.14f;
        for (int i = 0; i < lengths; i++)
        {
            float t = (i + 1) / (float)lengths;
            var next = new Vector3(drift.X * t * t, trunkTop * t, drift.Z * t * t);
            float r = i == 0 ? 0.12f : 0.12f - 0.03f * t;
            Tube(b, at, next, radius, r, 9, bark, i * 0.45f, (i + 1) * 0.45f);
            at = next;
            radius = r;
        }

        var crown = at + new Vector3(drift.X * 0.6f, 1.25f + Rand(s, 0, 5) * 0.2f, drift.Z * 0.6f);
        var radii = new Vector3(1.55f + Rand(s, 0, 6) * 0.35f, 1.15f + Rand(s, 0, 7) * 0.3f, 1.55f + Rand(s, 0, 8) * 0.35f);

        // Boughs from the trunk's top out into the crown
        int boughs = 5 + (int)(Rand(s, 0, 9) * 2f);
        for (int i = 0; i < boughs; i++)
        {
            float a = i * MathF.Tau / boughs + Rand(s, i, 10) * 0.7f, up = 0.25f + Rand(s, i, 11) * 0.75f;
            var tip = crown + new Vector3(MathF.Cos(a) * radii.X * 0.62f, (up - 0.55f) * radii.Y, MathF.Sin(a) * radii.Z * 0.62f);
            var elbow = Vector3.Lerp(at, tip, 0.5f) + new Vector3(0, 0.18f, 0);
            Tube(b, at - new Vector3(0, 0.12f, 0), elbow, 0.085f, 0.055f, 6, bark, 0f, 0.4f);
            Tube(b, elbow, tip, 0.055f, 0.02f, 5, bark, 0.4f, 0.8f);
        }

        // The leaves: cards through the crown, more of them toward its outside, each facing more or less outward and
        // every second one crossed by another, all shaded as one rounded crown
        var dark = Mix(Rgb(44, 96, 40), Rgb(54, 106, 44), Rand(s, 0, 12));
        var light = Mix(Rgb(120, 182, 70), Rgb(142, 196, 78), Rand(s, 0, 13));
        var core = crown - new Vector3(0, radii.Y * 0.35f, 0);
        const int cards = 112;
        for (int i = 0; i < cards; i++)
        {
            float z = 1f - 1.7f * Rand(s, i, 20), ring = MathF.Sqrt(MathF.Max(0f, 1f - z * z)), a = Rand(s, i, 21) * MathF.Tau;
            var way = new Vector3(ring * MathF.Cos(a), z, ring * MathF.Sin(a));
            float depth = 0.42f + 0.58f * MathF.Sqrt(Rand(s, i, 22));
            var middle = crown + way * radii * depth;
            float size = (0.52f + Rand(s, i, 23) * 0.34f) * (1.15f - 0.25f * depth);
            var facing = Vector3.Normalize(way + new Vector3(Rand(s, i, 24) - 0.5f, Rand(s, i, 25) - 0.3f, Rand(s, i, 26) - 0.5f) * 1.3f);
            var across = Vector3.Normalize(Vector3.Cross(facing, MathF.Abs(facing.Y) > 0.9f ? Vector3.UnitX : Vector3.UnitY));
            var upward = Vector3.Cross(across, facing);
            float turn = Rand(s, i, 27) * MathF.Tau;
            (across, upward) = (across * MathF.Cos(turn) + upward * MathF.Sin(turn), upward * MathF.Cos(turn) - across * MathF.Sin(turn));
            float tone = 0.9f + 0.2f * Rand(s, i, 28);

            void Leaves(Vector3 x, Vector3 y)
            {
                Vector3 p00 = middle - x * size - y * size, p10 = middle + x * size - y * size, p11 = middle + x * size + y * size, p01 = middle - x * size + y * size;
                Vector3 N(Vector3 p) => Vector3.Normalize(Vector3.Lerp(facing, Vector3.Normalize(p - core), 0.8f));
                Color C(Vector3 p)
                {
                    var rel = (p - crown) / radii;
                    float height = Math.Clamp(rel.Y * 0.5f + 0.5f, 0f, 1f), outside = Math.Clamp(rel.Length(), 0f, 1.1f);
                    var c = MeshBuilder.Scale(Mix(dark, light, height * 0.62f + outside * outside * 0.42f - 0.08f), tone);
                    return MeshBuilder.Sway(c, 0.10f + 0.22f * height + 0.12f * outside);
                }
                Card(b, 0, p00, p10, p11, p01, N(p00), N(p10), N(p11), N(p01), C(p00), C(p10), C(p11), C(p01));
            }
            Leaves(across, upward);
            if (i % 2 == 0) Leaves(facing, upward);
        }
        return b;
    }

    /// <summary>
    /// A pine of a variant, standing at the origin, about 5.6 units tall at size one: a straight trunk bare at its
    /// foot and whorls of boughs of needles up it, long and drooping low down, short and lifted toward its top.
    /// </summary>
    public static MeshBuilder Pine(int variant)
    {
        var b = new MeshBuilder();
        int s = 900 + variant * 41;
        var bark = Mix(Rgb(82, 62, 50), Rgb(96, 72, 54), Rand(s, 0, 1));
        float height = 5.2f + Rand(s, 0, 2) * 0.8f;
        const int lengths = 5;
        for (int i = 0; i < lengths; i++)
        {
            float t0 = i / (float)lengths, t1 = (i + 1) / (float)lengths;
            Tube(b, new Vector3(0, height * t0, 0), new Vector3(0, height * t1, 0), 0.13f * (1f - t0 * 0.9f) + (i == 0 ? 0.01f : 0f), 0.13f * (1f - t1 * 0.9f), 8, bark, t0 * 2.2f, t1 * 2.2f);
        }

        var dark = Mix(Rgb(30, 76, 52), Rgb(36, 84, 54), Rand(s, 0, 3));
        var light = Mix(Rgb(84, 142, 86), Rgb(98, 154, 90), Rand(s, 0, 4));
        float low = 1.0f + Rand(s, 0, 5) * 0.4f, spread = 1.45f + Rand(s, 0, 6) * 0.3f;
        const int whorls = 12;
        for (int k = 0; k < whorls; k++)
        {
            float t = k / (float)(whorls - 1);
            float y = low + (height - low) * MathF.Pow(t, 0.9f) * 0.97f;
            float reach = spread * (MathF.Pow(1f - t, 0.85f) * 0.94f + 0.10f);
            int boughs = t > 0.8f ? 5 : 7;
            float droop = 0.34f - 0.62f * t;
            for (int i = 0; i < boughs; i++)
            {
                float a = (i + Rand(s + k, i, 10) * 0.6f) * MathF.Tau / boughs + k * 0.9f;
                float length = reach * (0.82f + 0.3f * Rand(s + k, i, 11));
                var outward = new Vector3(MathF.Cos(a), 0, MathF.Sin(a));
                var tangent = new Vector3(-outward.Z, 0, outward.X);
                var root = new Vector3(0, y + (Rand(s + k, i, 12) - 0.5f) * 0.18f, 0);
                var tip = root + outward * length - new Vector3(0, droop * length, 0);
                float half = length * 0.34f;
                var along = Vector3.Normalize(tip - root);
                var over = Vector3.Normalize(Vector3.Cross(tangent, along));
                float tone = 0.88f + 0.24f * Rand(s + k, i, 13);
                Color inner = MeshBuilder.Sway(MeshBuilder.Scale(Mix(dark, light, 0.12f + 0.3f * t), tone), 0.05f + 0.1f * t);
                Color outer = MeshBuilder.Sway(MeshBuilder.Scale(Mix(dark, light, 0.62f + 0.38f * t), tone), 0.22f + 0.2f * t);
                var nRoot = Vector3.Normalize(over * 0.8f + outward * 0.3f);
                var nTip = Vector3.Normalize(over * 0.6f + outward * 0.8f);
                // Flat across the bough, and upright along it, so it has needles from above and from the side
                Card(b, 1, root - tangent * half, tip - tangent * half, tip + tangent * half, root + tangent * half,
                    nRoot, nTip, nTip, nRoot, inner, outer, outer, inner);
                Card(b, 1, root - over * half * 0.7f, tip - over * half * 0.7f, tip + over * half * 0.7f, root + over * half * 0.7f,
                    nRoot, nTip, nTip, nRoot, inner, outer, outer, inner);
            }
        }
        // Its leader: three boughs straight up from the top
        for (int i = 0; i < 3; i++)
        {
            float a = i * MathF.Tau / 3f;
            var side = new Vector3(MathF.Cos(a), 0, MathF.Sin(a)) * 0.2f;
            var root = new Vector3(0, height - 0.35f, 0);
            var tip = new Vector3(0, height + 0.35f, 0);
            var top = MeshBuilder.Sway(light, 0.4f);
            var n = Vector3.Normalize(Vector3.UnitY + Vector3.Normalize(Vector3.Cross(side, Vector3.UnitY)));
            Card(b, 1, root - side, tip - side, tip + side, root + side, n, n, n, n, top, top, top, top);
        }
        return b;
    }

    /// <summary>
    /// A tuft of grass at the origin (style guide, "Open world", "Grass"): eleven broad blades in a clump 0.5 m
    /// across and up to 0.3 m tall, leaning out, dark at the root and light at the tip, shaded as the ground under
    /// them is (their normals straight up), their tips swaying and parting; with <paramref name="flowers"/>, three
    /// stems with a blossom each over the blades, white, yellow or pink by the tuft's seed.
    /// </summary>
    public static void Tuft(MeshBuilder b, int seed, Color root, Color tip, bool flowers)
    {
        var up = Vector3.UnitY;
        const int blades = 11;
        for (int i = 0; i < blades; i++)
        {
            float a = (i + Rand(seed, i, 31) * 0.8f) * MathF.Tau / blades, from = 0.04f + Rand(seed, i, 34) * 0.14f;
            var dir = new Vector3(MathF.Cos(a), 0, MathF.Sin(a));
            float h = 0.16f + Rand(seed, i, 32) * 0.14f, lean = 0.06f + Rand(seed, i, 33) * 0.12f;
            var side = new Vector3(-dir.Z, 0, dir.X) * 0.034f;
            var foot = dir * from;
            var mid = foot + dir * lean * 0.4f + new Vector3(0, h * 0.6f, 0);
            var top = foot + dir * lean + new Vector3(0, h, 0);
            var middle = MeshBuilder.Sway(Mix(root, tip, 0.55f), 0.4f);
            b.Tri(foot - side, foot + side, mid + side * 0.7f, default, default, default, up, up, up, root, root, middle);
            b.Tri(foot - side, mid + side * 0.7f, mid - side * 0.7f, default, default, default, up, up, up, root, middle, middle);
            b.Tri(mid - side * 0.7f, mid + side * 0.7f, top, default, default, default, up, up, up, middle, middle, MeshBuilder.Sway(tip, 1f));
        }
        if (!flowers) return;
        Color petal = (seed % 3) switch { 0 => Rgb(250, 250, 244), 1 => Rgb(250, 214, 92), _ => Rgb(240, 150, 190) };
        var heart = Rgb(232, 170, 60);
        for (int i = 0; i < 3; i++)
        {
            float a = Rand(seed, i, 51) * MathF.Tau, r = 0.05f + Rand(seed, i, 52) * 0.12f, h = 0.26f + Rand(seed, i, 53) * 0.1f;
            var foot = new Vector3(MathF.Cos(a) * r, 0, MathF.Sin(a) * r);
            var head = foot + new Vector3(0, h, 0);
            var stem = MeshBuilder.Sway(root, 0.5f);
            var s = new Vector3(0.008f, 0, 0);
            b.Tri(foot - s, foot + s, head, default, default, default, up, up, up, root, root, stem);
            // The blossom: five petals lying open round its heart, tipped a little outward
            var swayed = MeshBuilder.Sway(petal, 0.6f);
            var centre = MeshBuilder.Sway(heart, 0.6f);
            for (int p = 0; p < 5; p++)
            {
                float pa = p * MathF.Tau / 5f + a;
                var o = new Vector3(MathF.Cos(pa), 0, MathF.Sin(pa));
                var w = new Vector3(-o.Z, 0, o.X) * 0.017f;
                var end = head + o * 0.042f - new Vector3(0, 0.008f, 0);
                b.Tri(head, head + o * 0.022f + w, end, default, default, default, up, up, up, centre, swayed, swayed);
                b.Tri(head, end, head + o * 0.022f - w, default, default, default, up, up, up, centre, swayed, swayed);
            }
        }
    }
}
