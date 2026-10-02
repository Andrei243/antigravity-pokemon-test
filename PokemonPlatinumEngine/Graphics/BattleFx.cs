using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

internal enum FxBlend : byte { Add, Alpha }

/// <summary>One quad of an effect: a sprite facing the camera, stretched along a direction, or lying flat.</summary>
internal struct FxQuad
{
    public FxShape Shape;
    public FxBlend Blend;

    /// <summary>Tested against the scene's depth (rings round a body, marks on the ground); otherwise drawn over the scene.</summary>
    public bool Depth;
    public Vector3 Center;

    /// <summary>Half the quad's width, in world units.</summary>
    public float Size;

    /// <summary>Turn in the view plane (or about <see cref="Normal"/> for a flat quad), radians.</summary>
    public float Rotation;
    public Color Color;

    /// <summary>When not zero, the quad is <see cref="Stretch"/> times longer along this world direction (streaks, trails).</summary>
    public Vector3 Along;
    public float Stretch;

    /// <summary>When not zero, the quad lies flat, facing this way (shockwaves on the ground).</summary>
    public Vector3 Normal;
}

/// <summary>A strip that faces the camera along a line of points: beams and bolts.</summary>
internal sealed class FxRibbon
{
    public Vector3[] Points = Array.Empty<Vector3>();
    public float Width;
    public Color Color;
    public FxBlend Blend;
    public FxShape Shape = FxShape.Beam;
}

/// <summary>Everything the effects draw this frame, plus the screen flash and any extra shake. GPU-free.</summary>
internal sealed class FxList
{
    public readonly List<FxQuad> Quads = new();
    public readonly List<FxRibbon> Ribbons = new();

    /// <summary>A flash over the whole field (0..1, at most 0.5 by the style guide), in this colour.</summary>
    public float Flash;
    public Color FlashColor = Color.White;

    /// <summary>Extra camera shake an effect asks for (Earthquake), 0..1.</summary>
    public float Shake;

    public int Count => Quads.Count + Ribbons.Count;

    public void Clear()
    {
        Quads.Clear();
        Ribbons.Clear();
        Flash = 0f;
        Shake = 0f;
    }

    public void Sprite(FxShape shape, Vector3 at, float size, Color color, FxBlend blend = FxBlend.Add, float rotation = 0f, bool depth = false)
    {
        if (color.A == 0 || size <= 0f) return;
        Quads.Add(new FxQuad { Shape = shape, Blend = blend, Depth = depth, Center = at, Size = size, Rotation = rotation, Color = color });
    }

    /// <summary>A sprite drawn <paramref name="stretch"/> times longer along <paramref name="along"/> (in the view plane).</summary>
    public void Streak(FxShape shape, Vector3 at, Vector3 along, float size, float stretch, Color color, FxBlend blend = FxBlend.Add, bool depth = false)
    {
        if (color.A == 0 || size <= 0f || along.LengthSquared() < 1e-8f) return;
        Quads.Add(new FxQuad { Shape = shape, Blend = blend, Depth = depth, Center = at, Size = size, Color = color, Along = Vector3.Normalize(along), Stretch = stretch });
    }

    /// <summary>A quad lying in the plane whose normal is <paramref name="normal"/>.</summary>
    public void Flat(FxShape shape, Vector3 at, Vector3 normal, float size, Color color, FxBlend blend = FxBlend.Add, float rotation = 0f, bool depth = true)
    {
        if (color.A == 0 || size <= 0f) return;
        Quads.Add(new FxQuad { Shape = shape, Blend = blend, Depth = depth, Center = at, Size = size, Rotation = rotation, Color = color, Normal = Vector3.Normalize(normal) });
    }

    public void Ribbon(Vector3[] points, float width, Color color, FxBlend blend = FxBlend.Add, FxShape shape = FxShape.Beam)
    {
        if (color.A == 0 || width <= 0f || points.Length < 2) return;
        Ribbons.Add(new FxRibbon { Points = points, Width = width, Color = color, Blend = blend, Shape = shape });
    }

    public void ScreenFlash(Color color, float amount)
    {
        amount = Math.Clamp(amount, 0f, 0.5f);
        if (amount <= Flash) return;
        Flash = amount;
        FlashColor = color;
    }
}

/// <summary>Where each Pokémon stands and how tall it is this frame, for aiming effects and the camera.</summary>
internal sealed class FxPlaces
{
    private readonly Vector3[,] feet = new Vector3[2, 2];
    private readonly float[,] height = new float[2, 2];

