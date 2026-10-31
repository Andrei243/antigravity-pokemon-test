using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

// The Gigantamax signatures (plan 25 · YD7; style guide, "Dynamax, Max Moves and Terastallization", "The
// Gigantamax signatures"): ten G-Max Moves given set pieces of their own in place of their type's piece and their
// mark (part a: G-Max Wildfire, Volt Crash, Terror, Resonance and Replenish; part b: Chi Strike, Gold Rush, Cuddle,
// Befuddle and Depletion), and every Gigantamax growth's clouds in its own
// G-Max Move's two colours. All of it our own. A signature lays over the Max Moves' shared beats (MaxCommon: the pulse,
// the crossing, the landing at ImpactTime, the finisher's shatter and the red glow), is faded by MaxMoveTime with
// SetPiece's envelope, and stays under MaxQuads with them at every age; pure in the cue's shown age and seed; no
// flash over 0.35, no shake over 0.5; nothing large over 1.3 h above the target's feet.
internal static partial class MoveFx
{
    /// <summary>The G-Max Moves with a set piece of their own (ten: part a's five and part b's).</summary>
    private static readonly Dictionary<string, FxRecipe> GMaxSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        ["G-Max Wildfire"] = GMaxWildfire,
        ["G-Max Volt Crash"] = GMaxVoltCrash,
        ["G-Max Terror"] = GMaxTerror,
        ["G-Max Resonance"] = GMaxResonance,
        ["G-Max Replenish"] = GMaxReplenish,
        // Part b
        ["G-Max Chi Strike"] = GMaxChiStrike,
        ["G-Max Gold Rush"] = GMaxGoldRush,
        ["G-Max Cuddle"] = GMaxCuddle,
        ["G-Max Befuddle"] = GMaxBefuddle,
        ["G-Max Depletion"] = GMaxDepletion
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
        // The curtains: each eight upright folds along a waving line, opening out from over the target. A band along
        // the line (part a's first look) read as a streak across the trees; an aurora hangs, so its folds stand up.
        float unroll = Fx.EaseOut(Math.Clamp((t - I) / 0.4f, 0f, 1f));
        Color[] colours = { green, blue, violet };
        const int Folds = 8;
        for (int k = 0; k < 3; k++)
        {
            float span = (w + h * (1.2f + 0.25f * k)) * unroll;
            float back = -h * (0.3f + 0.18f * k);
            float foot = h * (0.75f + 0.12f * k);
            for (int i = 0; i < Folds; i++)
            {
                float u = (i + 0.5f) / Folds * 2f - 1f;
                float sway = h * 0.07f * MathF.Sin(t * 2.2f + i * 1.3f + k * 2f);
                var at = feet + new Vector3(u * span + sway, foot + h * 0.06f * MathF.Sin(u * 4f + t * 3f + k),
                    back + h * 0.12f * MathF.Sin(u * 3f - t * 2f + k * 1.3f));
                float beat = 0.7f + 0.3f * MathF.Sin(t * 4f + i * 2.1f + k);
                // A soft fold standing three times as tall as wide, its foot on the curtain's waving line. No seam
                // along the foot: a line of any width along the curtain read as a streak again.
                float fw = Math.Max(h * 0.09f, span / Folds * 0.85f);
                fx.Streak(FxShape.Glow, at + Vector3.UnitY * fw * 2.2f, Vector3.UnitY, fw, 3f, Fx.Fade(colours[k], e * beat), FxBlend.Alpha);
            }
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
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + t * 1.8f;
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

    // ------------------------------------------------------------------ part b

    /// <summary>
    /// G-Max Chi Strike (Machamp): four great fists of gold light drive in on the target from four sides at mid height,
    /// 0.07 s apart, each trailing light and landing with a white-gold impact; at the fourth, a burst of gold sparks and
    /// an upright ring round its middle; then five gold stars circle over its head. About 32 quads.
    /// </summary>
    private static void GMaxChiStrike(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        if (e <= 0f) return;
        var mid = c.To;
        int s = c.Seed;
        var gold = Rgb(255, 210, 80);
        var pale = Rgb(255, 250, 220);
        float reach = w + h * 0.9f;
        for (int j = 0; j < 4; j++)
        {
            // From the four diagonals, the front two drawn over the body and the back two behind it
            float a = j * MathF.Tau / 4f + MathF.PI / 4f;
            var dir = new Vector3(MathF.Cos(a), 0.25f * (j % 2 == 0 ? 1f : -0.6f), MathF.Sin(a) * 0.6f);
            float t0 = I + 0.02f + 0.07f * j;
            float k = Math.Clamp((t - t0) / 0.14f, 0f, 1f);
            float fade = Fx.Envelope(t, t0 - 0.02f, t0, t0 + 0.32f, t0 + 0.55f);
            if (fade <= 0f) continue;
            bool behind = MathF.Sin(a) < 0f;
            var at = mid + dir * (reach * (1f - Fx.EaseOut(k)) + w * 0.6f);
            fx.Streak(FxShape.Glow, at + dir * h * 0.2f, dir, h * 0.12f, 2.5f, Fx.Fade(gold, fade * 0.6f), depth: behind);
            fx.Sprite(FxShape.Fist, at, h * 0.22f, Fx.Fade(gold, fade), FxBlend.Alpha, a + MathF.PI, depth: behind);
            fx.Sprite(FxShape.Fist, at, h * 0.15f, Fx.Fade(pale, fade * 0.45f), rotation: a + MathF.PI, depth: behind);
            float hit = Fx.Envelope(t, t0 + 0.13f, t0 + 0.15f, t0 + 0.2f, t0 + 0.32f);
            if (hit > 0f) fx.Sprite(FxShape.Impact, mid + dir * w * 0.6f, h * 0.35f * hit, Fx.Fade(pale, hit), rotation: a, depth: behind);
        }
        float last = I + 0.02f + 0.21f + 0.14f;
        Fx.Burst(fx, s, t, mid, h, last, 0.6f, 10, FxShape.Spark, gold, pale, 0.07f, 1.1f, salt: 771);
        // An upright ring of gold spreading round its middle, facing the camera's side
        Fx.Shockwave(fx, t, mid, Vector3.UnitZ, last, 0.5f, h * 1.3f, Fx.Fade(gold, 0.9f));
        // Five gold stars circling over its head: its blows strike true now
        float crown = Fx.Envelope(t, last + 0.1f, last + 0.3f, 1.2f, 1.4f);
        if (crown > 0f)
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f + t * 3f;
                var p = c.ToFeet + new Vector3(MathF.Cos(a) * (w * 0.7f + h * 0.15f), h * (1.0f + 0.08f * MathF.Sin(a * 2f + t * 4f)), MathF.Sin(a) * (w * 0.7f + h * 0.15f) * 0.5f);
                fx.Sprite(FxShape.Star, p, h * 0.09f, Fx.Fade(i % 2 == 0 ? gold : pale, crown), rotation: t * 4f + i, depth: MathF.Sin(a) < 0f);
            }
    }

