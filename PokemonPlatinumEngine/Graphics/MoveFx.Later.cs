using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

// The later mechanics' changes of shape (plan 25 · Y14; style guide, "Mega Evolution, Ultra Burst and Z-Moves"): a
// Mega Evolution and a Primal Reversion, Necrozma's Ultra Burst, and the Z-Power gathered before a Z-Move. All of it
// our own, since Platinum has none of it: each a pure function of its cue's age and seed, under MaxQuads at every age,
// with no screen flash over 0.35 and no shake. What the body itself shows (its light, its darkness, a cast of the
// Z-Power's colour) is BodyCast's, which the renderer hands the shader's flash.
internal static partial class MoveFx
{
    // The four colours of a Mega Evolution's light
    private static readonly Color MegaViolet = Rgb(186, 96, 255), MegaPink = Rgb(255, 120, 200), MegaOrange = Rgb(255, 176, 70), MegaCyan = Rgb(110, 220, 255);

    private static readonly Color[] MegaColours = { MegaViolet, MegaPink, MegaOrange, MegaCyan };

    /// <summary>A Primal Reversion's colours: Kyogre's blue (Water) or Groudon's red (anything else).</summary>
    private static FxColors PrimalColours(PokemonType type) => type == PokemonType.Water
        ? new(Rgb(64, 146, 255), Rgb(190, 232, 255), Rgb(30, 90, 190))
        : new(Rgb(236, 70, 40), Rgb(255, 200, 110), Rgb(150, 40, 20));

    private static readonly Color UltraGold = Rgb(255, 214, 110), UltraDark = Rgb(70, 40, 120), UltraWhite = Rgb(255, 244, 200);