    public FxPlaces()
    {
        // The settled stage's places and typical heights, for anything aimed before the renderer measures
        for (int slot = 0; slot < 2; slot++)
        {
            Set(BattleSide.Enemy, slot, BattleStage.EnemySpot + new Vector3(0, BattleStage.PlatformHeight, 0), 2.2f);
            Set(BattleSide.Player, slot, BattleStage.PlayerSpot + new Vector3(0, BattleStage.PlatformHeight, 0), 1.2f);
        }
    }

    public void Set(BattleSide side, int slot, Vector3 feetAt, float heightOf)
    {
        feet[(int)side, slot] = feetAt;
        height[(int)side, slot] = Math.Max(0.2f, heightOf);
    }

    public Vector3 Feet(BattleSide side, int slot) => feet[(int)side, Math.Clamp(slot, 0, 1)];

    /// <summary>Height of the Pokémon standing there (or a typical one), world units.</summary>
    public float Height(BattleSide side, int slot) => height[(int)side, Math.Clamp(slot, 0, 1)];

    /// <summary>The middle of its body: where moves aim.</summary>
    public Vector3 Body(BattleSide side, int slot) => Feet(side, slot) + Vector3.UnitY * Height(side, slot) * 0.5f;

    public Vector3 Head(BattleSide side, int slot) => Feet(side, slot) + Vector3.UnitY * Height(side, slot) * 0.8f;
}

/// <summary>A cue's places and timing, handed to the effect recipes.</summary>
internal readonly struct FxCtx
{
    public readonly EffectCue Cue;
    public readonly Vector3 From, To, FromFeet, ToFeet;
    public readonly float FromSize, ToSize;

    /// <summary>Where the move ends: the target, or past it for a miss.</summary>
    public readonly Vector3 Aim;

    public float Age => Cue.Age;
    public int Seed => Cue.Seed;

    public FxCtx(EffectCue cue, FxPlaces places)
    {
        Cue = cue;
        FromFeet = places.Feet(cue.FromSide, cue.FromSlot);
        ToFeet = places.Feet(cue.ToSide, cue.ToSlot);
        From = places.Body(cue.FromSide, cue.FromSlot);
        To = places.Body(cue.ToSide, cue.ToSlot);
        FromSize = places.Height(cue.FromSide, cue.FromSlot);
        ToSize = places.Height(cue.ToSide, cue.ToSlot);
        Aim = To;
        if (cue.Missed)
        {
            // Wide of the target and on past it
            var dir = To - From;
            var across = Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitY));
            Aim = To + across * ToSize * 1.1f + Vector3.Normalize(dir) * ToSize * 1.6f + Vector3.UnitY * ToSize * 0.2f;
        }
    }

    /// <summary>The size of things along the way: the user's at the start, the target's at the end.</summary>
    public float ScaleAt(float t) => FromSize + (ToSize - FromSize) * Math.Clamp(t, 0f, 1f);
}

/// <summary>
/// The effect primitives (plan 04 · G8): particles that fly, burst, rise and fall, beams and bolts, rings and
/// flashes. Each is a pure function of the cue's age and seed, so nothing is simulated or stored between frames.
/// Sizes are in units of the Pokémon's height.
/// </summary>
internal static class Fx
{
    public static float Rand(int seed, int i, int salt) => SoftCanvas.Rand(seed, i, salt);

    public static float Signed(int seed, int i, int salt) => Rand(seed, i, salt) * 2f - 1f;

    public static Color Fade(Color c, float alpha) => new(c.R, c.G, c.B, (byte)Math.Clamp((int)(c.A * Math.Clamp(alpha, 0f, 1f)), 0, 255));

    public static Color Mix(Color a, Color b, float t) => PixelCanvas.Mix(a, b, Math.Clamp(t, 0f, 1f));

    public static float EaseOut(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return 1f - (1f - t) * (1f - t);
    }

    /// <summary>0..1 over [a, b] and back to 0 over [c, d]: a fade in, hold and fade out.</summary>
    public static float Envelope(float t, float a, float b, float c, float d)
    {
        if (t <= a || t >= d) return 0f;
        if (t < b) return (t - a) / (b - a);
        if (t > c) return (d - t) / (d - c);
        return 1f;
    }

    /// <summary>A random direction, spread mostly sideways and up (bursts read best across the screen).</summary>
    public static Vector3 Direction(int seed, int i)
    {
        float a = Rand(seed, i, 11) * MathF.Tau;
        float y = Signed(seed, i, 12) * 0.8f + 0.15f;
        float s = MathF.Sqrt(MathF.Max(0f, 1f - y * y));
        return new Vector3(MathF.Cos(a) * s, y, MathF.Sin(a) * s * 0.6f);
    }

