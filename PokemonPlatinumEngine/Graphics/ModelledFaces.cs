using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The modelled kit's faces (style guide, "Modelled characters", "Faces"): a smooth texture of 512 pixels over the front
/// of a modelled head, drawn the way an animated film draws a face: large upright eyes under a heavy upper lash line,
/// a tall iris that lightens toward its foot with one big highlight, thin brows, a nose that is a single small shade
/// and a mouth that is a line. The five expressions, their blinks and the mouth open with a syllable are every kit's
/// (<see cref="Expression"/>). Cut out where nothing is painted, so the skin shows. No GPU calls.
/// </summary>
internal static class ModelledFaces
{
    public const int TextureSize = 512;

    /// <summary>The eyes' half spacing and their height over the head's middle, in the head's half height.</summary>
    public const float EyeHalfSpacing = 0.335f, EyeHeight = -0.06f;

    private static readonly Color Ink = new(38, 28, 44, 255);
    private static readonly Color White = new(250, 248, 246, 255);

    public static PixelCanvas Paint(CharacterStyle s, FaceLayout face, Expression expression, bool blink, bool mouthOpen)
    {
        const int S = TextureSize;
        var canvas = new PixelCanvas(S, S);
        float k = S / (2f * face.TextureHalfSize);
        float R = face.EyeHalfSpacing / EyeHalfSpacing;
        var c = face.TextureCenter;
        Vector2 Px(float x, float y) => new(S * 0.5f + (face.Anchor.X + x * R - c.X) * k, S * 0.5f - (face.Anchor.Y + (y - EyeHeight) * R - c.Y) * k);
        float u = R * k;
        var skin = s.Skin;
        var warm = PixelCanvas.Mix(skin, new Color(196, 110, 104, 255), 0.5f);

        if (s.Blush)
            foreach (float side in new[] { -1f, 1f })
                Soft(canvas, Px(side * 0.46f, -0.33f), 0.16f * u, 0.085f * u, new Color(244, 128, 128, 255), 0.34f);

        foreach (float side in new[] { -1f, 1f })
            Eye(canvas, s, Px(side * EyeHalfSpacing, EyeHeight), side, u, expression, blink, warm);

        if (s.Glasses is Color frame)
        {
            float rx = 0.25f * u, ry = 0.2f * u, line = 0.026f * u;
            foreach (float side in new[] { -1f, 1f })
                AaPaint.Arc(canvas, Px(side * EyeHalfSpacing, EyeHeight - 0.01f), rx, ry, 0f, MathF.Tau, line, frame);
            AaPaint.Line(canvas, Px(-EyeHalfSpacing, EyeHeight) + new Vector2(rx, 0), Px(EyeHalfSpacing, EyeHeight) - new Vector2(rx, 0), line, frame);
        }

        // Brows: a thin tapered stroke in the hair's own dark
        var brow = PixelCanvas.Mix(PixelCanvas.Shadow(s.HairColor, 0.25f), Ink, 0.2f);
        float rise = expression == Expression.Surprised ? 0.06f : 0f;
        float inner = expression switch { Expression.Angry => -0.07f, Expression.Sad => 0.06f, _ => 0f };
        float outer = expression switch { Expression.Angry => 0.03f, Expression.Sad => -0.04f, _ => 0f };
        foreach (float side in new[] { -1f, 1f })
        {
            Vector2 prev = default;
            for (int i = 0; i <= 16; i++)
            {
                float t = i / 16f;
                var at = Px(side * (0.15f + 0.37f * t), EyeHeight + 0.245f + rise + 0.035f * MathF.Sin(MathF.PI * MathF.Pow(t, 0.75f)) + inner * (1f - t) + outer * t);
                if (i > 0) AaPaint.Line(canvas, prev, at, (s.BushyBrows ? 0.06f : 0.034f) * (1f - 0.55f * t) * u, brow);
                prev = at;
            }
        }

        // The nose: one small shade under its tip, to the side away from the light
        AaPaint.Line(canvas, Px(0.012f, -0.425f), Px(0.05f, -0.415f), 0.016f * u, PixelCanvas.Mix(skin, warm, 0.75f));

        Mouth(canvas, Px, u, expression, mouthOpen, skin);
        return canvas;
    }

    private static void Soft(PixelCanvas canvas, Vector2 at, float rx, float ry, Color colour, float strength)
    {
        for (int y = (int)(at.Y - ry); y <= (int)(at.Y + ry); y++)
            for (int x = (int)(at.X - rx); x <= (int)(at.X + rx); x++)
            {
                float dx = (x + 0.5f - at.X) / rx, dy = (y + 0.5f - at.Y) / ry;
                float d = dx * dx + dy * dy;
                if (d < 1f) AaPaint.Plot(canvas, x, y, colour, strength * (1f - d) * (1f - d));
            }
    }

