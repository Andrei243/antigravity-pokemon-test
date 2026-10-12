using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// One piece of a scene effect to draw: a cell of the scene atlas (<see cref="LifeArt.Scene"/>), a tile across,
/// tinted, lying on the ground (<paramref name="Flat"/>, turned by quarter turns as a footprint is) or standing up
/// facing the eye. An upright piece stands <paramref name="Lift"/> tiles above <paramref name="At"/>'s height, in
/// rows of a tile to be stretched like every upright thing in the field.
/// </summary>
internal readonly record struct SceneQuad(Vector3 At, float Width, float Height, int Cell, Color Tint, bool Flat, int Turn = 0, float Lift = 0f);

/// <summary>
/// How the picture wavers now (style guide, "Life", "Scene effects"): its rows pushed sideways by at most
/// <paramref name="Reach"/> texels of the field on a sine that has travelled <paramref name="Phase"/> radians.
/// The default is no ripple at all.
/// </summary>
public readonly record struct SceneRipple(int Reach, float Phase)
{
    public bool Plays => Reach > 0;
}

/// <summary>
/// The scene effects a story's script lights in the field (plan 09 · L11; style guide, "Life", "Scene effects"):
/// what each kind draws at a moment, as a pure function of its age, its length, its seed and the way it is sent,
/// as <see cref="FieldLife.Quads"/> is for life and <see cref="MoveFx"/> for a battle's cues. No GPU calls, so
/// tests hold each moment still.
/// </summary>
internal static class SceneFx
{
    public const int Cell = 32, Columns = 8, Rows = 5;

    // Cells of the atlas: the burst's three frames, a spark's three, the beam's four widths and the glow at its
    // foot, the rift's six frames (top and bottom half each) and the shadow's two frames (three cells by two each)
    public const int Burst = 0, Spark = 3, Beam = 6, Glow = 10, Rift = 11, Shadow = 23;

    /// <summary>
    /// The ripple's grid: a row is a texel of the field at the picture's height (a 1080p picture has three pixels
    /// to the texel), and its offset is counted in texels of the picture's width; the sine is this many rows long.
    /// </summary>
    public const int RippleRows = 360, RippleColumns = 640, RippleWave = 24;

    private const float Texel = 1f / 32f;

    /// <summary>How far in front of its tile's middle an upright effect stands, so it is over whoever stands there.</summary>
    private const float Front = 4f * Texel;

    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    private static readonly Color BurstTint = Rgb(255, 244, 208), SparkTint = Rgb(255, 246, 196), RiftMote = Rgb(214, 110, 246);
    private static readonly Color ShadowTint = new(20, 14, 36, 120);

    /// <summary>A tile's seed, from whole numbers alone (never a string's hash, which changes from run to run).</summary>
    public static int SeedOf(int x, int y) => (int)(((uint)x * 73856093u) ^ ((uint)y * 19349663u)) & 0x7FFFFFFF;

    private static float Hash(int seed, int i, int salt) => GroundBaker.Rand01(seed, i, salt);

    private static float Snap(float v) => MathF.Round(v * 32f) / 32f;

    /// <summary>Alpha as an effect goes, in the two steps life fades by (170, then 90) over its last stretch.</summary>
    private static byte Going(SceneEffect kind, float age, float ends)
    {
        float left = ends - age, going = SceneEffects.Out(kind);
        if (left >= going) return 255;
        float f = left / going;
        return f > 0.66f ? (byte)255 : f > 0.33f ? (byte)170 : (byte)90;
    }

    private static byte Times(byte a, byte b) => (byte)(a * b / 255);

    /// <summary>When a held effect that is stopped at <paramref name="age"/> is over: a burst or a shadow at the end of the round it is in, the rest after their last stretch.</summary>
    public static float StopAt(SceneEffect kind, float age, float seconds) => kind is SceneEffect.Burst or SceneEffect.Shadow && seconds > 0f
        ? (MathF.Floor(age / seconds) + 1f) * seconds
        : age + SceneEffects.Out(kind);

    /// <summary>The quads of an effect of its own length, round its tile's foot at the origin.</summary>
    public static void Quads(SceneEffect kind, float age, float seconds, int seed, Direction way, List<SceneQuad> into) =>
        Quads(kind, age, seconds, seconds, seed, way, Vector3.Zero, into);