    /// <summary>Two directions at right angles to <paramref name="dir"/>, the first level with the ground.</summary>
    public static (Vector3 Across, Vector3 Up) Frame(Vector3 dir)
    {
        var d = dir.LengthSquared() < 1e-8f ? Vector3.UnitZ : Vector3.Normalize(dir);
        var across = Vector3.Cross(d, Vector3.UnitY);
        across = across.LengthSquared() < 1e-6f ? Vector3.UnitX : Vector3.Normalize(across);
        return (across, Vector3.Normalize(Vector3.Cross(across, d)));
    }

    /// <summary>
    /// Particles flying from the user to the target (or past it): they leave one after another between
    /// <paramref name="t0"/> and <paramref name="t1"/> − <paramref name="flight"/>, spreading into a cone, along an
    /// arc <paramref name="arc"/> × the distance high.
    /// </summary>
    public static void Stream(FxList fx, in FxCtx c, float t0, float t1, float flight, int count, FxShape shape, Color a, Color b,
        float size, float arc = 0f, float spread = 0.25f, float spin = 0f, FxBlend blend = FxBlend.Add, float trail = 0f, bool reverse = false)
    {
        var from = reverse ? c.To : c.From;
        var to = reverse ? c.From : c.Aim;
        var path = to - from;
        float dist = path.Length();
        var (across, up) = Frame(path);
        for (int i = 0; i < count; i++)
        {
            float leave = t0 + (t1 - flight - t0) * (count == 1 ? 0f : i / (float)(count - 1));
            float t = (c.Age - leave) / flight;
            if (t < 0f || t > 1f) continue;
            float jx = Signed(c.Seed, i, 1) * spread, jy = Signed(c.Seed, i, 2) * spread;
            float sizeAt = reverse ? c.ScaleAt(1f - t) : c.ScaleAt(t);
            var p = Vector3.Lerp(from, to, t) + Vector3.UnitY * arc * dist * 4f * t * (1f - t)
                + (across * jx + up * jy) * sizeAt * MathF.Sin(MathF.Min(1f, t * 1.4f) * MathF.PI * 0.5f);
            float s = size * sizeAt * (0.75f + 0.5f * Rand(c.Seed, i, 3));
            var color = Mix(a, b, Rand(c.Seed, i, 4));
            float fade = Math.Min(1f, (1f - t) * 6f) * Math.Min(1f, t * 8f + 0.3f);
            if (trail > 0f)
            {
                var dir = to - from + Vector3.UnitY * arc * dist * 4f * (1f - 2f * t);
                fx.Streak(shape, p, dir, s, 1f + trail, Fade(color, fade), blend);
            }
            else fx.Sprite(shape, p, s, Fade(color, fade), blend, spin * c.Age + Rand(c.Seed, i, 5) * MathF.Tau);
        }
    }

    /// <summary>Particles thrown out from <paramref name="at"/> at <paramref name="t0"/>, slowing as they go and fading out.</summary>
    public static void Burst(FxList fx, int seed, float age, Vector3 at, float scale, float t0, float life, int count, FxShape shape,
        Color a, Color b, float size, float speed, float gravity = 0f, FxBlend blend = FxBlend.Add, float spin = 0f, float streak = 0f, int salt = 0)
    {
        float t = age - t0;
        if (t < 0f || t > life) return;
        for (int i = 0; i < count; i++)
        {
            float own = life * (0.55f + 0.45f * Rand(seed, i, 21 + salt));
            float k = t / own;
            if (k > 1f) continue;
            var dir = Direction(seed + salt * 31, i);
            float travel = speed * scale * (0.5f + 0.7f * Rand(seed, i, 22 + salt)) * EaseOut(k);
            var p = at + dir * travel - Vector3.UnitY * gravity * scale * t * t;
            float s = size * scale * (0.6f + 0.8f * Rand(seed, i, 23 + salt)) * (1f - 0.5f * k);
            var color = Fade(Mix(a, b, Rand(seed, i, 24 + salt)), 1f - k * k);
            if (streak > 0f) fx.Streak(shape, p, dir - Vector3.UnitY * gravity * t * 0.5f, s, 1f + streak * (1f - k), color, blend);
            else fx.Sprite(shape, p, s, color, blend, Rand(seed, i, 25 + salt) * MathF.Tau + spin * t * (Rand(seed, i, 26) < 0.5f ? -1f : 1f));
        }
    }

