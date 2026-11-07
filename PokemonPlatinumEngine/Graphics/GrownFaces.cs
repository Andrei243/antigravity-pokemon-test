using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The grown kit's faces (plan 30 · FM4, part b; style guide, "Grown characters", "Faces"): a smooth texture of 256
/// pixels laid over the front of a grown head on its face patch, as the chibi kit's 128 is, with a grown layout: almond
/// eyes a fifth of the head's width apart at its middle with smaller irises, lids and a fold, brows always drawn (but
/// where the look's own are sculpted), the nostrils under the sculpted nose's tip, and lips. The five expressions, their
/// blinks and the mouth open with a syllable are the chibi kit's (<see cref="Expression"/>), so a script's mood and a
/// trainer who speaks work unchanged. The texture is cut out where nothing is painted, so the skin under it shows. No
/// GPU calls.
/// </summary>
internal static class GrownFaces
{
    public const int TextureSize = 256;

    /// <summary>The eyes' half spacing in the head's half height (style guide, "Grown characters", "The head").</summary>
    public const float EyeHalfSpacing = 0.30f;

    private static readonly Color Ink = new(34, 26, 40, 255);
    private static readonly Color White = new(246, 243, 240, 255);

    /// <summary>
    /// Paints a grown face's texture: the square <see cref="FaceLayout.TextureCenter"/> ± half its size on the front of
    /// the head, in model units, the features placed from the anchor between the eyes in the head's half height.
    /// </summary>
    public static PixelCanvas Paint(CharacterStyle s, FaceLayout face, Expression expression, bool blink, bool mouthOpen)
    {
        const int S = TextureSize;
        var canvas = new PixelCanvas(S, S);
        float k = S / (2f * face.TextureHalfSize);
        float R = face.EyeHalfSpacing / EyeHalfSpacing;
        var c = face.TextureCenter;
        // A point of the face in the head's half height from the anchor (x to the character's left, y up) in pixels
        Vector2 Px(float x, float y) => new(S * 0.5f + (face.Anchor.X + x * R - c.X) * k, S * 0.5f - (face.Anchor.Y + y * R - c.Y) * k);
        float u = R * k;   // pixels to the head's half height

        var skin = s.Skin;
        var fold = PixelCanvas.Mix(skin, new Color(150, 96, 92, 255), 0.42f);
        bool woman = s.Lashes;

        // The nose's two nostrils under its tip, over the sculpted nose (whose own light shades the rest of it: a shade
        // painted over the tip read as a clown's nose on the first turntables)
        foreach (float side in new[] { -1f, 1f })
            Blob(canvas, Px(side * 0.07f, -0.425f), 0.026f * u, 0.014f * u, PixelCanvas.Mix(skin, new Color(120, 66, 64, 255), 0.42f));

        foreach (float side in new[] { -1f, 1f })
            Eye(canvas, s, Px(side * EyeHalfSpacing, 0f), side, u, expression, blink, woman, fold);

        // Glasses: a rim round each eye, a bridge between them and a short temple outward, there with the eyes shut too
        if (s.Glasses is Color frame)
        {
            float rx = 0.2f * u, ry = 0.135f * u, line = Math.Max(2.2f, 0.024f * u);
            foreach (float side in new[] { -1f, 1f })
            {
                var e = Px(side * EyeHalfSpacing, 0.005f);
                AaPaint.Arc(canvas, e, rx, ry, 0f, MathF.PI * 2f, line, frame);
                AaPaint.Line(canvas, e + new Vector2(side * rx, -ry * 0.25f), e + new Vector2(side * (rx + 0.1f * u), -ry * 0.4f), line, frame);
            }
            var l = Px(-EyeHalfSpacing, 0.005f);
            var r = Px(EyeHalfSpacing, 0.005f);
            AaPaint.Arc(canvas, (l + r) / 2f + new Vector2(0, 0.01f * u), (r.X - l.X) / 2f - rx, 0.05f * u, MathF.PI * 1.15f, MathF.PI * 1.85f, line, frame);
        }

        // Brows, always but where the look's own are sculpted (bushy brows); a grown face's bangs rarely cover them
        if (!s.BushyBrows)
        {
            var brow = PixelCanvas.Mix(PixelCanvas.Shadow(s.HairColor, 0.3f), Ink, 0.15f);
            foreach (float side in new[] { -1f, 1f })
                Brow(canvas, Px, side, u, expression, brow, woman);
        }

        Mouth(canvas, s, Px, u, expression, mouthOpen, woman, skin);
        return canvas;
    }