    /// <summary>
    /// What an effect draws <paramref name="age"/> seconds after it was lit: <paramref name="seconds"/> is its
    /// length (a round of it, for one that is held), <paramref name="ends"/> when it is over (infinity while it
    /// is held), <paramref name="foot"/> the middle of its tile on the ground. A ripple draws nothing here.
    /// </summary>
    public static void Quads(SceneEffect kind, float age, float seconds, float ends, int seed, Direction way, Vector3 foot, List<SceneQuad> into)
    {
        if (age < 0f || age >= ends || seconds <= 0f) return;
        byte alpha = Going(kind, age, ends);
        var front = foot + new Vector3(0f, 0f, Front);
        switch (kind)
        {
            case SceneEffect.Burst:
            {
                // Each round of a held burst is the same burst again
                float t = age % seconds;
                if (t < 0.3f) into.Add(new SceneQuad(front, 1f, 1f, Burst + (int)(t / 0.1f), BurstTint with { A = alpha }, false));
                // Eight sparks fly out on the screen's plane, slowing as they go, and linger as dots; those thrown
                // downward are thrown low, or the ground would swallow them
                float reach = 40f * Texel * (1f - (1f - MathF.Min(1f, t / 0.5f)) * (1f - MathF.Min(1f, t / 0.5f)));
                int frame = t < 0.2f ? 0 : t < 0.4f ? 1 : 2;
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * MathF.PI / 4f + (Hash(seed, i, 961) - 0.5f) * 0.5f;
                    float x = MathF.Cos(angle) * reach, up = MathF.Sin(angle) * reach;
                    if (up < 0f) up *= 0.3f;
                    into.Add(new SceneQuad(front + new Vector3(Snap(x), 0f, 0f), 1f, 1f, Spark + frame, SparkTint with { A = alpha }, false, Lift: Snap(up)));
                }
                break;
            }
            case SceneEffect.Beam:
            {
                // It widens a texel each side at a time, then breathes between its two widest, and narrows as it goes
                int frame = age < 0.2f ? Math.Min(3, (int)(age / 0.05f)) : (int)((age - 0.2f) / 0.1f) % 2 == 0 ? 3 : 2;
                float left = ends - age;
                if (left < SceneEffects.Out(kind)) frame = Math.Clamp((int)(left / 0.05f), 0, frame);
                for (int s = 0; s < 6; s++)
                {
                    byte a = s < 4 ? alpha : Times(alpha, s == 4 ? (byte)170 : (byte)90);
                    into.Add(new SceneQuad(front, 1f, 1f, Beam + frame, Color.White with { A = a }, false, Lift: s));
                }
                into.Add(new SceneQuad(foot + new Vector3(0f, 0.015f, 0f), 1f, 1f, Glow, Color.White with { A = (byte)(alpha / 2) }, true));
                // Two motes of light rise up the column, a round of it each second
                for (int i = 0; i < 2; i++)
                {
                    float x = (Hash(seed, i, 962) - 0.5f) * 0.25f;
                    float up = (age * 0.75f + Hash(seed, i, 963)) % 1f * 4f;
                    into.Add(new SceneQuad(front + new Vector3(Snap(x), 0f, Texel), 1f, 1f, Spark + 2, Color.White with { A = alpha }, false, Lift: Snap(up)));
                }
                break;
            }
            case SceneEffect.Rift:
            {
                // A slit, then half open, then open with its rim crawling; it closes the same way
                int frame = age < 0.12f ? 0 : age < 0.25f ? 1 : 2 + (int)((age - 0.25f) / 0.125f) % 4;
                float left = ends - age;
                if (left < SceneEffects.Out(kind)) frame = left < 0.12f ? 0 : 1;
                byte a = frame == 0 ? Times(alpha, 200) : alpha;
                into.Add(new SceneQuad(front, 1f, 1f, Rift + 2 * frame + 1, Color.White with { A = a }, false, Lift: 0.5f));
                into.Add(new SceneQuad(front, 1f, 1f, Rift + 2 * frame, Color.White with { A = a }, false, Lift: 1.5f));
                // Once open it sheds motes of its rim, drifting up and out to its sides
                if (frame >= 2)
                    for (int i = 0; i < 2; i++)
                    {
                        float p = (age * 0.5f + Hash(seed, i, 964)) % 1f;
                        float x = (i == 0 ? -1f : 1f) * (0.25f + p * 0.25f);
                        into.Add(new SceneQuad(front + new Vector3(Snap(x), 0f, Texel), 1f, 1f, Spark + 2, RiftMote with { A = alpha }, false, Lift: Snap(0.5f + p * 1.5f)));
                    }
                break;
            }
            case SceneEffect.Shadow:
            {
                // It crosses sixteen tiles the way it is sent, from eight before its tile to eight beyond, beating its wings
                float t = age % seconds;
                float along = Snap(-8f + 16f * t / seconds);
                int frame = (int)(t / 0.25f) % 2;
                var tint = ShadowTint with { A = Times(ShadowTint.A, alpha) };
                int turn = way switch { Direction.Right => 1, Direction.Down => 2, Direction.Left => 3, _ => 0 };
                for (int row = 0; row < 2; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        // Its shape heads north in the atlas; the cells are turned and laid round where it has come to
                        float lx = col - 1f, lz = row - 0.5f - along;
                        var (dx, dz) = way switch
                        {
                            Direction.Right => (-lz, lx),
                            Direction.Down => (-lx, -lz),
                            Direction.Left => (lz, -lx),
                            _ => (lx, lz)
                        };
                        into.Add(new SceneQuad(foot + new Vector3(dx, 0.015f, dz), 1f, 1f, Shadow + frame * 6 + row * 3 + col, tint, true, turn));
                    }
                break;
            }
            case SceneEffect.Sparkles:
            {
                // Eight points, each a round of 0.6 s begun in its own eighth of the round (the first at once, so
                // something shows from the start): a star, a cross, a dot, then nothing, rising a texel at a time
                const float round = 0.6f, shows = 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    float first = round / 8f * (i + (i == 0 ? 0f : Hash(seed, i, 965)));
                    if (age < first) continue;
                    int n = (int)((age - first) / round);
                    float start = first + n * round, t = age - start;
                    if (t >= shows || start + shows > ends) continue;
                    int frame = t < 0.2f ? 0 : t < 0.4f ? 1 : 2;
                    float x = (Hash(seed, i * 31 + n, 966) * 2f - 1f) * 0.75f;
                    float up = Hash(seed, i * 31 + n, 967) - 0.4f + MathF.Floor(t * 16f) * Texel;
                    into.Add(new SceneQuad(front + new Vector3(Snap(x), 0f, 0f), 1f, 1f, Spark + frame, SparkTint with { A = alpha }, false, Lift: Snap(up)));
                }
                break;
            }
        }
    }

    /// <summary>How white a burst makes the whole picture: 85 % for its first twentieth of a second, then 50 %, then 20 % until a fifth; only in its first round.</summary>
    public static float Flash(SceneEffect kind, float age, float ends)
    {
        if (kind != SceneEffect.Burst || age < 0f || age >= ends) return 0f;
        return age < 0.05f ? 0.85f : age < 0.12f ? 0.5f : age < 0.2f ? 0.2f : 0f;
    }

    /// <summary>
    /// How a ripple wavers the picture <paramref name="age"/> seconds after it began: a texel, two, then three a
    /// tenth of a second apart, stepping back down over its last stretch; the wave travels two lengths a second,
    /// stepped thirty times a second.
    /// </summary>
    public static SceneRipple Ripple(SceneEffect kind, float age, float ends)
    {
        if (kind != SceneEffect.Ripple || age < 0f || age >= ends) return default;
        int reach = age < 0.1f ? 1 : age < 0.2f ? 2 : 3;
        float left = ends - age, going = SceneEffects.Out(kind);
        if (left < going) reach = Math.Min(reach, left / going > 0.66f ? 3 : left / going > 0.33f ? 2 : 1);
        float phase = MathF.Floor(age * 30f) / 30f * MathF.Tau * 2f;
        return new SceneRipple(reach, phase);
    }

    /// <summary>How many texels a row of the picture is pushed sideways: what the composite's shader works out for itself (<see cref="FieldShaders.SetPost"/>).</summary>
    public static int RippleOffset(int row, SceneRipple ripple) =>
        ripple.Reach == 0 ? 0 : (int)MathF.Floor(ripple.Reach * MathF.Sin(MathF.Tau * row / RippleWave + ripple.Phase) + 0.5f);
}