    /// <summary>A ring growing to <paramref name="radius"/> and fading: flat on the ground, or facing the camera when <paramref name="normal"/> is zero.</summary>
    public static void Shockwave(FxList fx, float age, Vector3 center, Vector3 normal, float t0, float duration, float radius, Color color,
        FxShape shape = FxShape.Ring)
    {
        float k = (age - t0) / duration;
        if (k < 0f || k > 1f) return;
        float r = radius * (0.15f + 0.85f * EaseOut(k));
        var c = Fade(color, (1f - k) * (1f - k * 0.3f));
        if (normal == Vector3.Zero) fx.Sprite(shape, center, r, c);
        else fx.Flat(shape, center, normal, r, c);
    }

    /// <summary>A beam from the user that reaches the target in 0.1 s, holds and fades by <paramref name="t1"/>.</summary>
    public static void Beam(FxList fx, in FxCtx c, float t0, float t1, float width, Color outer, Color core, int sparks = 10)
    {
        float t = c.Age;
        if (t < t0 || t > t1) return;
        float reach = Math.Clamp((t - t0) / 0.1f, 0f, 1f);
        float fade = Math.Clamp((t1 - t) / 0.18f, 0f, 1f);
        var end = Vector3.Lerp(c.From, c.Aim, reach);
        float w0 = width * c.FromSize, w1 = width * c.ToSize;
        float pulse = 1f + 0.12f * MathF.Sin(t * 70f);
        // The beam narrows toward the user, so it reads as coming out of its body
        var mid = Vector3.Lerp(c.From, end, 0.5f);
        // A body in the beam's own colour (light alone would wash out to white over a bright field), glow round it
        // and a bright core
        fx.Ribbon(new[] { c.From, mid, end }, (w0 + w1) * 0.5f * fade * pulse, Fade(outer, 0.85f), FxBlend.Alpha);
        fx.Ribbon(new[] { c.From, mid, end }, (w0 + w1) * 0.55f * fade * pulse, Fade(outer, 0.4f));
        fx.Ribbon(new[] { c.From, mid, end }, (w0 + w1) * 0.1f * fade * pulse, Fade(core, 0.95f));
        fx.Sprite(FxShape.Glow, c.From, w0 * 1.6f * fade, Fade(outer, 0.8f));
        if (reach >= 1f)
        {
            fx.Sprite(FxShape.Glow, end, w1 * 2.6f * fade, Fade(outer, 0.85f));
            fx.Sprite(FxShape.Spark, end, w1 * 2.2f * fade, Fade(core, 0.9f), rotation: t * 3f);
        }
        // Motes running along it
        var (across, up) = Frame(c.Aim - c.From);
        for (int i = 0; i < sparks; i++)
        {
            float u = ((t - t0) * 2.4f + i / (float)sparks) % 1f;
            if (u > reach) continue;
            float swirl = u * 14f + i;
            var p = Vector3.Lerp(c.From, c.Aim, u) + (across * MathF.Cos(swirl) + up * MathF.Sin(swirl)) * (w0 + (w1 - w0) * u) * 0.7f;
            fx.Sprite(FxShape.Glow, p, (w0 + (w1 - w0) * u) * 0.35f * fade, core);
        }
    }

    /// <summary>A forked lightning bolt from <paramref name="a"/> to <paramref name="b"/>, redrawn 24 times a second and flickering.</summary>
    public static void Bolt(FxList fx, float age, int seed, Vector3 a, Vector3 b, float t0, float t1, float width, Color glow, Color core, int branches = 2)
    {
        if (age < t0 || age > t1) return;
        int frame = (int)(age * 24f);
        if (frame % 6 == 5) return;
        float fade = Math.Clamp((t1 - age) / 0.12f, 0f, 1f);
        var path = b - a;
        float len = path.Length();
        var (across, up) = Frame(path);
        const int n = 10;
        var pts = new Vector3[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float u = i / (float)n;
            float amp = MathF.Sin(u * MathF.PI) * len * 0.08f;
            pts[i] = Vector3.Lerp(a, b, u) + (across * Signed(seed + frame, i, 41) + up * Signed(seed + frame, i, 42)) * amp;
        }
        fx.Ribbon(pts, width * 3.2f * fade, Fade(glow, 0.6f));
        fx.Ribbon(pts, width * fade, core);
        for (int k = 0; k < branches; k++)
        {
            int at = 3 + (int)(Rand(seed + frame, k, 43) * (n - 5));
            var start = pts[at];
            var dir = Vector3.Normalize(path) * 0.5f + across * Signed(seed + frame, k, 44) + up * Signed(seed + frame, k, 45) * 0.6f;
            var fork = new Vector3[4];
            fork[0] = start;
            for (int j = 1; j < 4; j++)
                fork[j] = fork[j - 1] + Vector3.Normalize(dir) * len * 0.07f + (across * Signed(seed + frame, k * 7 + j, 46) + up * Signed(seed + frame, k * 7 + j, 47)) * len * 0.03f;
            fx.Ribbon(fork, width * 0.55f * fade, Fade(core, 0.85f));
        }
    }