    // ------------------------------------------------------------------ the parts

    /// <summary>
    /// An eye: an almond of white whose upper edge arches higher than its lower, the iris (lighter toward its foot, a
    /// darker ring round it) and its pupil cut by the lids, two highlights, the upper lid's line heavier toward the outer
    /// corner (with lashes flicked out on a woman's), a light lower lid and the fold above. Shut, the lid's line curves
    /// down with the lashes at its outer end.
    /// </summary>
    private static void Eye(PixelCanvas canvas, CharacterStyle s, Vector2 at, float side, float u, Expression expression, bool blink, bool woman, Color fold)
    {
        float w = 0.14f * u, up = 0.072f * u, down = 0.05f * u;
        float tilt = -0.012f * u;   // the outer corner a little higher than the inner
        if (expression == Expression.Surprised) { up *= 1.3f; down *= 1.25f; }
        // Happy: the lower lid raised in a smile; sad: the upper lid drooping at the outer corner; angry: pressed down at the inner
        float lift = expression == Expression.Happy ? 0.03f * u : 0f;

        // The upper and the lower lid's edges as heights over (negative) and under the eye's middle line, x outward from the middle
        float Upper(float x)
        {
            float t = Math.Clamp(x / w, -1f, 1f);
            float h = up * MathF.Pow(1f - t * t, 0.6f);
            if (expression == Expression.Sad) h -= 0.25f * up * MathF.Max(0f, t);
            if (expression == Expression.Angry) h -= 0.35f * up * MathF.Max(0f, -t);
            if (expression == Expression.Happy) h *= 0.92f;
            return -h + tilt * t;
        }
        float Lower(float x)
        {
            float t = Math.Clamp(x / w, -1f, 1f);
            return down * MathF.Pow(1f - t * t, 0.8f) - lift * (1f - t * t) + tilt * t;
        }

        if (blink)
        {
            // Shut: the lashes' line, curving down from corner to corner, and the fold over it
            Curve(canvas, at, side, w, x => 0.25f * down * (1f - (x / w) * (x / w)) + tilt * (x / w), 0.022f * u, 0.012f * u, Ink);
            if (woman) Lashes(canvas, at + new Vector2(side * w, tilt), side, u, 0.4f);
            Curve(canvas, at - new Vector2(0, 0.055f * u), side, w * 0.9f, x => -0.4f * up * (1f - (x / w) * (x / w)), 0.009f * u, 0.009f * u, fold);
            return;
        }

        // The almond, its iris and pupil, sampled four by four per pixel so the edges are smooth
        float irisR = 0.071f * u * (expression == Expression.Surprised ? 0.86f : 1f);
        var irisAt = at + new Vector2(0, 0.006f * u);
        var irisDark = PixelCanvas.Mix(s.Eyes, Ink, 0.45f);
        var irisLight = PixelCanvas.Light1(s.Eyes, 0.42f);
        var pupil = PixelCanvas.Mix(s.Eyes, Ink, 0.85f);
        int x0 = (int)(at.X - w - 2), x1 = (int)(at.X + w + 2), y0 = (int)(at.Y - up * 1.4f - 2), y1 = (int)(at.Y + down * 1.4f + 2);
        for (int py = y0; py <= y1; py++)
            for (int px = x0; px <= x1; px++)
            {
                int inside = 0;
                for (int sy = 0; sy < 4; sy++)
                    for (int sx = 0; sx < 4; sx++)
                    {
                        float qx = px + (sx + 0.5f) / 4f - at.X, qy = py + (sy + 0.5f) / 4f - at.Y;
                        float ox = qx * side;
                        if (MathF.Abs(qx) <= w && qy >= Upper(ox) && qy <= Lower(ox)) inside++;
                    }
                if (inside == 0) continue;
                var p = new Vector2(px + 0.5f, py + 0.5f);
                float ri = Vector2.Distance(p, irisAt);
                Color col = White;
                if (ri <= irisR)
                {
                    float t = Math.Clamp((p.Y - (irisAt.Y - irisR)) / (2f * irisR), 0f, 1f);
                    col = PixelCanvas.Mix(irisDark, irisLight, t * 0.9f);
                    if (ri > irisR * 0.82f) col = PixelCanvas.Mix(col, irisDark, (ri - irisR * 0.82f) / (irisR * 0.18f) * 0.8f);
                    if (ri <= irisR * 0.44f) col = pupil;
                }
                else if (ri <= irisR + 1f)
                    col = PixelCanvas.Mix(White, irisDark, irisR + 1f - ri);
                // The upper lid casts its shade over the top of the white
                float shade = Math.Clamp(1f - (p.Y - (at.Y + Upper((p.X - at.X) * side))) / (0.035f * u), 0f, 1f);
                col = PixelCanvas.Mix(col, PixelCanvas.Mix(col, fold, 0.6f), shade * 0.7f);
                AaPaint.Plot(canvas, px, py, col, inside / 16f);
            }
        // Two highlights on the iris, the larger up toward the light
        AaPaint.Ellipse(canvas, irisAt + new Vector2(-0.032f * u, -0.03f * u), 0.019f * u, 0.019f * u, Color.White);
        AaPaint.Ellipse(canvas, irisAt + new Vector2(0.026f * u, 0.03f * u), 0.009f * u, 0.009f * u, Color.White);

        // The upper lid's line, heavier toward the outer corner; the lower lid's light line along its outer part; the fold
        Curve(canvas, at, side, w * 1.04f, Upper, woman ? 0.03f * u : 0.024f * u, 0.012f * u, Ink);
        if (woman) Lashes(canvas, at + new Vector2(side * w, Upper(w)), side, u, 1f);
        Curve(canvas, at, side, w * 0.96f, x => Lower(x) + 0.004f * u, 0.009f * u, 0.009f * u, fold, from: -0.2f);
        Curve(canvas, at - new Vector2(0, 0.048f * u), side, w * 0.88f, x => Upper(x) * 0.95f - 0.008f * u, 0.009f * u, 0.009f * u, fold, from: -0.6f);
    }