internal static partial class LifeArt
{
    /// <summary>
    /// The scene effects' atlas (style guide, "Life", "Scene effects"): cells of 32 texels, a tile each, eight to
    /// a row. The burst's frames and the sparks are white and grey to be tinted; the beam, the rift and the
    /// shadow are painted in their own colours (the shadow white, to be tinted dark, its rim at half alpha).
    /// </summary>
    public static PixelCanvas Scene()
    {
        const int cell = SceneFx.Cell;
        var c = new PixelCanvas(cell * SceneFx.Columns, cell * SceneFx.Rows);
        var white = Color.White;
        var grey = Rgb(214, 214, 222);
        (int X, int Y) Origin(int index) => (cell * (index % SceneFx.Columns), cell * (index / SceneFx.Columns));

        // The burst: a core and its rays, out from the middle of the cell, in three frames
        void Rays(int index, int count, float from, float to, float width, Color near, Color far, float breakFrom = 0f, float breakTo = 0f)
        {
            var (ox, oy) = Origin(index);
            for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    float vx = x + 0.5f - 16f, vy = y + 0.5f - 16f, d = MathF.Sqrt(vx * vx + vy * vy);
                    if (d < from || d > to || (d > breakFrom && d < breakTo)) continue;
                    float angle = MathF.Atan2(vy, vx), step = MathF.Tau / count;
                    float off = angle - MathF.Round(angle / step) * step;
                    // An axis's ray is two texels wide, a diagonal's a line of single texels
                    bool axis = count == 4 || (int)MathF.Round(angle / step) % 2 == 0;
                    if (MathF.Abs(d * MathF.Sin(off)) > (axis ? width : width * 0.75f)) continue;
                    c.SetRaw(ox + x, oy + y, d < (from + to) / 2f ? near : far);
                }
        }
        void Core(int index, int size, Color col)
        {
            var (ox, oy) = Origin(index);
            Pix.Disc(c, ox + 16 - size / 2, oy + 16 - size / 2, size, col);
        }
        Rays(SceneFx.Burst, 4, 3f, 7f, 1f, white, white);
        Core(SceneFx.Burst, 6, white);
        Rays(SceneFx.Burst + 1, 8, 4f, 12f, 1f, white, grey);
        Core(SceneFx.Burst + 1, 8, white);
        Rays(SceneFx.Burst + 2, 8, 8f, 15f, 0.6f, grey, grey, 10.5f, 12f);
        Core(SceneFx.Burst + 2, 4, grey);