    /// <summary>Rings rising round a Pokémon and motes spiralling up from its feet: a status move on itself.</summary>
    public static void Aura(FxList fx, float age, int seed, Vector3 feet, float h, float t0, float t1, Color color, Color light, int motes = 14)
    {
        float t = age - t0;
        float span = t1 - t0;
        if (t < 0f || t > span) return;
        float env = Envelope(t, 0f, 0.12f, span - 0.25f, span);
        for (int k = 0; k < 3; k++)
        {
            float u = ((t / span) * 1.6f + k / 3f) % 1f;
            var at = feet + Vector3.UnitY * h * (0.05f + 0.95f * u);
            fx.Flat(FxShape.Ring, at, Vector3.UnitY, h * (0.62f - 0.22f * u), Fade(color, env * (1f - u) * 0.9f), depth: true);
        }
        for (int i = 0; i < motes; i++)
        {
            float u = ((t * 0.9f + Rand(seed, i, 51)) % 1f);
            float a = Rand(seed, i, 52) * MathF.Tau + t * 3f;
            float rad = h * (0.45f + 0.1f * Rand(seed, i, 53)) * (1f - u * 0.4f);
            var p = feet + new Vector3(MathF.Cos(a) * rad, h * 1.15f * u, MathF.Sin(a) * rad * 0.6f);
            fx.Sprite(i % 3 == 0 ? FxShape.Spark : FxShape.Glow, p, h * 0.09f, Fade(Mix(color, light, Rand(seed, i, 54)), env * (1f - u)));
        }
        fx.Sprite(FxShape.Glow, feet + Vector3.UnitY * h * 0.5f, h * 0.9f, Fade(color, env * 0.35f));
    }

    /// <summary>Things falling onto <paramref name="target"/> from above between <paramref name="t0"/> and <paramref name="t1"/>.</summary>
    public static void Rain(FxList fx, float age, int seed, Vector3 target, float h, float t0, float t1, float fall, int count, FxShape shape,
        Color a, Color b, float size, FxBlend blend = FxBlend.Alpha, float streak = 0f)
    {
        for (int i = 0; i < count; i++)
        {
            float start = t0 + (t1 - t0 - fall) * Rand(seed, i, 61);
            float k = (age - start) / fall;
            if (k < 0f || k > 1f) continue;
            var top = target + new Vector3(Signed(seed, i, 62) * h * 0.9f + h * 0.4f, h * 2.6f, Signed(seed, i, 63) * h * 0.5f);
            var bottom = target + new Vector3(Signed(seed, i, 64) * h * 0.5f, -h * 0.35f, Signed(seed, i, 65) * h * 0.4f);
            var p = Vector3.Lerp(top, bottom, k * k);
            var color = Fade(Mix(a, b, Rand(seed, i, 66)), Math.Min(1f, (1f - k) * 5f));
            float s = size * h * (0.7f + 0.6f * Rand(seed, i, 67));
            if (streak > 0f) fx.Streak(shape, p, bottom - top, s, 1f + streak, color, blend);
            else fx.Sprite(shape, p, s, color, blend, Rand(seed, i, 68) * MathF.Tau + age * 4f);
        }
    }

    /// <summary>The flash of a blow landing: a jagged burst and a spark, <paramref name="size"/> times the target's height.</summary>
    public static void Contact(FxList fx, float age, int seed, Vector3 at, float h, float t0, float size, Color color, Color core)
    {
        float k = (age - t0) / 0.28f;
        if (k < 0f || k > 1f) return;
        float rot = Rand(seed, 0, 71) * MathF.Tau;
        fx.Sprite(FxShape.Glow, at, h * size * 1.3f, Fade(color, (1f - k) * 0.5f));
        fx.Sprite(FxShape.Impact, at, h * size * (0.55f + 0.6f * EaseOut(k)), Fade(color, 1f - k), rotation: rot);
        fx.Sprite(FxShape.Spark, at, h * size * 1.25f * (1f - 0.4f * k), Fade(core, 1f - k), rotation: rot * 0.5f);
    }