    /// <summary>A line along y = <paramref name="height"/>(x) across an eye, from <paramref name="from"/> of its half width on the inner side to its outer corner, its width going from inner to outer.</summary>
    private static void Curve(PixelCanvas canvas, Vector2 at, float side, float halfWidth, Func<float, float> height, float outerWidth, float innerWidth, Color colour, float from = -1f)
    {
        const int Steps = 18;
        Vector2 Point(float x) => at + new Vector2(x * side, height(x));
        var prev = Point(from * halfWidth);
        for (int i = 1; i <= Steps; i++)
        {
            float t = from + (1f - from) * i / Steps;
            var next = Point(t * halfWidth);
            float share = (t + 1f) / 2f;
            AaPaint.Line(canvas, prev, next, innerWidth + (outerWidth - innerWidth) * share, colour);
            prev = next;
        }
    }

    /// <summary>A woman's lashes: two short flicks out and up from the eye's outer corner.</summary>
    private static void Lashes(PixelCanvas canvas, Vector2 corner, float side, float u, float open)
    {
        AaPaint.Line(canvas, corner, corner + new Vector2(side * 0.05f * u, -0.035f * u * open - 0.01f * u), 0.016f * u, Ink);
        AaPaint.Line(canvas, corner - new Vector2(side * 0.03f * u, 0.012f * u), corner + new Vector2(side * 0.02f * u, -0.05f * u * open - 0.008f * u), 0.012f * u, Ink);
    }

    /// <summary>
    /// A brow: a tapered stroke arching over the eye from its inner end, heavier, to its outer, raised in surprise, its
    /// inner end down in anger and up in sorrow.
    /// </summary>
    private static void Brow(PixelCanvas canvas, Func<float, float, Vector2> px, float side, float u, Expression expression, Color colour, bool woman)
    {
        float rise = expression == Expression.Surprised ? 0.06f : 0f;
        float inner = expression switch { Expression.Angry => -0.05f, Expression.Sad => 0.05f, _ => 0f };
        float outer = expression switch { Expression.Angry => 0.02f, Expression.Sad => -0.025f, _ => 0f };
        const int Steps = 16;
        float thick = woman ? 0.04f : 0.056f;
        for (int i = 0; i <= Steps; i++)
        {
            float t = i / (float)Steps;
            float x = 0.12f + 0.36f * t;
            float arch = 0.035f * MathF.Sin(MathF.PI * MathF.Pow(t, 0.8f));
            float y = 0.185f + rise + arch + inner * (1f - t) + outer * t;
            var at = px(side * x, y);
            float r = (thick * (1f - 0.62f * t)) * 0.5f * u;
            AaPaint.Ellipse(canvas, at, r * 1.2f, r, colour);
        }
    }