    private static void Eye(PixelCanvas canvas, CharacterStyle s, Vector2 at, float side, float u, Expression expression, bool blink, Color warm)
    {
        float w = 0.168f * u, up = 0.150f * u, down = 0.092f * u;
        float tilt = -0.014f * u;
        if (expression == Expression.Surprised) { up *= 1.14f; down *= 1.12f; }
        float lift = expression == Expression.Happy ? 0.05f * u : 0f;

        // The lids' edges over (negative) and under the eye's middle, x outward from the face's middle
        float Upper(float ox)
        {
            float t = Math.Clamp(ox / w, -1f, 1f);
            float h = up * MathF.Pow(MathF.Max(0f, 1f - MathF.Pow(MathF.Abs(t), 2.5f)), 0.62f);
            if (expression == Expression.Sad) h -= 0.3f * up * MathF.Max(0f, t);
            if (expression == Expression.Angry) h -= 0.42f * up * MathF.Max(0f, -t + 0.1f);
            if (expression == Expression.Happy) h *= 0.9f;
            return -h + tilt * t;
        }
        float Lower(float ox)
        {
            float t = Math.Clamp(ox / w, -1f, 1f);
            return down * MathF.Pow(MathF.Max(0f, 1f - t * t), 0.72f) - lift * (1f - t * t) + tilt * t;
        }
        float Lash(float ox) => (0.030f + 0.022f * Math.Clamp(ox / w * 0.5f + 0.5f, 0f, 1f)) * u * (s.Lashes ? 1.25f : 1f);

        if (blink)
        {
            Vector2 prev = default;
            for (int i = 0; i <= 18; i++)
            {
                float t = -1f + 2f * i / 18f;
                var p = at + new Vector2(side * t * w, 0.3f * down * (1f - t * t) + tilt * t + 0.02f * u);
                if (i > 0) AaPaint.Line(canvas, prev, p, (0.026f + 0.012f * (t * 0.5f + 0.5f)) * u, Ink);
                prev = p;
            }
            if (s.Lashes) AaPaint.Line(canvas, prev, prev + new Vector2(side * 0.05f * u, 0.03f * u), 0.018f * u, Ink);
            return;
        }

        float irisRx = 0.104f * u, irisRy = 0.136f * u;
        if (expression == Expression.Surprised) { irisRx *= 0.86f; irisRy *= 0.86f; }
        var irisAt = at + new Vector2(0, 0.004f * u);
        var eye = s.Eyes;
        var irisDark = PixelCanvas.Mix(eye, Ink, 0.55f);
        var irisMid = PixelCanvas.Mix(eye, new Color(96, 112, 168, 255), 0.25f);
        var irisLight = PixelCanvas.Light1(PixelCanvas.Mix(eye, new Color(132, 164, 220, 255), 0.45f), 0.3f);
        const int N = 4;
        int x0 = (int)(at.X - w * 1.5f - 3), x1 = (int)(at.X + w * 1.5f + 3), y0 = (int)(at.Y - up * 1.6f - 3), y1 = (int)(at.Y + down * 1.5f + 3);
        for (int py = y0; py <= y1; py++)
            for (int px = x0; px <= x1; px++)
            {
                int inside = 0, lash = 0;
                for (int sy = 0; sy < N; sy++)
                    for (int sx = 0; sx < N; sx++)
                    {
                        float qx = px + (sx + 0.5f) / N - at.X, qy = py + (sy + 0.5f) / N - at.Y;
                        float ox = qx * side;
                        float top = Upper(ox);
                        if (MathF.Abs(qx) <= w && qy >= top && qy <= Lower(ox)) inside++;
                        // The lash line: a band along the upper lid, heavier outward, running a little past the outer corner
                        if (ox >= -w * 1.02f && ox <= w * 1.12f && qy <= top + 0.008f * u && qy >= top - Lash(ox) * (ox > w ? MathF.Max(0f, 1f - (ox - w) / (0.12f * w)) : 1f)) lash++;
                    }
                if (inside > 0)
                {
                    var p = new Vector2(px + 0.5f, py + 0.5f);
                    float ex = (p.X - irisAt.X) / irisRx, ey = (p.Y - irisAt.Y) / irisRy;
                    float ri = MathF.Sqrt(ex * ex + ey * ey);
                    Color col = White;
                    if (ri <= 1f)
                    {
                        float t = Math.Clamp(ey * 0.5f + 0.5f, 0f, 1f);
                        col = t < 0.5f ? PixelCanvas.Mix(irisDark, irisMid, t * 2f) : PixelCanvas.Mix(irisMid, irisLight, (t - 0.5f) * 2f);
                        if (ri > 0.84f) col = PixelCanvas.Mix(col, irisDark, (ri - 0.84f) / 0.16f * 0.85f);
                        if (ri <= 0.42f) col = PixelCanvas.Mix(PixelCanvas.Mix(eye, Ink, 0.9f), col, Math.Clamp((ri - 0.36f) / 0.06f, 0f, 1f));
                    }
                    else if (ri <= 1.03f) col = PixelCanvas.Mix(irisDark, White, (ri - 1f) / 0.03f);
                    // The lid's shade over the top of the eye
                    float shade = Math.Clamp(1f - (p.Y - (at.Y + Upper((p.X - at.X) * side))) / (0.06f * u), 0f, 1f);
                    col = PixelCanvas.Mix(col, PixelCanvas.Mix(col, new Color(92, 80, 120, 255), 0.55f), shade * 0.75f);
                    AaPaint.Plot(canvas, px, py, col, inside / (float)(N * N));
                }
                if (lash > 0) AaPaint.Plot(canvas, px, py, Ink, lash / (float)(N * N));
            }
        // One big highlight up toward the light and a small one opposite, the same way in both eyes
        AaPaint.Ellipse(canvas, irisAt + new Vector2(-0.040f * u, -0.058f * u), 0.034f * u, 0.040f * u, Color.White);
        AaPaint.Ellipse(canvas, irisAt + new Vector2(0.038f * u, 0.062f * u), 0.014f * u, 0.014f * u, new Color(255, 255, 255, 255));

        // The lower lid: a light line along its outer part, and the fold over the eye
        Vector2 last = default;
        for (int i = 0; i <= 10; i++)
        {
            float ox = w * (0.15f + 0.85f * i / 10f);
            var p = at + new Vector2(side * ox, Lower(ox) + 0.006f * u);
            if (i > 0) AaPaint.Line(canvas, last, p, 0.012f * u, PixelCanvas.Mix(warm, Ink, 0.35f));
            last = p;
        }
        for (int i = 0; i <= 12; i++)
        {
            float ox = w * (-0.5f + 1.4f * i / 12f);
            var p = at + new Vector2(side * ox, Upper(ox) - Lash(ox) - 0.034f * u);
            if (i > 0) AaPaint.Line(canvas, last, p, 0.010f * u, warm);
            last = p;
        }
        if (s.Lashes)
        {
            var corner = at + new Vector2(side * w * 1.06f, Upper(w) - 0.01f * u);
            AaPaint.Line(canvas, corner, corner + new Vector2(side * 0.06f * u, -0.045f * u), 0.02f * u, Ink);
            AaPaint.Line(canvas, corner - new Vector2(side * 0.04f * u, 0.02f * u), corner + new Vector2(side * 0.02f * u, -0.07f * u), 0.016f * u, Ink);
        }
    }