    /// <summary>Bright slashes swept across the target one after another.</summary>
    public static void Slash(FxList fx, float age, int seed, Vector3 at, float h, float t0, int count, float gap, Color color, Color core, FxShape shape = FxShape.Streak)
    {
        for (int i = 0; i < count; i++)
        {
            float k = (age - t0 - i * gap) / 0.24f;
            if (k < 0f || k > 1f) continue;
            float angle = -0.75f + (i % 2 == 0 ? 0f : 1.5f) + Signed(seed, i, 81) * 0.25f;
            var off = new Vector3(Signed(seed, i, 82) * h * 0.15f, Signed(seed, i, 83) * h * 0.15f, 0);
            float grow = EaseOut(k * 2.5f);
            if (shape == FxShape.Streak)
            {
                // A long thin arc of light, drawn as a streak turned across the target
                var along = new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0);
                fx.Streak(FxShape.Streak, at + off, along, h * 0.13f, 7f * grow, Fade(color, 1f - k));
                fx.Streak(FxShape.Streak, at + off, along, h * 0.06f, 7f * grow, Fade(core, 1f - k));
            }
            else fx.Sprite(shape, at + off, h * 0.55f * (0.7f + 0.3f * grow), Fade(color, 1f - k), rotation: angle - MathF.PI / 2f);
        }
    }

    /// <summary>A glowing ball flying from the user to the target with a wobble, trailing light.</summary>
    /// <param name="body">The orb's own colour, drawn solid under its glow (a dark one for shadows); the outer colour if not given.</param>
    public static void Orb(FxList fx, in FxCtx c, float t0, float t1, float size, Color outer, Color core, FxShape trail = FxShape.Glow, Color? body = null)
    {
        float span = t1 - t0;
        float t = (c.Age - t0) / span;
        if (t < 0f || t > 1f) return;
        var (across, up) = Frame(c.Aim - c.From);
        for (int i = 6; i >= 0; i--)
        {
            float u = t - i * 0.035f;
            if (u < 0f) continue;
            float e = u * u * (3f - 2f * u);
            var p = Vector3.Lerp(c.From, c.Aim, e) + (across * MathF.Sin(u * 13f) + up * MathF.Cos(u * 11f)) * c.ScaleAt(u) * 0.06f;
            float s = size * c.ScaleAt(u) * (i == 0 ? 1f : 0.7f - i * 0.08f);
            if (i == 0)
            {
                fx.Sprite(FxShape.Glow, p, s * 2.1f, Fade(outer, 0.7f));
                fx.Sprite(FxShape.Orb, p, s * 0.9f, Fade(body ?? outer, body.HasValue ? 0.95f : 0.7f), FxBlend.Alpha, c.Age * 5f);
                fx.Sprite(FxShape.Orb, p, s, Fade(outer, body.HasValue ? 0.55f : 0.8f), rotation: c.Age * 5f);
                fx.Sprite(FxShape.Glow, p, s * 0.55f, core);
            }
            else fx.Sprite(trail, p, s, Fade(outer, 0.5f - i * 0.06f), rotation: c.Age * 4f + i);
        }
    }

    /// <summary>A wall of water rolling over the ground from the user to the target, rising as it goes, and breaking over it.</summary>
    public static void WaveWall(FxList fx, in FxCtx c, float t0, float t1, Color water, Color foam)
    {
        float k = (c.Age - t0) / (t1 - t0);
        if (k < 0f || k > 1.15f) return;
        var dir = c.Cue.Missed ? c.Aim - c.From : c.ToFeet - c.FromFeet;
        dir.Y = 0f;
        var forward = Vector3.Normalize(dir);
        var (across, _) = Frame(dir);
        float travel = Math.Min(k, 1f);
        var front = Vector3.Lerp(c.FromFeet, c.FromFeet + dir, travel * travel * (3f - 2f * travel));
        float h = (c.FromSize + (c.ToSize - c.FromSize) * travel) * (0.7f + 0.8f * travel);
        // It stands tallest as it reaches the target, then collapses
        float crest = k < 0.85f ? 0.4f + 0.6f * k / 0.85f : Math.Max(0f, 1f - (k - 0.85f) / 0.3f);
        float width = (c.FromSize + (c.ToSize - c.FromSize) * travel) * 2.4f;
        float fade = Math.Clamp((1.15f - k) / 0.25f, 0f, 1f);
        for (int i = 0; i < 14; i++)
            for (int j = 0; j < 4; j++)
            {
                float u = i / 13f - 0.5f, v = j / 3f;
                float rise = h * crest * (1f - u * u * 2.4f);
                if (rise <= 0f) continue;
                var p = front + across * u * width + Vector3.UnitY * rise * v - forward * rise * 0.35f * v * v
                    + across * Signed(c.Seed, i * 4 + j, 141) * width * 0.03f;
                var color = Mix(water, foam, v * v * 0.7f);
                fx.Sprite(FxShape.Drop, p, rise * 0.32f + width * 0.04f, Fade(color, 0.85f * fade), FxBlend.Alpha, 3.14f + Signed(c.Seed, i, 142) * 0.4f);
                if (j == 3) fx.Sprite(FxShape.Glow, p + Vector3.UnitY * rise * 0.08f, rise * 0.3f, Fade(foam, 0.6f * fade));
            }
    }

    /// <summary>Rings (or arcs) travelling from the user to the target, one after another.</summary>
    public static void Waves(FxList fx, in FxCtx c, float t0, float t1, int count, FxShape shape, Color color, float size, float travel = 0.3f)
    {
        float gap = count > 1 ? (t1 - t0 - travel) / (count - 1) : 0f;
        for (int i = 0; i < count; i++)
        {
            float k = (c.Age - t0 - i * gap) / travel;
            if (k < 0f || k > 1f) continue;
            var p = Vector3.Lerp(c.From, c.Aim, k);
            float s = size * c.ScaleAt(k) * (0.5f + 0.7f * k);
            // Arcs open toward where they travel
            var dir = c.Aim - c.From;
            float rot = shape == FxShape.Wave ? MathF.Atan2(-dir.Z * 0.3f + dir.Y, dir.X) : 0f;
            fx.Sprite(shape, p, s, Fade(color, Math.Min(1f, (1f - k) * 3f)), rotation: rot);
        }
    }

    /// <summary>A sparkle that pops on a point (a glint in the eye, the shine of a hardened shell).</summary>
    public static void Glint(FxList fx, float age, Vector3 at, float h, float t0, Color color, float size = 0.45f)
    {
        float k = (age - t0) / 0.35f;
        if (k < 0f || k > 1f) return;
        float s = MathF.Sin(k * MathF.PI);
        fx.Sprite(FxShape.Spark, at, h * size * s, color, rotation: k * 1.2f);
        fx.Sprite(FxShape.Glow, at, h * size * 0.6f * s, Fade(color, 0.7f));
    }
}