    /// <summary>
    /// The mouth a little under the nose (style guide, "Grown characters", "Faces"): lips in a rose shade with the line
    /// between them darker (a man's lips nearer the skin), and each expression's own shape: a smile with the teeth
    /// showing, a small round opening, the corners down, a tight line; speaking, an opening shaped by the face.
    /// </summary>
    private static void Mouth(PixelCanvas canvas, CharacterStyle s, Func<float, float, Vector2> px, float u, Expression expression, bool open, bool woman, Color skin)
    {
        var lip = woman ? new Color(200, 98, 110, 255) : PixelCanvas.Mix(skin, new Color(168, 92, 92, 255), 0.4f);
        var line = woman ? new Color(132, 52, 64, 255) : PixelCanvas.Mix(skin, new Color(104, 50, 56, 255), 0.7f);
        var inside = new Color(78, 26, 40, 255);
        var teeth = new Color(248, 246, 240, 255);
        var at = px(0f, -0.625f);
        float w = 0.16f * u;

        void Lips(float upper, float lower, float smile)
        {
            // The lower lip, the fuller, and the upper above the line, shaded toward the corners
            AaPaint.Ellipse(canvas, at + new Vector2(0, 0.032f * u), w * 0.72f, lower * u, PixelCanvas.Mix(lip, skin, woman ? 0f : 0.25f));
            AaPaint.Ellipse(canvas, at - new Vector2(0, 0.012f * u), w * 0.86f, upper * u, PixelCanvas.Mix(lip, line, 0.25f));
            Line(smile, 0.016f * u);
        }
        void Line(float smile, float width)
        {
            const int Steps = 12;
            var prev = at + new Vector2(-w, -smile * u);
            for (int i = 1; i <= Steps; i++)
            {
                float t = -1f + 2f * i / Steps;
                var next = at + new Vector2(t * w, -smile * u * t * t);
                AaPaint.Line(canvas, prev, next, width, line);
                prev = next;
            }
        }

        if (open)
        {
            // Speaking: an opening shaped by the face, the lips round it, the dark of the mouth and the tongue's light
            var (ow, oh, drop) = expression switch
            {
                Expression.Happy => (0.12f, 0.08f, 0.01f),
                Expression.Surprised => (0.07f, 0.09f, 0.0f),
                Expression.Sad => (0.06f, 0.04f, 0.02f),
                Expression.Angry => (0.11f, 0.045f, 0.0f),
                _ => (0.08f, 0.055f, 0.005f)
            };
            var o = at + new Vector2(0, drop * u + 0.01f * u);
            AaPaint.Ellipse(canvas, o, (ow + 0.025f) * u, (oh + 0.025f) * u, lip);
            AaPaint.Ellipse(canvas, o, ow * u, oh * u, inside);
            AaPaint.Ellipse(canvas, o + new Vector2(0, oh * 0.55f * u), ow * 0.55f * u, oh * 0.32f * u, new Color(214, 104, 116, 255));
            if (expression == Expression.Angry)
                AaPaint.Line(canvas, o + new Vector2(-ow * 0.8f * u, -oh * 0.45f * u), o + new Vector2(ow * 0.8f * u, -oh * 0.45f * u), 0.012f * u, teeth);
            return;
        }
        switch (expression)
        {
            case Expression.Happy:
            {
                // A smile with the upper teeth showing under the lip
                var lipLow = at + new Vector2(0, 0.018f * u);
                AaPaint.HalfDisc(canvas, lipLow - new Vector2(0, 0.022f * u), w * 0.88f, 0.085f * u, lip);
                AaPaint.HalfDisc(canvas, lipLow - new Vector2(0, 0.02f * u), w * 0.76f, 0.062f * u, inside);
                AaPaint.HalfDisc(canvas, lipLow - new Vector2(0, 0.02f * u), w * 0.72f, 0.022f * u, teeth);
                Line(0.05f, 0.014f * u);
                break;
            }
            case Expression.Surprised:
                AaPaint.Ellipse(canvas, at + new Vector2(0, 0.01f * u), 0.065f * u, 0.07f * u, lip);
                AaPaint.Ellipse(canvas, at + new Vector2(0, 0.012f * u), 0.042f * u, 0.048f * u, inside);
                break;
            case Expression.Sad:
                Lips(0.022f, 0.036f, -0.045f);
                break;
            case Expression.Angry:
                Lips(0.018f, 0.028f, -0.015f);
                break;
            default:
                Lips(0.022f, 0.04f, 0.012f);
                break;
        }
    }

    // ------------------------------------------------------------------ painting helpers

    /// <summary>A small soft-edged opaque blob (a nostril).</summary>
    private static void Blob(PixelCanvas canvas, Vector2 at, float rx, float ry, Color colour) => AaPaint.Ellipse(canvas, at, rx, ry, colour);
}