    /// <summary>
    /// G-Max Gold Rush (Meowth): eighteen gold coins rain out of the sky round the target, each spinning to its edge and
    /// back, landing flat round its feet in a growing heap; glints wink on the heap and the ground glows gold. About 34 quads.
    /// </summary>
    private static void GMaxGoldRush(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        if (e <= 0f) return;
        var feet = c.ToFeet;
        int s = c.Seed;
        var gold = Rgb(255, 200, 60);
        var rim = Rgb(200, 140, 30);
        var glint = Rgb(255, 250, 210);
        fx.Flat(FxShape.Glow, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, w + h * 1.0f, Fx.Fade(gold, e * 0.45f), FxBlend.Alpha);
        for (int i = 0; i < 18; i++)
        {
            float t0 = I + 0.02f + 0.035f * i;
            float k = (t - t0) / 0.4f;
            if (k < 0f) continue;
            // A heap, not a scatter: the coins land close before its feet (the camera's side, where the body hides none),
            // the later ones nearer and higher on the pile
            float a = (0.12f + 0.76f * Fx.Rand(s, i, 781)) * MathF.PI;
            float pile = i / 17f;
            float r = w * 0.55f + h * (0.12f + 0.5f * Fx.Rand(s, i, 782)) * (1f - 0.5f * pile);
            var ground = feet + new Vector3(MathF.Cos(a) * r, h * (0.03f + 0.09f * pile), MathF.Sin(a) * r * 0.6f);
            bool behind = MathF.Sin(a) < 0f;
            if (k < 1f)
            {
                // Falling from 1.25 h on a slant, spinning to its edge and back
                var p = ground + new Vector3(h * 0.25f * (1f - k), h * 1.2f * (1f - k * k), 0f);
                float spin = Math.Max(0.2f, MathF.Abs(MathF.Cos(k * 9f + i)));
                fx.Streak(FxShape.Dot, p, Vector3.UnitY, h * 0.1f * spin, 1f / spin, Fx.Fade(rim, e), FxBlend.Alpha, depth: behind);
                fx.Streak(FxShape.Dot, p, Vector3.UnitY, h * 0.075f * spin, 1f / spin, Fx.Fade(gold, e), FxBlend.Alpha, depth: behind);
            }
            else
            {
                // Lying in the heap, tipped a little as a coin on a pile lies, on its darker rim
                var tip = Vector3.Normalize(Vector3.UnitY + new Vector3(Fx.Signed(s, i, 783), 0f, Fx.Signed(s, i, 784)) * 0.45f);
                fx.Flat(FxShape.Dot, ground - Vector3.UnitY * h * 0.012f, tip, h * 0.16f, Fx.Fade(rim, e), FxBlend.Alpha, i);
                fx.Flat(FxShape.Dot, ground, tip, h * 0.13f, Fx.Fade(gold, e), FxBlend.Alpha, i);
                if (i % 3 == 0)
                {
                    float wink = MathF.Max(0f, MathF.Sin((t - t0) * 7f + i));
                    fx.Sprite(FxShape.Star, ground + Vector3.UnitY * h * 0.05f, h * 0.1f * wink, Fx.Fade(glint, e * wink), rotation: t * 2f);
                }
            }
        }
    }