/// <summary>Draws an <see cref="FxList"/> inside the 3D pass, with the default shader (rlgl batches carry no model matrix).</summary>
internal static class FxRenderer
{
    private static readonly List<int> order = new();

    public static void Draw(FxList fx, Camera3D camera)
    {
        if (fx.Count == 0) return;
        var forward = Vector3.Normalize(camera.Target - camera.Position);
        var right = Vector3.Normalize(Vector3.Cross(forward, camera.Up));
        var up = Vector3.Cross(right, forward);
        var tex = FxTextures.Atlas;

        Rlgl.DrawRenderBatchActive();
        Rlgl.DisableDepthMask();
        Rlgl.DisableBackfaceCulling();

        // Depth-tested first (rings round bodies and on the ground), then everything else over the scene; within
        // each, alpha-blended matter back to front, then light added on top
        for (int pass = 0; pass < 2; pass++)
        {
            bool depth = pass == 0;
            if (depth) Rlgl.EnableDepthTest();
            else Rlgl.DisableDepthTest();

            order.Clear();
            for (int i = 0; i < fx.Quads.Count; i++)
                if (fx.Quads[i].Depth == depth && fx.Quads[i].Blend == FxBlend.Alpha) order.Add(i);
            order.Sort((a, b) => Vector3.DistanceSquared(fx.Quads[b].Center, camera.Position)
                .CompareTo(Vector3.DistanceSquared(fx.Quads[a].Center, camera.Position)));
            Rlgl.SetBlendMode(BlendMode.Alpha);
            foreach (int i in order) Quad(fx.Quads[i], tex.Id, camera.Position, right, up);
            if (!depth) foreach (var r in fx.Ribbons) if (r.Blend == FxBlend.Alpha) Ribbon(r, tex.Id, camera.Position);
            Rlgl.DrawRenderBatchActive();

            Rlgl.SetBlendMode(BlendMode.Additive);
            foreach (var q in fx.Quads) if (q.Depth == depth && q.Blend == FxBlend.Add) Quad(q, tex.Id, camera.Position, right, up);
            if (!depth) foreach (var r in fx.Ribbons) if (r.Blend == FxBlend.Add) Ribbon(r, tex.Id, camera.Position);
            Rlgl.DrawRenderBatchActive();
        }

        Rlgl.SetTexture(0);
        Rlgl.SetBlendMode(BlendMode.Alpha);
        Rlgl.EnableDepthTest();
        Rlgl.EnableDepthMask();
        Rlgl.EnableBackfaceCulling();
    }

