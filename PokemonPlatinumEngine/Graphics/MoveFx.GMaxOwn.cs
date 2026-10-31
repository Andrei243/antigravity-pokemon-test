using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

// The Gigantamax signatures, part a (plan 25 · YD7; style guide, "Dynamax, Max Moves and Terastallization", "The
// Gigantamax signatures"): five G-Max Moves given set pieces of their own in place of their type's piece and their
// mark (G-Max Wildfire, Volt Crash, Terror, Resonance and Replenish), and every Gigantamax growth's clouds in its own
// G-Max Move's two colours. All of it our own. A signature lays over the Max Moves' shared beats (MaxCommon: the pulse,
// the crossing, the landing at ImpactTime, the finisher's shatter and the red glow), is faded by MaxMoveTime with
// SetPiece's envelope, and stays under MaxQuads with them at every age; pure in the cue's shown age and seed; no
// flash over 0.35, no shake over 0.5; nothing large over 1.3 h above the target's feet.
internal static partial class MoveFx
{
    /// <summary>The G-Max Moves with a set piece of their own (part a: five; part b adds five more).</summary>
    private static readonly Dictionary<string, FxRecipe> GMaxSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        ["G-Max Wildfire"] = GMaxWildfire,
        ["G-Max Volt Crash"] = GMaxVoltCrash,
        ["G-Max Terror"] = GMaxTerror,
        ["G-Max Resonance"] = GMaxResonance,
        ["G-Max Replenish"] = GMaxReplenish
    };

    /// <summary>The G-Max Moves with a set piece of their own (plan 25 · YD7).</summary>
    public static IEnumerable<string> GMaxSignatureMoves => GMaxSignatures.Keys;

    // ------------------------------------------------------------------ a Gigantamax's own clouds

    /// <summary>
    /// A Gigantamax's cloud (plan 25 · YD7): its G-Max Move's two colours by turns, deepened a third toward the
    /// cloud's dark as the Dynamax's red is, a colour too dark to read lifted halfway to the other; null for no G-Max Move.
    /// </summary>
    private static Color? GMaxCloud(string? gmax, int i)
    {
        if (gmax == null || !GMaxMarks.TryGetValue(gmax, out var m)) return null;
        var (a, b) = Readable(m.A, m.B);
        return Fx.Mix(DynaCloud, i % 2 == 0 ? a : b, 0.65f);
    }

    /// <summary>A Gigantamax cloud's light (plan 25 · YD7): the brighter of its two colours, a fifth toward white, the other by turns where it reads.</summary>
    private static Color? GMaxGlow(string? gmax, int i)
    {
        if (gmax == null || !GMaxMarks.TryGetValue(gmax, out var m)) return null;
        var (a, b) = Readable(m.A, m.B);
        bool aBrighter = Luma(a) >= Luma(b);
        var bright = aBrighter ? a : b;
        var other = aBrighter ? b : a;
        var pick = i % 2 == 0 || Luma(other) < 110f ? bright : other;
        return Fx.Mix(pick, White, 0.2f);
    }

    /// <summary>The colours a G-Max Move's growth takes, for tests and the harness (null for none): the first cloud's, the second's and the light.</summary>
    public static (Color A, Color B, Color Light)? GMaxGrowthColours(string? gmax) =>
        GMaxCloud(gmax, 0) is { } a && GMaxCloud(gmax, 1) is { } b && GMaxGlow(gmax, 0) is { } l ? (a, b, l) : null;

    private static float Luma(Color c) => 0.3f * c.R + 0.59f * c.G + 0.11f * c.B;

    /// <summary>A pair of a mark's colours with one too dark to read in a cloud (Terror's black, One Blow's) lifted halfway to the other.</summary>
    private static (Color A, Color B) Readable(Color a, Color b)
    {
        if (Luma(a) < 70f) a = Fx.Mix(a, b, 0.5f);
        if (Luma(b) < 70f) b = Fx.Mix(b, a, 0.5f);
        return (a, b);
    }

    // ------------------------------------------------------------------ the five signatures

    /// <summary>
    /// G-Max Wildfire (Charizard): a ring of ten walls of flame springs up round the target from the ground and roars
    /// up to its shoulders, embers lift off them, the ground inside burns orange and flames lie burning round its feet
    /// as the walls die down. About 40 quads.
    /// </summary>
    private static void GMaxWildfire(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        if (e <= 0f) return;
        var feet = c.ToFeet;
        int s = c.Seed;
        var orange = Rgb(255, 120, 40);
        var gold = Rgb(255, 210, 90);
        var deep = Rgb(200, 50, 30);
        float ring = w + h * 0.35f;
        // The ground burning
        fx.Flat(FxShape.Glow, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, ring * 1.5f, Fx.Fade(deep, e * 0.6f), FxBlend.Alpha);
        fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, ring * 1.05f, Fx.Fade(orange, e), rotation: t * 2f);
        // The walls of flame: two flames stacked on each of ten places round the feet, springing up one after another
        for (int i = 0; i < 10; i++)
        {
            var at = AroundFeet(c, i, 10, ring, 0.15f);
            float rise = Fx.EaseOut(Math.Clamp((t - I - 0.025f * i) / 0.22f, 0f, 1f));
            if (rise <= 0f) continue;
            float tall = h * 0.85f * rise * (0.85f + 0.15f * MathF.Sin(t * 13f + i * 1.7f)) * (0.4f + 0.6f * e);
            bool behind = at.Z < feet.Z;
            fx.Sprite(FxShape.Flame, at + Vector3.UnitY * tall * 0.3f, h * 0.3f * rise, Fx.Fade(i % 2 == 0 ? orange : deep, e), FxBlend.Alpha, 0f, depth: behind);
            fx.Sprite(FxShape.Flame, at + Vector3.UnitY * tall * 0.75f, h * 0.22f * rise, Fx.Fade(gold, e * 0.9f), rotation: 0.15f * MathF.Sin(t * 9f + i), depth: behind);
        }
        // Embers lifting off the walls
        for (int i = 0; i < 10; i++)
        {
            float u = (t * 1.3f + Fx.Rand(s, i, 701)) % 1f;
            var at = AroundFeet(c, i, 10, ring * (0.9f + 0.2f * Fx.Rand(s, i, 702)), 0.45f);
            var p = at + new Vector3(Fx.Signed(s, i, 703) * h * 0.1f * u, h * (0.4f + 0.8f * u), 0f);
            fx.Sprite(FxShape.Ember, p, h * 0.05f, Fx.Fade(i % 3 == 0 ? gold : orange, e * MathF.Sin(u * MathF.PI)), depth: at.Z < feet.Z);
        }
        // Flames lying burning round its feet as the walls die down
        float lie = Fx.Envelope(t, I + 0.45f, I + 0.6f, 1.2f, 1.4f);
        if (lie > 0f)
            for (int i = 0; i < 6; i++)
            {
                var at = AroundFeet(c, i, 6, ring * 0.7f, 0.5f);
                fx.Flat(FxShape.Flame, at + Vector3.UnitY * h * 0.04f, Vector3.UnitY, h * 0.2f, Fx.Fade(i % 2 == 0 ? orange : gold, lie), FxBlend.Add, i + t * 2f);
            }
    }

    /// <summary>
    /// G-Max Volt Crash (Pikachu): a great bolt strikes down out of the sky on the target at the impact with a yellow
    /// flash of 0.22, six bolts run out from its feet along the ground and crackle there, crosses of light fall round it,
    /// and a disc of yellow light lies under it. About 34 quads and ribbons.
    /// </summary>
    private static void GMaxVoltCrash(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        if (e <= 0f) return;
        var feet = c.ToFeet;
        var mid = c.To;
        int s = c.Seed;
        var yellow = Rgb(255, 230, 60);
        var pale = Rgb(255, 255, 210);
        // The great bolt out of the sky, and its strike
        Fx.Bolt(fx, t, s + 711, feet + new Vector3(h * 0.2f, h * 2.6f, 0f), mid + Vector3.UnitY * h * 0.3f, I, I + 0.32f, h * 0.09f, yellow, pale, 2);
        fx.ScreenFlash(pale, 0.22f * Fx.Envelope(t, I, I + 0.02f, I + 0.06f, I + 0.24f));
        fx.Sprite(FxShape.Glow, mid + Vector3.UnitY * h * 0.3f, h * 0.9f * Fx.Envelope(t, I, I + 0.03f, I + 0.15f, I + 0.45f), Fx.Fade(yellow, 0.8f));
        // Six bolts running out along the ground from its feet, one after another, crackling
        for (int j = 0; j < 6; j++)
        {
            float a = j * MathF.Tau / 6f + 0.3f;
            var from = feet + Vector3.UnitY * h * 0.05f;
            var to = from + new Vector3(MathF.Cos(a), 0f, MathF.Sin(a) * 0.6f) * (w + h * 1.1f);
            float t0 = I + 0.08f + j * 0.05f;
            Fx.Bolt(fx, t, s + 731 + j * 13, from, to, t0, t0 + 0.55f, h * 0.035f, yellow, pale, 0);
        }
        // The ground lit under it
        fx.Flat(FxShape.Glow, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, w + h * 0.9f, Fx.Fade(yellow, e * 0.55f));
        fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, w + h * 0.5f, Fx.Fade(pale, e), rotation: -t * 6f);
        // Crosses of light falling round it
        Fx.Rain(fx, t, s + 741, mid, h, I + 0.15f, 1.35f, 0.3f, 10, FxShape.Cross, yellow, pale, 0.1f, FxBlend.Add);
    }

    /// <summary>
    /// G-Max Terror (Gengar): a pool of shadow spreads under the target, six hands of shadow rise out of it round the
    /// target and close on it, wisps of violet circle it, and a great eye opens over it, held and closing. About 26 quads.
    /// </summary>
    private static void GMaxTerror(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        if (e <= 0f) return;
        var feet = c.ToFeet;
        var violet = Rgb(150, 80, 220);
        var shadow = Rgb(36, 18, 54);
        var lilac = Rgb(210, 170, 255);
        float spread = Fx.EaseOut(Math.Clamp((t - I) / 0.3f, 0f, 1f));
        float ring = w + h * 0.3f;
        // The pool of shadow
        fx.Flat(FxShape.Glow, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, ring * 1.7f * spread, Fx.Fade(shadow, e * 0.9f), FxBlend.Alpha);
        fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, ring * 1.2f * spread, Fx.Fade(violet, e * 0.8f), rotation: -t * 1.5f);
        // Six hands rising out of it and closing on the target
        for (int i = 0; i < 6; i++)
        {
            float k = Math.Clamp((t - I - 0.1f - 0.04f * i) / 0.35f, 0f, 1f);
            if (k <= 0f) continue;
            float close = Fx.EaseOut(Math.Clamp((t - I - 0.5f) / 0.35f, 0f, 1f));
            var at = AroundFeet(c, i, 6, ring * (1.1f - 0.45f * close), 0.5f);
            float a = i * MathF.Tau / 6f + 0.5f;
            var p = at + Vector3.UnitY * h * (0.1f + 0.55f * Fx.EaseOut(k));
            bool behind = at.Z < feet.Z;
            // Leaning in toward the target as they close
            float lean = MathF.Cos(a) * 0.6f * close;
            fx.Sprite(FxShape.Glow, p, h * 0.2f, Fx.Fade(violet, e * 0.5f), depth: behind);
            fx.Sprite(FxShape.Palm, p, h * 0.2f, Fx.Fade(shadow, e * 0.95f), FxBlend.Alpha, lean, depth: behind);
        }
        // Wisps circling
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + t * 2.6f;
            var p = feet + new Vector3(MathF.Cos(a) * ring * 0.9f, h * (0.5f + 0.2f * MathF.Sin(a * 2f + t * 3f)), MathF.Sin(a) * ring * 0.55f);
            fx.Sprite(FxShape.Wisp, p, h * 0.12f, Fx.Fade(i % 2 == 0 ? violet : lilac, e * 0.9f), rotation: a, depth: MathF.Sin(a) < 0f);
        }
        // The great eye opening over it, held, closing (its size breathing as it opens)
        float open = Fx.Envelope(t, I + 0.2f, I + 0.45f, 1.15f, 1.35f);
        if (open > 0f)
        {
            var at = feet + Vector3.UnitY * h * 1.08f;
            fx.Sprite(FxShape.Glow, at, h * 0.45f * open, Fx.Fade(violet, open * 0.55f));
            fx.Sprite(FxShape.Eye, at, h * 0.32f * open, Fx.Fade(lilac, open), FxBlend.Alpha, 0.06f * MathF.Sin(t * 5f));
            fx.Sprite(FxShape.Dot, at, h * 0.08f * open, Fx.Fade(Rgb(255, 60, 90), open), rotation: 0f);
        }
    }

    /// <summary>
    /// G-Max Resonance (Lapras): three curtains of aurora, pale green, sea-blue and violet, ripple out across the sky
    /// over the target, rings of sound rise round it, snowflakes turn round it and motes of light fall from the
    /// curtains. About 32 quads and ribbons.
    /// </summary>
    private static void GMaxResonance(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        if (e <= 0f) return;
        var feet = c.ToFeet;
        int s = c.Seed;
        var green = Rgb(150, 240, 190);
        var blue = Rgb(150, 210, 255);
        var violet = Rgb(190, 160, 255);
        // The curtains, unrolling out from over the target and waving
        float unroll = Fx.EaseOut(Math.Clamp((t - I) / 0.4f, 0f, 1f));
        Color[] colours = { green, blue, violet };
        for (int k = 0; k < 3; k++)
        {
            var pts = new Vector3[12];
            float y = h * (1.0f + 0.1f * k);
            float span = (w + h * 1.6f) * unroll;
            for (int i = 0; i < pts.Length; i++)
            {
                float u = i / (pts.Length - 1f) * 2f - 1f;
                pts[i] = feet + new Vector3(u * span, y + h * 0.06f * MathF.Sin(u * 4f + t * 3f + k), -h * (0.25f + 0.15f * k) + h * 0.12f * MathF.Sin(u * 3f - t * 2f + k * 1.3f));
            }
            // A wide, soft curtain of the colour and a bright seam along it (a thin band read as a streak of light, not an aurora)
            fx.Ribbon(pts, h * 0.42f, Fx.Fade(colours[k], e * 0.32f), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.1f, Fx.Fade(colours[k], e * 0.75f));
        }
        // Rings of sound rising round it
        for (int j = 0; j < 4; j++)
        {
            float u = ((t - I) * 1.1f + j / 4f) % 1f;
            if (t < I) break;
            fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * (0.05f + 0.9f * u), Vector3.UnitY, (w + h * 0.2f) * (0.8f + 0.5f * u),
                Fx.Fade(j % 2 == 0 ? green : blue, e * (1f - u) * 0.9f), rotation: j, depth: false);
        }
        // Snowflakes turning round it
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + t * 1.8f;
            float r = w + h * 0.3f;
            var p = feet + new Vector3(MathF.Cos(a) * r, h * (0.3f + 0.25f * MathF.Sin(a * 3f + t)), MathF.Sin(a) * r * 0.6f);
            fx.Sprite(FxShape.Snowflake, p, h * 0.09f, Fx.Fade(i % 2 == 0 ? White : blue, e), FxBlend.Alpha, a + t * 2f, depth: MathF.Sin(a) < 0f);
        }
        // Motes falling from the curtains
        for (int i = 0; i < 8; i++)
        {
            float u = (t * 0.9f + Fx.Rand(s, i, 751)) % 1f;
            var p = feet + new Vector3(Fx.Signed(s, i, 752) * (w + h * 1.2f) * unroll, h * (1.05f - 0.9f * u), -h * 0.2f);
            fx.Sprite(FxShape.Dot, p, h * 0.04f, Fx.Fade(colours[i % 3], e * MathF.Sin(u * MathF.PI)));
        }
    }

    /// <summary>The berries G-Max Replenish throws: red, violet, blue, gold and pink, our own.</summary>
    private static readonly Color[] Berries = { Rgb(220, 60, 70), Rgb(150, 80, 190), Rgb(70, 120, 220), Rgb(240, 190, 60), Rgb(250, 130, 170) };

    /// <summary>
    /// G-Max Replenish (Snorlax): a burst of berries thrown up out of the target that arc out and land round it, each
    /// with a puff where it lands, leaves drifting down, and berries popping up round the user's own feet in a green
    /// glow (the move gives its side back what it ate). About 40 quads.
    /// </summary>
    private static void GMaxReplenish(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        if (e <= 0f) return;
        var feet = c.ToFeet;
        var mid = c.To;
        int s = c.Seed;
        var green = Rgb(90, 170, 70);
        var leaf = Rgb(130, 200, 90);
        // Fourteen berries thrown up out of it on arcs, landing round it and bouncing once
        for (int i = 0; i < 14; i++)
        {
            float t0 = I + 0.05f + 0.02f * i;
            float k = (t - t0) / 0.55f;
            if (k < 0f) continue;
            float a = i * MathF.Tau / 14f + Fx.Signed(s, i, 761) * 0.2f;
            float reach = (w + h * (0.5f + 0.4f * Fx.Rand(s, i, 762)));
            var colour = Berries[i % Berries.Length];
            bool behind = MathF.Sin(a) < 0f;
            if (k < 1f)
            {
                var start = mid + Vector3.UnitY * h * 0.2f;
                var end = feet + new Vector3(MathF.Cos(a) * reach, h * 0.06f, MathF.Sin(a) * reach * 0.6f);
                var p = Vector3.Lerp(start, end, k) + Vector3.UnitY * h * 0.7f * 4f * k * (1f - k);
                fx.Sprite(FxShape.Seed, p, h * 0.11f, Fx.Fade(colour, e), FxBlend.Alpha, k * 5f + i, depth: behind);
            }
            else
            {
                // A small bounce and lying there
                float b = k - 1f;
                float hop = b < 0.3f ? MathF.Sin(b / 0.3f * MathF.PI) * h * 0.08f : 0f;
                var p = feet + new Vector3(MathF.Cos(a) * reach, h * 0.06f + hop, MathF.Sin(a) * reach * 0.6f);
                fx.Sprite(FxShape.Seed, p, h * 0.11f, Fx.Fade(colour, e), FxBlend.Alpha, 5f + i, depth: behind);
                if (i % 2 == 0) Fx.Shockwave(fx, t, p - Vector3.UnitY * h * 0.04f, Vector3.UnitY, t0 + 0.55f, 0.3f, h * 0.2f, Fx.Fade(leaf, 0.8f));
            }
        }
        // Leaves drifting down round it
        for (int i = 0; i < 6; i++)
        {
            float u = (t * 0.8f + Fx.Rand(s, i, 763)) % 1f;
            var p = feet + new Vector3(Fx.Signed(s, i, 764) * (w + h * 0.6f) + 0.15f * h * MathF.Sin(u * 9f), h * (1.1f - u), Fx.Signed(s, i, 765) * h * 0.3f);
            fx.Sprite(FxShape.Leaf, p, h * 0.08f, Fx.Fade(i % 2 == 0 ? green : leaf, e * MathF.Sin(u * MathF.PI)), FxBlend.Alpha, u * 6f + i);
        }
        // Back on the user's side: berries popping up round its feet in a green glow
        float back = Fx.Envelope(t, I + 0.4f, I + 0.55f, 1.2f, 1.4f);
        if (back > 0f && !c.Cue.OnSelf)
        {
            float hu = c.FromSize, hw = c.FromHalfWidth;
            fx.Flat(FxShape.Glow, c.FromFeet + Vector3.UnitY * hu * 0.02f, Vector3.UnitY, hw + hu * 0.5f, Fx.Fade(green, back * 0.55f));
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f + 0.4f;
                float k = Math.Clamp((t - I - 0.45f - i * 0.05f) / 0.25f, 0f, 1f);
                var p = c.FromFeet + new Vector3(MathF.Cos(a) * (hw + hu * 0.2f), hu * (0.05f + 0.25f * MathF.Sin(k * MathF.PI)), MathF.Sin(a) * (hw + hu * 0.2f) * 0.6f);
                fx.Sprite(FxShape.Seed, p, hu * 0.1f * k, Fx.Fade(Berries[i], back), FxBlend.Alpha, i, depth: MathF.Sin(a) < 0f);
            }
        }
    }
}