    /// <summary>
    /// G-Max Cuddle (Eevee): twelve pink hearts float in from round the stage and gather into a ring that hugs the target at
    /// its middle, squeezing twice; then a great soft heart of pink light blooms over its body with sparkles round it, and
    /// four little hearts pop up over its head as the hug lets go. About 28 quads.
    /// </summary>
    private static void GMaxCuddle(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        if (e <= 0f) return;
        var feet = c.ToFeet;
        int s = c.Seed;
        var pink = Rgb(255, 150, 190);
        var cream = Rgb(240, 214, 170);
        var deep = Rgb(240, 100, 150);
        float hugY = h * 0.5f;
        float gather = Fx.EaseOut(Math.Clamp((t - I) / 0.35f, 0f, 1f));
        // The hug's squeeze: twice between 0.7 and 1.0 s, letting go after 1.05 s
        float squeeze = t > I + 0.35f && t < I + 0.7f ? 0.15f * MathF.Max(0f, MathF.Sin((t - I - 0.35f) / 0.35f * MathF.Tau * 2f)) : 0f;
        float letGo = Fx.EaseOut(Math.Clamp((t - I - 0.7f) / 0.3f, 0f, 1f));
        float ring = (w + h * 0.25f) * (1f - squeeze) * (1f + 0.4f * letGo);
        float far = w + h * 1.6f;
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f + 0.2f + (1f - gather) * 0.8f;
            float r = far + (ring - far) * gather;
            var p = feet + new Vector3(MathF.Cos(a) * r, hugY + h * 0.05f * MathF.Sin(t * 5f + i) + (1f - gather) * h * 0.3f * Fx.Signed(s, i, 791), MathF.Sin(a) * r * 0.6f);
            fx.Sprite(FxShape.Heart, p, h * 0.12f, Fx.Fade(i % 3 == 0 ? cream : pink, e * (1f - 0.7f * letGo)), FxBlend.Alpha, 0.2f * MathF.Sin(t * 4f + i), depth: MathF.Sin(a) < 0f);
        }
        // The great soft heart blooming over its body, and sparkles round it
        float bloom = Fx.Envelope(t, I + 0.55f, I + 0.75f, 1.15f, 1.38f);
        if (bloom > 0f)
        {
            var at = feet + Vector3.UnitY * h * 0.55f + Vector3.UnitZ * (w * 0.3f);
            fx.Sprite(FxShape.Glow, at, h * 0.6f * bloom, Fx.Fade(pink, bloom * 0.45f), FxBlend.Alpha);
            fx.Sprite(FxShape.Heart, at, h * 0.38f * (0.85f + 0.15f * bloom), Fx.Fade(deep, bloom * 0.55f), FxBlend.Alpha);
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + t;
                var p = at + new Vector3(MathF.Cos(a) * h * 0.45f, MathF.Sin(a) * h * 0.4f, 0f);
                fx.Sprite(FxShape.Star, p, h * 0.06f * bloom, Fx.Fade(cream, bloom), rotation: t * 3f + i);
            }
        }
        // Four little hearts popping up over its head as it lets go
        for (int i = 0; i < 4; i++)
        {
            float k = Math.Clamp((t - I - 0.75f - 0.06f * i) / 0.45f, 0f, 1f);
            if (k <= 0f) continue;
            var p = feet + new Vector3((i - 1.5f) * h * 0.18f, h * (0.85f + 0.25f * Fx.EaseOut(k)), 0f);
            fx.Sprite(FxShape.Heart, p, h * 0.07f * MathF.Sin(MathF.Min(1f, k * 2f) * MathF.PI * 0.5f), Fx.Fade(deep, e * (1f - k * k)), FxBlend.Alpha);
        }
    }

    /// <summary>
    /// G-Max Befuddle (Butterfree): a pair of great wings of violet light opens behind the target and beats twice; a falling
    /// spiral of twenty-four glittering scales in the three colours of what it may bring (poison, paralysis, sleep) turns
    /// round it; and, as they settle, a bubble, a spark and a Z pop over its head one after another. About 34 quads.
    /// </summary>
    private static void GMaxBefuddle(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        if (e <= 0f) return;
        var feet = c.ToFeet;
        var violet = Rgb(176, 120, 230);
        var gold = Rgb(255, 214, 90);
        Color[] statuses = { Rgb(190, 90, 220), Rgb(255, 220, 70), Rgb(170, 210, 255) };
        // The wings: two great petals of light behind it, opening and beating twice
        float open = Fx.EaseOut(Math.Clamp((t - I) / 0.25f, 0f, 1f));
        float beat = 0.25f * MathF.Sin(Math.Max(0f, t - I - 0.25f) * MathF.Tau * 1.8f);
        var back = feet + new Vector3(0f, h * 0.6f, -(w * 0.4f + h * 0.2f));
        // Each side a butterfly's two lobes, a great forewing slanting up and out and a smaller hindwing below it, so they
        // read as wings and not as two discs; each lobe edged gold
        for (int side = -1; side <= 1; side += 2)
        {
            for (int lobe = 0; lobe < 2; lobe++)
            {
                bool fore = lobe == 0;
                float tilt = side * ((fore ? 0.75f : 2.1f) + beat) * open;
                var at = back + new Vector3(side * h * (fore ? 0.38f : 0.26f) * open, h * (fore ? 0.2f : -0.18f), 0f);
                float size = h * (fore ? 0.52f : 0.32f) * open;
                fx.Sprite(FxShape.Petal, at, size, Fx.Fade(gold, e * 0.55f), FxBlend.Alpha, tilt, depth: true);
                fx.Sprite(FxShape.Petal, at, size * 0.84f, Fx.Fade(fore ? violet : Fx.Mix(violet, Rgb(110, 70, 170), 0.5f), e * 0.8f), FxBlend.Alpha, tilt, depth: true);
            }
        }
        // The scales falling in a spiral round it
        for (int i = 0; i < 24; i++)
        {
            float u = ((t - I) * 0.8f + i / 24f) % 1f;
            if (t < I) break;
            float a = i * 2.39996f + t * 2.4f;
            float r = (w + h * 0.35f) * (1.1f - 0.4f * u);
            var p = feet + new Vector3(MathF.Cos(a) * r, h * (1.15f - 1.05f * u), MathF.Sin(a) * r * 0.6f);
            fx.Sprite(FxShape.Spore, p, h * 0.06f, Fx.Fade(statuses[i % 3], e * MathF.Sin(u * MathF.PI)), rotation: a, depth: MathF.Sin(a) < 0f);
        }
        // A bubble, a spark and a Z popping over its head one after another
        FxShape[] signs = { FxShape.Bubble, FxShape.Spark, FxShape.Zzz };
        for (int j = 0; j < 3; j++)
        {
            float k = Fx.Envelope(t, I + 0.55f + 0.12f * j, I + 0.65f + 0.12f * j, 1.15f, 1.35f);
            if (k <= 0f) continue;
            var p = feet + new Vector3((j - 1) * h * 0.28f, h * (0.95f + 0.06f * (j % 2)), w * 0.2f);
            fx.Sprite(signs[j], p, h * 0.11f * k, Fx.Fade(statuses[j], k), FxBlend.Alpha);
        }
    }

    /// <summary>
    /// G-Max Depletion (Duraludon): a tower of hex panels of steel-blue light stands behind the target, two columns of
    /// six, lit at the impact and going dark one by one from the top down as a building's windows go out; motes of cyan
    /// are drawn up out of the target, fading as they rise; and eight hex plates round its feet dim from cyan to dark
    /// steel. About 34 quads.
    /// </summary>
    private static void GMaxDepletion(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age, h = c.ToSize, w = c.ToHalfWidth, e = SetPiece(t);
        if (e <= 0f) return;
        var feet = c.ToFeet;
        int s = c.Seed;
        var steel = Rgb(90, 130, 190);
        var cyan = Rgb(160, 230, 255);
        var dark = Rgb(40, 54, 80);
        // The tower behind it: two columns of six panels, going out from the top down after 0.5 s
        var back = feet + new Vector3(0f, 0f, -(w * 0.5f + h * 0.25f));
        float panel = h * 0.13f;
        for (int row = 0; row < 6; row++)
        {
            float outAt = I + 0.25f + 0.09f * (5 - row);
            float lit = 1f - Math.Clamp((t - outAt) / 0.08f, 0f, 1f);
            float rise = Fx.EaseOut(Math.Clamp((t - I - 0.02f * row) / 0.15f, 0f, 1f));
            for (int colm = 0; colm < 2; colm++)
            {
                var p = back + new Vector3((colm - 0.5f) * panel * 1.9f, h * 0.12f + row * panel * 1.85f * rise, 0f);
                fx.Sprite(FxShape.Hex, p, panel, Fx.Fade(Fx.Mix(dark, cyan, lit), e * 0.9f), FxBlend.Alpha, 0f, depth: true);
            }
        }
        // Motes of cyan drawn up out of it
        for (int i = 0; i < 10; i++)
        {
            float u = ((t - I) * 1.2f + Fx.Rand(s, i, 801)) % 1f;
            if (t < I) break;
            var p = c.To + new Vector3(Fx.Signed(s, i, 802) * w * 0.8f, h * 0.9f * u, Fx.Signed(s, i, 803) * h * 0.2f);
            fx.Sprite(FxShape.Dot, p, h * 0.045f * (1f - u * 0.5f), Fx.Fade(cyan, e * (1f - u)));
        }
        // Eight hex plates lying round its feet, dimming from cyan to dark steel
        float dim = Math.Clamp((t - I - 0.3f) / 0.6f, 0f, 1f);
        for (int i = 0; i < 8; i++)
        {
            var at = AroundFeet(c, i, 8, w + h * 0.45f, 0.2f);
            float flicker = dim > 0f && dim < 1f ? 0.7f + 0.3f * MathF.Sin(t * 30f + i * 2f) : 1f;
            fx.Flat(FxShape.Hex, at + Vector3.UnitY * h * 0.03f, Vector3.UnitY, h * 0.16f, Fx.Fade(Fx.Mix(cyan, steel, dim), e * flicker), FxBlend.Alpha, i * 0.3f);
        }
        fx.Flat(FxShape.Glow, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, w + h * 0.7f, Fx.Fade(Fx.Mix(cyan, dark, dim), e * 0.4f), FxBlend.Alpha);
    }
}