    private static void Quad(in FxQuad q, uint texture, Vector3 eye, Vector3 right, Vector3 up)
    {
        Vector3 ax, ay;
        if (q.Normal != Vector3.Zero)
        {
            // Flat: two axes in the plane, turned by the rotation
            var t1 = Vector3.Normalize(Vector3.Cross(q.Normal, MathF.Abs(q.Normal.Y) > 0.9f ? Vector3.UnitX : Vector3.UnitY));
            var t2 = Vector3.Cross(q.Normal, t1);
            float c = MathF.Cos(q.Rotation), s = MathF.Sin(q.Rotation);
            ax = (t1 * c + t2 * s) * q.Size;
            ay = (t2 * c - t1 * s) * q.Size;
        }
        else if (q.Along != Vector3.Zero)
        {
            // Stretched along a direction, as seen from the camera
            var view = Vector3.Normalize(q.Center - eye);
            var along = q.Along - view * Vector3.Dot(q.Along, view);
            if (along.LengthSquared() < 1e-6f) along = right;
            along = Vector3.Normalize(along);
            var across = Vector3.Normalize(Vector3.Cross(view, along));
            ax = along * q.Size * q.Stretch;
            ay = across * q.Size;
        }
        else
        {
            float c = MathF.Cos(q.Rotation), s = MathF.Sin(q.Rotation);
            ax = (right * c + up * s) * q.Size;
            ay = (up * c - right * s) * q.Size;
        }

        var (min, max) = FxTextures.UV(q.Shape);
        var p = q.Center;
        // A full batch is flushed here, which forgets its texture: set it again every time
        Rlgl.CheckRenderBatchLimit(4);
        Rlgl.SetTexture(texture);
        Rlgl.Begin(DrawMode.Quads);
        Rlgl.Color4ub(q.Color.R, q.Color.G, q.Color.B, q.Color.A);
        Rlgl.TexCoord2f(min.X, max.Y); Rlgl.Vertex3f(p.X - ax.X - ay.X, p.Y - ax.Y - ay.Y, p.Z - ax.Z - ay.Z);
        Rlgl.TexCoord2f(max.X, max.Y); Rlgl.Vertex3f(p.X + ax.X - ay.X, p.Y + ax.Y - ay.Y, p.Z + ax.Z - ay.Z);
        Rlgl.TexCoord2f(max.X, min.Y); Rlgl.Vertex3f(p.X + ax.X + ay.X, p.Y + ax.Y + ay.Y, p.Z + ax.Z + ay.Z);
        Rlgl.TexCoord2f(min.X, min.Y); Rlgl.Vertex3f(p.X - ax.X + ay.X, p.Y - ax.Y + ay.Y, p.Z - ax.Z + ay.Z);
        Rlgl.End();
    }

    private static void Ribbon(FxRibbon r, uint texture, Vector3 eye)
    {
        var (min, max) = FxTextures.UV(r.Shape);
        float half = r.Width * 0.5f;
        for (int i = 0; i + 1 < r.Points.Length; i++)
        {
            var a = r.Points[i];
            var b = r.Points[i + 1];
            // Each joint's side vector averages its segments, so the strip doesn't crack at the bends
            var sa = Side(r.Points, i, eye) * half;
            var sb = Side(r.Points, i + 1, eye) * half;
            float u0 = min.X + (max.X - min.X) * 0.5f;
            Rlgl.CheckRenderBatchLimit(4);
            Rlgl.SetTexture(texture);
            Rlgl.Begin(DrawMode.Quads);
            Rlgl.Color4ub(r.Color.R, r.Color.G, r.Color.B, r.Color.A);
            Rlgl.TexCoord2f(u0, max.Y); Rlgl.Vertex3f(a.X - sa.X, a.Y - sa.Y, a.Z - sa.Z);
            Rlgl.TexCoord2f(u0, max.Y); Rlgl.Vertex3f(b.X - sb.X, b.Y - sb.Y, b.Z - sb.Z);
            Rlgl.TexCoord2f(u0, min.Y); Rlgl.Vertex3f(b.X + sb.X, b.Y + sb.Y, b.Z + sb.Z);
            Rlgl.TexCoord2f(u0, min.Y); Rlgl.Vertex3f(a.X + sa.X, a.Y + sa.Y, a.Z + sa.Z);
            Rlgl.End();
        }
    }

    private static Vector3 Side(Vector3[] pts, int i, Vector3 eye)
    {
        var dir = i == 0 ? pts[1] - pts[0] : i == pts.Length - 1 ? pts[i] - pts[i - 1] : pts[i + 1] - pts[i - 1];
        var view = pts[i] - eye;
        var side = Vector3.Cross(dir, view);
        return side.LengthSquared() < 1e-10f ? Vector3.UnitY : Vector3.Normalize(side);
    }
}