    /// <summary>
    /// What the body itself shows through one of these cues, as the shader's flash (a colour and how much of it):
    /// a Mega Evolution's light, an Ultra Burst's dark and then its light, the Z-Power's pulsing cast. None for any
    /// other cue.
    /// </summary>
    public static (Color Color, float Amount) BodyCast(EffectCue cue)
    {
        float t = cue.Age;
        switch (cue.Kind)
        {
            case CueKind.Mega:
                return (cue.Primal ? Fx.Mix(PrimalColours(cue.Type).Light, White, 0.5f) : Rgb(255, 252, 240), 0.96f * Fx.Envelope(t, 0.3f, 0.72f, 0.92f, 1.3f));
            case CueKind.UltraBurst:
                if (t < BattleAnimator.UltraBurstSwapTime) return (Rgb(40, 26, 70), 0.85f * Math.Clamp(t / 0.5f, 0f, 1f));
                return (UltraWhite, 0.96f * Fx.Envelope(t, BattleAnimator.UltraBurstSwapTime, BattleAnimator.UltraBurstSwapTime + 0.01f, 1.05f, 1.45f));
            case CueKind.ZPower:
                return (ColorsOf(cue.Type).Main, 0.3f * (0.6f + 0.4f * MathF.Sin(t * 9f)) * Fx.Envelope(t, 0f, 0.25f, 1.1f, BattleAnimator.ZPowerTime));
            default:
                return (default, 0f);
        }
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// A ribbon through <paramref name="points"/> cut where it passes behind or before the body whose middle is at
    /// <paramref name="middleZ"/>: the runs on the camera's side (+Z) are drawn over the body, the rest depth-tested,
    /// as the turn's moments split what goes round a body.
    /// </summary>
    private static void RoundRibbon(FxList fx, List<Vector3> points, float middleZ, float width, Color color, FxBlend blend)
    {
        int start = 0;
        for (int i = 1; i <= points.Count; i++)
        {
            bool ends = i == points.Count || (points[i].Z >= middleZ) != (points[start].Z >= middleZ);
            if (!ends) continue;
            // Each run shares its last point with the next, so the ribbon has no gap where it turns behind
            int last = Math.Min(i, points.Count - 1);
            if (last - start >= 1)
            {
                var run = points.GetRange(start, last - start + 1).ToArray();
                fx.Ribbon(run, width, color, blend, depth: points[start].Z < middleZ);
            }
            start = i;
        }
    }

    /// <summary>
    /// A helix round a body from its feet: <paramref name="turns"/> turns up to <paramref name="top"/> × h, showing
    /// as far up as <paramref name="reveal"/> (0..1), its radius from <paramref name="r0"/> at the feet to
    /// <paramref name="r1"/> at the top.
    /// </summary>
    private static List<Vector3> Helix(Vector3 feet, float h, float r0, float r1, float top, float turns, float phase, float reveal, int steps = 22)
    {
        var points = new List<Vector3>(steps + 1);
        int shown = (int)MathF.Ceiling(steps * Math.Clamp(reveal, 0f, 1f));
        for (int i = 0; i <= shown; i++)
        {
            float u = i / (float)steps;
            float a = phase + u * turns * MathF.Tau;
            float r = r0 + (r1 - r0) * u;
            points.Add(feet + new Vector3(MathF.Cos(a) * r, h * (0.04f + top * u), MathF.Sin(a) * r * 0.6f));
        }
        return points;
    }

    /// <summary>Things set round a body at a height, split at its middle (the camera's side drawn over it).</summary>
    private static void Encircle(FxList fx, Vector3 feet, float y, float rx, int count, float spin, float age, int seed, FxShape shape,
        Color a, Color b, float size, FxBlend blend, float env)
    {
        if (env <= 0f) return;
        for (int i = 0; i < count; i++)
        {
            float angle = i * MathF.Tau / count + age * spin + Fx.Rand(seed, i, 301) * 0.3f;
            var p = feet + new Vector3(MathF.Cos(angle) * rx, y, MathF.Sin(angle) * rx * 0.6f);
            fx.Sprite(shape, p, size, Fx.Fade(i % 2 == 0 ? a : b, env), blend, angle, depth: MathF.Sin(angle) < 0f);
        }
    }

    // ------------------------------------------------------------------ Mega Evolution and Primal Reversion

    /// <summary>
    /// A Mega Evolution (or a Primal Reversion, in its own colour): ribbons of light spiral up round the body as it
    /// whitens; at <see cref="BattleAnimator.MegaSwapTime"/> its shape changes inside the light and the light bursts in
    /// a ring and sparks under the Mega mark; then sparks settle round the new body.
    /// </summary>
    private static void MegaEvolution(in FxCtx c, FxList fx)
    {
        float t = c.Age;
        float h = c.FromSize, w = c.FromHalfWidth;
        var feet = c.FromFeet;
        var middle = c.From;
        const float S = BattleAnimator.MegaSwapTime;
        bool primal = c.Cue.Primal;
        var pc = PrimalColours(c.Cue.Type);
        Color Colour(int i) => primal ? (i % 2 == 0 ? pc.Main : pc.Light) : MegaColours[i % 4];
        fx.Land(S);

        // Gathering: four ribbons spiralling up and drawing in
        float gather = Fx.Envelope(t, 0f, 0.15f, 0.7f, 0.95f);
        if (gather > 0f)
        {
            float reveal = Math.Clamp(t / 0.5f, 0f, 1f);
            for (int k = 0; k < 4; k++)
            {
                var helix = Helix(feet, h, w * 0.75f + h * 0.08f, w * 0.5f + h * 0.04f, 1.12f, 2f, k * MathF.PI / 2f + t * 5f, reveal);
                RoundRibbon(fx, helix, feet.Z, h * 0.05f, Fx.Fade(Colour(k), gather * 0.7f), FxBlend.Alpha);
                RoundRibbon(fx, helix, feet.Z, h * 0.02f, Fx.Fade(White, gather * 0.8f), FxBlend.Add);
            }
            for (int i = 0; i < 12; i++)
            {
                float u = (t * 1.1f + Fx.Rand(c.Seed, i, 311)) % 1f;
                float a = Fx.Rand(c.Seed, i, 312) * MathF.Tau + t * 4f;
                float r = w * 0.8f + h * 0.05f;
                var p = feet + new Vector3(MathF.Cos(a) * r, h * 1.1f * u, MathF.Sin(a) * r * 0.6f);
                fx.Sprite(FxShape.Spark, p, h * 0.06f, Fx.Fade(Colour(i), gather * MathF.Sin(u * MathF.PI)), depth: MathF.Sin(a) < 0f);
            }
            fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, (w + h * 0.3f) * (1.4f - 0.5f * reveal), Fx.Fade(Colour(0), gather * 0.8f));
        }

        // The burst as the shape changes: rings facing the lens, sparks (drops or embers for a Primal) and a flash
        Fx.Shockwave(fx, t, middle, Vector3.Zero, S, 0.5f, h * 1.5f, Fx.Fade(Colour(0), 0.9f));
        Fx.Shockwave(fx, t, middle, Vector3.Zero, S + 0.06f, 0.5f, h * 1.2f, Fx.Fade(Colour(3), 0.8f));
        if (primal)
            Fx.Burst(fx, c.Seed, t, middle, h, S, 0.6f, 16, c.Cue.Type == PokemonType.Water ? FxShape.Drop : FxShape.Ember, pc.Main, pc.Light, 0.08f, 1.0f, 0.6f,
                c.Cue.Type == PokemonType.Water ? FxBlend.Alpha : FxBlend.Add);
        else
            for (int i = 0; i < 16; i++)
            {
                float k = (t - S) / (0.45f + 0.15f * Fx.Rand(c.Seed, i, 313));
                if (k < 0f || k > 1f) continue;
                var p = middle + Fx.Direction(c.Seed, i + 40) * h * 1.0f * Fx.EaseOut(k);
                fx.Sprite(FxShape.Spark, p, h * 0.09f * (1f - 0.5f * k), Fx.Fade(Colour(i), 1f - k * k), rotation: k * 3f);
            }
        fx.ScreenFlash(primal ? pc.Light : Rgb(236, 214, 255), 0.3f * Fx.Envelope(t, S - 0.02f, S + 0.03f, S + 0.06f, S + 0.23f));

        // The mark over the head
        float mark = Fx.Envelope(t, S, S + 0.1f, S + 0.45f, S + 0.6f);
        if (mark > 0f)
        {
            var at = feet + Vector3.UnitY * h * 1.15f;
            float r = h * 0.21f;
            fx.Sprite(FxShape.Glow, at, r * 1.6f, Fx.Fade(Colour(0), mark * 0.5f));
            fx.Sprite(FxShape.Ring, at, r, Fx.Fade(primal ? pc.Main : White, mark));
            if (primal)
                for (int i = 0; i < 3; i++)
                {
                    float a = i * MathF.Tau / 3f + MathF.PI / 2f;
                    fx.Sprite(FxShape.Spark, at + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * r, r * 0.45f, Fx.Fade(pc.Light, mark), rotation: a);
                }
            else
            {
                fx.Sprite(FxShape.Spark, at, r * 0.7f, Fx.Fade(White, mark), rotation: MathF.PI / 4f);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * MathF.Tau / 4f + t * 3f;
                    fx.Sprite(FxShape.Glow, at + new Vector3(MathF.Cos(a), MathF.Sin(a), 0f) * r, r * 0.3f, Fx.Fade(MegaColours[i], mark));
                }
            }
        }