    private static void Mouth(PixelCanvas canvas, Func<float, float, Vector2> px, float u, Expression expression, bool open, Color skin)
    {
        var line = PixelCanvas.Mix(skin, new Color(116, 52, 60, 255), 0.82f);
        var inside = new Color(112, 40, 52, 255);
        var tongue = new Color(226, 120, 128, 255);
        var at = px(0f, -0.655f);

        void Curve(float half, float smile, float width)
        {
            Vector2 prev = default;
            for (int i = 0; i <= 14; i++)
            {
                float t = -1f + 2f * i / 14f;
                var p = at + new Vector2(t * half * u, smile * u * (t * t - 0.5f));
                if (i > 0) AaPaint.Line(canvas, prev, p, width * u * (1f - 0.45f * t * t), line);
                prev = p;
            }
        }

        if (open)
        {
            var (ow, oh) = expression switch
            {
                Expression.Happy => (0.12f, 0.085f), Expression.Surprised => (0.065f, 0.09f), Expression.Sad => (0.06f, 0.04f), Expression.Angry => (0.11f, 0.05f), _ => (0.08f, 0.055f)
            };
            var o = at + new Vector2(0, 0.02f * u);
            AaPaint.Ellipse(canvas, o, (ow + 0.012f) * u, (oh + 0.012f) * u, line);
            AaPaint.Ellipse(canvas, o, ow * u, oh * u, inside);
            AaPaint.Ellipse(canvas, o + new Vector2(0, oh * 0.5f * u), ow * 0.6f * u, oh * 0.36f * u, tongue);
            return;
        }
        switch (expression)
        {
            case Expression.Happy:
                AaPaint.HalfDisc(canvas, at - new Vector2(0, 0.012f * u), 0.125f * u, 0.095f * u, line);
                AaPaint.HalfDisc(canvas, at - new Vector2(0, 0.010f * u), 0.110f * u, 0.078f * u, inside);
                AaPaint.Ellipse(canvas, at + new Vector2(0, 0.048f * u), 0.06f * u, 0.022f * u, tongue);
                break;
            case Expression.Surprised:
                AaPaint.Ellipse(canvas, at + new Vector2(0, 0.012f * u), 0.052f * u, 0.062f * u, line);
                AaPaint.Ellipse(canvas, at + new Vector2(0, 0.012f * u), 0.038f * u, 0.048f * u, inside);
                break;
            case Expression.Sad:
                Curve(0.085f, 0.05f, 0.022f);
                break;
            case Expression.Angry:
                Curve(0.10f, 0.03f, 0.024f);
                break;
            default:
                Curve(0.095f, -0.035f, 0.022f);
                // The lower lip's small shade
                AaPaint.Line(canvas, at + new Vector2(-0.03f * u, 0.058f * u), at + new Vector2(0.03f * u, 0.058f * u), 0.012f * u, PixelCanvas.Mix(skin, line, 0.3f));
                break;
        }
    }
}