        // A spark: a star of seven texels, a cross of five, a dot of two
        var (sx, sy) = Origin(SceneFx.Spark);
        c.Rect(sx + 15, sy + 12, 1, 7, white);
        c.Rect(sx + 12, sy + 15, 7, 1, white);
        c.Rect(sx + 14, sy + 14, 3, 3, white);
        var (kx, ky) = Origin(SceneFx.Spark + 1);
        c.Rect(kx + 15, ky + 13, 1, 5, white);
        c.Rect(kx + 13, ky + 15, 5, 1, white);
        var (dx, dy) = Origin(SceneFx.Spark + 2);
        c.Rect(dx + 15, dy + 15, 2, 2, white);

        // The beam: a column 12, 14, 16 and 18 texels wide, white at its core and blue at its edges, which step
        // in and out a texel every four rows so the light seems to run up it
        var beamCore = Rgb(248, 252, 255);
        var beamEdge = Rgb(206, 232, 255);
        var beamRim = Rgb(150, 196, 240);
        for (int f = 0; f < 4; f++)
        {
            var (ox, oy) = Origin(SceneFx.Beam + f);
            int width = 12 + 2 * f, x0 = 16 - width / 2;
            for (int y = 0; y < cell; y++)
            {
                int band = y / 4;
                int left = x0 + ((band + f) % 3 == 0 ? 1 : 0), right = x0 + width - ((band + f + 1) % 3 == 0 ? 1 : 0);
                for (int x = left; x < right; x++)
                {
                    var col = x == left || x == right - 1 ? beamRim : x < left + 3 || x >= right - 3 ? beamEdge : beamCore;
                    c.SetRaw(ox + x, oy + y, col);
                }
            }
        }