        // Settling: sparks drifting down round the new body
        for (int i = 0; i < 14; i++)
        {
            float k = (t - 1.0f - 0.15f * Fx.Rand(c.Seed, i, 314)) / 0.42f;
            if (k < 0f || k > 1f) continue;
            float a = Fx.Rand(c.Seed, i, 315) * MathF.Tau;
            float r = w * (0.6f + 0.6f * Fx.Rand(c.Seed, i, 316));
            var p = feet + new Vector3(MathF.Cos(a) * r, h * (1.1f - 0.8f * k), MathF.Sin(a) * r * 0.6f);
            fx.Sprite(primal ? FxShape.Glow : FxShape.Spark, p, h * 0.05f, Fx.Fade(Colour(i), MathF.Sin(k * MathF.PI)), depth: MathF.Sin(a) < 0f);
        }
    }

    // ------------------------------------------------------------------ Ultra Burst

    /// <summary>
    /// Necrozma's Ultra Burst: the body darkens as prisms close in on it, cracks of light run out of its middle, and at
    /// <see cref="BattleAnimator.UltraBurstSwapTime"/> it turns to light, changes shape inside it and rays of gold burst
    /// out; gold dust settles.
    /// </summary>
    private static void UltraBurst(in FxCtx c, FxList fx)
    {
        float t = c.Age;
        float h = c.FromSize, w = c.FromHalfWidth;
        var feet = c.FromFeet;
        var middle = c.From;
        const float S = BattleAnimator.UltraBurstSwapTime;
        fx.Land(S);

        // The prisms closing in
        float env = Fx.Envelope(t, 0f, 0.15f, 0.75f, 0.9f);
        if (env > 0f)
        {
            float close = Fx.EaseOut(Math.Clamp(t / 0.8f, 0f, 1f));
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f + t * 2f;
                float r = w * (1.1f + 0.4f * (1f - close)) + h * 0.05f;
                var p = feet + new Vector3(MathF.Cos(a) * r, h * (0.3f + 0.45f * Fx.Rand(c.Seed, i, 321)), MathF.Sin(a) * r * 0.6f);
                bool behind = MathF.Sin(a) < 0f;
                fx.Sprite(FxShape.Shard, p, h * 0.13f, Fx.Fade(UltraDark, env * 0.9f), FxBlend.Alpha, a + t * 3f, depth: behind);
                fx.Sprite(FxShape.Spark, p, h * 0.07f, Fx.Fade(UltraGold, env * (0.6f + 0.4f * MathF.Sin(t * 12f + i))), rotation: a, depth: behind);
            }
        }

        // Cracks of light out of its middle, more and longer
        if (t >= 0.5f && t < S)
        {
            float grow = (t - 0.5f) / (S - 0.5f);
            int cracks = 3 + (int)(grow * 5f);
            for (int i = 0; i < cracks; i++)
            {
                var dir = Fx.Direction(c.Seed, i + 70);
                var end = middle + dir * h * (0.15f + 0.3f * grow);
                var bend = Vector3.Lerp(middle, end, 0.5f) + Fx.Direction(c.Seed, i + 90) * h * 0.04f;
                fx.Ribbon(new[] { middle, bend, end }, h * 0.03f, Fx.Fade(UltraGold, 0.9f));
                fx.Ribbon(new[] { middle, bend, end }, h * 0.012f, White);
            }
        }

        // The burst: rays of gold, a ring, a flash
        float rays = Fx.Envelope(t, S, S + 0.07f, 1.1f, 1.4f);
        if (rays > 0f)
        {
            float reach = Fx.EaseOut(Math.Clamp((t - S) / 0.2f, 0f, 1f));
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f + MathF.PI / 8f;
                var dir = new Vector3(MathF.Cos(a), MathF.Sin(a), 0.15f * Fx.Signed(c.Seed, i, 322));
                var end = middle + dir * h * 0.8f * reach;
                fx.Ribbon(new[] { middle, Vector3.Lerp(middle, end, 0.5f), end }, h * 0.07f * rays, Fx.Fade(UltraGold, rays * 0.8f));
                fx.Ribbon(new[] { middle, Vector3.Lerp(middle, end, 0.5f), end }, h * 0.025f * rays, Fx.Fade(White, rays));
            }
            fx.Sprite(FxShape.Glow, middle, h * 0.7f, Fx.Fade(UltraGold, rays * 0.5f));
        }
        Fx.Shockwave(fx, t, middle, Vector3.Zero, S, 0.55f, h * 1.4f, Fx.Fade(UltraGold, 0.9f));
        fx.ScreenFlash(Rgb(255, 240, 200), 0.35f * Fx.Envelope(t, S - 0.02f, S + 0.03f, S + 0.06f, S + 0.26f));

        // Gold dust settling
        for (int i = 0; i < 16; i++)
        {
            float k = (t - 1.05f - 0.2f * Fx.Rand(c.Seed, i, 323)) / 0.5f;
            if (k < 0f || k > 1f) continue;
            float a = Fx.Rand(c.Seed, i, 324) * MathF.Tau;
            float r = w * (0.5f + 0.9f * Fx.Rand(c.Seed, i, 325));
            var p = feet + new Vector3(MathF.Cos(a) * r, h * (1.15f - 0.9f * k), MathF.Sin(a) * r * 0.6f);
            fx.Sprite(FxShape.Glow, p, h * 0.045f, Fx.Fade(UltraGold, MathF.Sin(k * MathF.PI)), depth: MathF.Sin(a) < 0f);
        }
    }

    // ------------------------------------------------------------------ the Z-Power

    /// <summary>
    /// The Z-Power gathered before a Z-Move, in the colour of its type: a tall frame of light rising round the body
    /// out of the ground and held, motes rising inside it, a ring at its feet, and a Z drawn stroke by stroke over its
    /// head. The pose is the animator's (the charge's gathering).
    /// </summary>
    private static void ZPower(in FxCtx c, FxColors col, FxList fx)
    {
        float t = c.Age;
        float h = c.FromSize, w = c.FromHalfWidth;
        var feet = c.FromFeet;
        fx.Land(0.45f);
        float env = Fx.Envelope(t, 0f, 0.1f, 1.1f, BattleAnimator.ZPowerTime);
        if (env <= 0f) return;
        float pulse = 0.8f + 0.2f * MathF.Sin(t * 10f);

        // The frame, rising out of the ground: its sides and its rounded top
        float rise = Fx.EaseOut(Math.Clamp(t / 0.45f, 0f, 1f));
        float half = w * 1.25f + h * 0.06f;
        float top = h * 1.2f * rise;
        float round = Math.Min(half * 0.6f, top * 0.3f);
        var frame = new List<Vector3> { feet + new Vector3(-half, 0f, 0f), feet + new Vector3(-half, top - round, 0f) };
        for (int i = 1; i < 6; i++)
        {
            float a = MathF.PI - i * MathF.PI / 12f;
            frame.Add(feet + new Vector3(-half + round + MathF.Cos(a) * round, top - round + MathF.Sin(a) * round, 0f));
        }
        frame.Add(feet + new Vector3(-half + round, top, 0f));
        frame.Add(feet + new Vector3(half - round, top, 0f));
        for (int i = 1; i < 6; i++)
        {
            float a = MathF.PI / 2f - i * MathF.PI / 12f;
            frame.Add(feet + new Vector3(half - round + MathF.Cos(a) * round, top - round + MathF.Sin(a) * round, 0f));
        }
        frame.Add(feet + new Vector3(half, top - round, 0f));
        frame.Add(feet + new Vector3(half, 0f, 0f));
        if (top > h * 0.05f)
        {
            var pts = frame.ToArray();
            fx.Ribbon(pts, h * 0.06f, Fx.Fade(col.Main, env * 0.7f), FxBlend.Alpha);
            fx.Ribbon(pts, h * 0.035f * pulse, Fx.Fade(col.Light, env * 0.9f));
            fx.Ribbon(pts, h * 0.14f, Fx.Fade(col.Main, env * 0.25f * pulse));
        }

        // Motes rising inside it, and a ring at its feet
        for (int i = 0; i < 12; i++)
        {
            float u = (t * 0.9f + Fx.Rand(c.Seed, i, 331)) % 1f;
            if (u * h * 1.15f > top) continue;
            var p = feet + new Vector3(Fx.Signed(c.Seed, i, 332) * half * 0.85f, h * 1.15f * u, h * 0.1f * Fx.Signed(c.Seed, i, 333));
            fx.Sprite(i % 3 == 0 ? FxShape.Spark : FxShape.Glow, p, h * 0.055f, Fx.Fade(Fx.Mix(col.Main, col.Light, Fx.Rand(c.Seed, i, 334)), env * MathF.Sin(u * MathF.PI)));
        }
        fx.Flat(FxShape.Ring, feet + Vector3.UnitY * h * 0.03f, Vector3.UnitY, half * (1.05f + 0.05f * pulse), Fx.Fade(col.Light, env * 0.8f));
        fx.Flat(FxShape.Glow, feet + Vector3.UnitY * h * 0.02f, Vector3.UnitY, half * 1.1f, Fx.Fade(col.Main, env * 0.4f));

        // The Z over its head, stroke by stroke
        var zAt = feet + Vector3.UnitY * h * 1.2f;
        float zw = h * 0.18f, zh = h * 0.08f;
        Vector3[] corners = { zAt + new Vector3(-zw, zh, 0f), zAt + new Vector3(zw, zh, 0f), zAt + new Vector3(-zw, -zh, 0f), zAt + new Vector3(zw, -zh, 0f) };
        for (int s = 0; s < 3; s++)
        {
            float k = Math.Clamp((t - 0.35f - s * 0.1f) / 0.1f, 0f, 1f);
            if (k <= 0f) break;
            var a = corners[s];
            var b = Vector3.Lerp(a, corners[s + 1], k);
            fx.Ribbon(new[] { a, b }, h * 0.05f, Fx.Fade(col.Main, env), FxBlend.Alpha);
            fx.Ribbon(new[] { a, b }, h * 0.025f, Fx.Fade(col.Light, env));
        }
        if (t >= 0.65f) fx.Sprite(FxShape.Glow, zAt, h * 0.3f, Fx.Fade(col.Light, env * 0.35f * pulse));
    }
}