        // The glow lying round the beam's foot: an ellipse of 28 by 14 texels, paler in the middle
        var (gx, gy) = Origin(SceneFx.Glow);
        for (int y = 0; y < cell; y++)
            for (int x = 0; x < cell; x++)
            {
                float u = (x + 0.5f - 16f) / 14f, v = (y + 0.5f - 16f) / 7f, d = u * u + v * v;
                if (d <= 1f) c.SetRaw(gx + x, gy + y, d < 0.4f ? beamCore : beamEdge);
            }

        // The rift: a tear a tile wide and two tall, a slit, half open, then open in four frames whose ragged rim
        // has crawled on a little each time; dark inside, with specks
        var rimLight = Rgb(255, 214, 255);
        var rimMid = Rgb(214, 110, 246);
        var inside = Rgb(30, 14, 48);
        var speck = Rgb(90, 52, 128);
        for (int f = 0; f < 6; f++)
        {
            var (tx, ty) = Origin(SceneFx.Rift + 2 * f);
            var (bx, by) = Origin(SceneFx.Rift + 2 * f + 1);
            float open = f == 0 ? 1f : f == 1 ? 5f : 11f;
            for (int y = 0; y < 2 * cell; y++)
            {
                float s = MathF.Sin(MathF.PI * (y + 0.5f) / (2 * cell));
                if (s < 0.08f) continue;
                float jag = f == 0 ? 0f : MathF.Round(GroundBaker.Rand01(y / 3, f, 951) * 2f) - 1f;
                float half = MathF.Max(0f, open * MathF.Pow(s, 0.6f) + jag);
                float rim = s > 0.3f ? 2f : 1f;
                for (int x = 0; x < cell; x++)
                {
                    float d = MathF.Abs(x + 0.5f - 16f);
                    Color col;
                    if (d <= half) col = GroundBaker.Rand01(x, y + 64 * f, 952) < 0.06f ? speck : inside;
                    else if (d <= half + rim) col = rimMid;
                    else if (d <= half + rim + 1f) col = rimLight;
                    else continue;
                    if (y < cell) c.SetRaw(tx + x, ty + y, col);
                    else c.SetRaw(bx + x, by + y - cell, col);
                }
            }
        }

        // The shadow, heading north: a long body with a head and a tail, and wings spread wide (the first frame) or
        // drawn in and up (the second), three cells by two; its rim at half alpha
        for (int f = 0; f < 2; f++)
        {
            const int w = 3 * cell, h = 2 * cell;
            var shape = new bool[w, h];
            float reach = f == 0 ? 44f : 30f, raise = f == 0 ? 0f : -6f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float vx = x + 0.5f - 48f, vy = y + 0.5f;
                    float bu = vx / 7f, bv = (vy - 30f) / 22f;
                    bool body = bu * bu + bv * bv <= 1f;
                    bool head = vx * vx + (vy - 8f) * (vy - 8f) <= 25f;
                    float tailX = vx - (vy - 52f) / 3f;
                    bool tail = vy >= 50f && MathF.Abs(tailX) <= 2f - (vy - 50f) / 8f;
                    float d = MathF.Abs(vx);
                    bool wing = false;
                    if (d >= 6f && d <= reach)
                    {
                        float top = 18f + raise + d * 0.2f;
                        float notch = d % 12f / 12f;
                        float bottom = 32f + raise * 0.5f + d * 0.1f - 7f * (1f - MathF.Abs(notch * 2f - 1f));
                        bottom = MathF.Min(bottom, top + (reach - d) * 1.2f);
                        wing = vy >= top && vy <= bottom;
                    }
                    shape[x, y] = body || head || tail || wing;
                }
            var rimTint = new Color(255, 255, 255, 128);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!shape[x, y]) continue;
                    bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1
                        || !shape[x - 1, y] || !shape[x + 1, y] || !shape[x, y - 1] || !shape[x, y + 1];
                    var (ox, oy) = Origin(SceneFx.Shadow + f * 6 + y / cell * 3 + x / cell);
                    c.SetRaw(ox + x % cell, oy + y % cell, edge ? rimTint : white);
                }
        }
        return c;
    }
}
